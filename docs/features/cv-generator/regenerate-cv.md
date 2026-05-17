# RegenerateCV

## Summary

User regenerates a CV with a different template (or the same template with a different tailoring prompt). Creates a **new** GeneratedCV record with fresh AI-tailored content while preserving the original CV in history. Uses the original profile and JD snapshots — no re-fetching from other modules.

## Actor

Authenticated user (CV owner)

## Infrastructure

- **Wolverine** — async message processing (in-process)
- **gRPC** — fetch new template
- **OpenAI API** — CV content re-tailoring

## Endpoints

### Trigger Regeneration

```
POST /api/cv/{id}/regenerate
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "templateId": "guid",
  "tailoringPrompt": "Make it more concise. Focus on cloud experience."
}
```

**202 Accepted**

```json
{
  "generationId": "guid"
}
```

Creates a **new** `GeneratedCV` record (new ID) using the original's profile/JD snapshots. Publishes a `TailorCV` command via Wolverine.

### Poll Status

Same as GenerateCV:

```
GET /api/cv/generate/{generationId}/status
Authorization: Bearer {accessToken}
```

Returns the same status response as [generate-cv.md](./generate-cv.md#poll-status).

## Validation Rules

| Field | Rules |
|-------|-------|
| TemplateId | Required, valid GUID |
| TailoringPrompt | Optional, max 2000 chars |

## Business Rules

- Original CV must have status=Done
- Creates a **new** GeneratedCV record (new ID) — original CV stays untouched in history
- Reuses the original's ProfileSnapshot and JobSnapshot (same profile + JD data)
- Fresh AI tailoring with new template + optional new prompt
- Match score is recalculated (same algorithm, same profile + JD → same score as original)
- Only the CV owner can regenerate
- The new generation follows the exact same background pipeline as GenerateCV

## Relationship to Original

```
History:
├── Original CV (id: aaa, template: minimal, prompt: null, content: {...})
│   └── User manually edited content
├── Regenerated CV (id: bbb, template: professional, prompt: "more concise", content: {...})
│   └── Fresh AI output, no manual edits
└── Regenerated CV (id: ccc, template: creative, prompt: null, content: {...})
    └── Fresh AI output
```

- Each regeneration is independent
- Original CV with manual edits is preserved
- User can compare versions and pick the best one

## Flow

1. Client sends `POST /api/cv/{id}/regenerate` with templateId + optional prompt
2. **Auth middleware** validates JWT
3. **ValidationDecorator** runs FluentValidation
4. **Handler** executes:
   - Get `UserId` from `ICurrentUserService`
   - Find original GeneratedCV by id and userId → 404 if not found
   - Check status=Done → 409 if still processing
   - Create new GeneratedCV record (new ID, copy userId + ProfileSnapshot + JobSnapshot, new templateId, new tailoringPrompt, status=Queued)
   - Publish `TailorCV` command via Wolverine
   - Return 202 with new generationId
5. **Wolverine background handler** processes (same as GenerateCV):
   - Fetch new template via gRPC → validate active
   - Compute match score (same result — same profile + JD)
   - Call OpenAI with profile + JD + new template context + tailoringPrompt → fresh Content
   - Store Content + MatchScore, set status=Done
   - Publish `CVTailoringCompleted`
6. Client polls `GET /api/cv/generate/{generationId}/status`

## Inter-module Interactions

**gRPC call** during background processing:

| Call | Module | Purpose |
|------|--------|---------|
| `GetTemplateById` | Templates | Fetch new template (validate active) |

No profile/JD gRPC needed — uses existing snapshots.

### External Dependencies

| Service | Purpose | Resilience |
|---------|---------|------------|
| Templates gRPC | Fetch new template | Built-in gRPC retry |
| OpenAI API | CV content re-tailoring | Wolverine built-in retry |

## Diagrams

### Full Flow — Sequence Diagram

```mermaid
sequenceDiagram
    participant C as Client
    participant MW as Auth Middleware
    participant API as Minimal API
    participant V as ValidationDecorator
    participant L as LoggingDecorator
    participant H as Handler
    participant DB as CVGeneratorDbContext
    participant W as Wolverine Bus
    participant WH as Background Handler
    participant GRPC as Templates gRPC
    participant AI as OpenAI API

    Note over C,AI: STEP 1 — Trigger Regeneration
    C->>MW: POST /api/cv/{id}/regenerate + Bearer
    MW->>MW: Validate JWT
    MW->>API: Request
    API->>V: HandleAsync(request)
    V->>V: Validate
    V->>L: HandleAsync(request)
    L->>H: HandleAsync(request)
    H->>DB: Find original GeneratedCV by id + userId
    alt Not Found
        H-->>L: Result.Failure(NotFoundError)
        L-->>C: 404 Not Found
    end
    alt Status not Done
        H-->>L: Result.Failure(ConflictError)
        L-->>C: 409 Conflict
    end
    H->>DB: Create new GeneratedCV<br/>(copy snapshots, new templateId, status=Queued)
    H->>W: PublishAsync(TailorCV)
    H-->>C: 202 { generationId }

    Note over C,AI: STEP 2 — Background Processing
    W->>WH: Deliver TailorCV command
    WH->>GRPC: GetTemplateById(templateId)
    GRPC-->>WH: Template data → validate active
    WH->>WH: Compute match score (same as original)
    WH->>AI: Tailor CV content (snapshots + new template + prompt)
    AI-->>WH: Fresh Content JSON
    WH->>DB: Update status=Done + content + score
    WH->>W: PublishAsync(CVTailoringCompleted)

    Note over C,AI: STEP 3 — Poll Status (same as GenerateCV)
    C->>API: GET /api/cv/generate/{generationId}/status
    API-->>C: { status, generatedCv }
```

### Flowchart

```mermaid
flowchart TD
    A[POST /api/cv/id/regenerate] --> B{JWT valid?}
    B -->|No| C[Return 401]
    B -->|Yes| D{Validate}
    D -->|Invalid| E[Return 400]
    D -->|Valid| F{Original CV exists?}
    F -->|No| G[Return 404]
    F -->|Yes| H{Original status == Done?}
    H -->|No| I[Return 409]
    H -->|Yes| J[Create new GeneratedCV<br/>copy snapshots, new templateId]
    J --> K[Publish TailorCV]
    K --> L[Return 202 new generationId]

    M[Background] --> N[Fetch new template via gRPC]
    N --> O{Active?}
    O -->|No| P[status=Failed]
    O -->|Yes| Q[Compute match score]
    Q --> R[OpenAI: re-tailor with new template + prompt]
    R --> S{Success?}
    S -->|No| P
    S -->|Yes| T[Store content + score]
    T --> U[status=Done + publish event]
```

## Error Codes

| Code | HTTP Status | When |
|------|-------------|------|
| `VALIDATION` | 400 | Invalid/missing templateId |
| `UNAUTHORIZED` | 401 | Missing or invalid JWT |
| `NOT_FOUND` | 404 | Original CV not found |
| `CONFLICT` | 409 | Original CV still processing |
| — | 202 | Regeneration triggered |

## History Preservation

Regeneration never modifies or deletes existing CVs. Each generation (original or regenerated) is a separate record in history. The user's manual edits on any version are preserved until they explicitly delete the CV (future feature).
