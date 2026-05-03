# GetGeneratedCV

## Summary

Get full details of a single generated CV, including all content, match score, cover letter, snapshots, and PDF status. This endpoint also serves as the data source for **client-side preview rendering** — the frontend combines the CV content from this response with template HTML/CSS from `GET /api/templates/{id}` to render a live preview.

## Actor

Authenticated user (CV owner)

## Request

```
GET /api/cv/{id}
Authorization: Bearer {accessToken}
```

## Response

**200 OK**

```json
{
  "id": "guid",
  "generationType": "FullCV",
  "status": "Done",
  "profileSnapshot": {
    "headline": "Senior Software Engineer",
    "summary": "10 years of experience in...",
    "firstName": "Jane",
    "lastName": "Smith",
    "sections": [...]
  },
  "jobSnapshot": {
    "title": "Senior Software Engineer",
    "company": "Google",
    "location": "Mountain View, CA",
    "requiredSkills": ["C#", "Azure", "SQL", "Docker"],
    "responsibilities": [...],
    "seniorityLevel": "Senior"
  },
  "templateId": "guid",
  "content": {
    "summary": "Results-driven senior engineer with 8+ years building scalable systems...",
    "sections": [
      {
        "type": "Experience",
        "title": "Relevant Experience",
        "items": [
          {
            "company": "Google",
            "role": "Senior Engineer",
            "description": "Led microservices architecture migration...",
            "startDate": "2020-01",
            "endDate": null,
            "isCurrent": true
          }
        ]
      },
      {
        "type": "Skill",
        "title": "Key Skills",
        "items": ["C#", "Azure", "SQL", "Docker"]
      }
    ]
  },
  "matchScore": {
    "percentage": 82,
    "matchingSkills": ["C#", "Azure", "SQL", "Docker"],
    "missingSkills": ["Kubernetes", "Terraform"]
  },
  "coverLetter": null,
  "tailoringPrompt": "Emphasize leadership experience",
  "pdfStatus": "Ready",
  "createdAt": "2026-05-03T10:00:00Z",
  "updatedAt": "2026-05-03T10:05:00Z"
}
```

**404 Not Found**

```json
{
  "code": "NOT_FOUND",
  "message": "Generated CV not found"
}
```

**401 Unauthorized**

```json
{
  "code": "UNAUTHORIZED",
  "message": "Not authenticated"
}
```

## Business Rules

- Only the CV owner can access their generated CVs
- Returns full content including all sections, match score, and cover letter
- `profileSnapshot` and `jobSnapshot` are the frozen data from generation time
- If status is Queued/Processing, `content` and `matchScore` are null
- If status is Failed, `error` field is included

## Client-Side Preview

The frontend renders CV previews client-side by combining data from two endpoints:

1. **`GET /api/cv/{id}`** (this endpoint) — returns CV content JSON
2. **`GET /api/templates/{templateId}`** — returns template HTML + CSS

The frontend injects the CV content into the template's HTML structure and renders it in a sandboxed iframe using `srcdoc`:

```
┌─────────────────────────────────────────┐
│ Browser (iframe srcdoc)                 │
│                                         │
│  Template HTML + CSS                    │
│  ┌───────────────────────────────────┐  │
│  │ {summary}                         │  │
│  │ {experience sections}             │  │
│  │ {skills}                          │  │
│  │ {education}                       │  │
│  └───────────────────────────────────┘  │
│                                         │
└─────────────────────────────────────────┘
```

This provides:
- **Instant preview updates** when user edits content (no server round-trip)
- **No dedicated backend preview endpoint** needed
- **Template IP is still protected** — template HTML is only served to authenticated users

## Flow

1. Client sends `GET /api/cv/{id}`
2. **Auth middleware** validates JWT
3. **LoggingDecorator** logs entry
4. **Handler** executes:
   - Get `UserId` from `ICurrentUserService`
   - Find GeneratedCV by id and userId → 404 if not found or not owner
   - Return full CV details
5. **LoggingDecorator** logs result

## Inter-module Interactions

**None directly from this endpoint.** The CV data is fully self-contained (uses stored snapshots).

The client-side preview rendering calls `GET /api/templates/{templateId}` from the Templates module, but this is a frontend-initiated call, not a backend inter-module interaction.

```mermaid
graph LR
    subgraph "CVGenerator Module"
        A[GetGeneratedCV Endpoint]
    end

    subgraph "Templates Module"
        B[GetTemplate Endpoint]
    end

    subgraph "Frontend"
        C[CV Detail Page]
        D[Preview Renderer]
    end

    C -->|1. GET /api/cv/id| A
    C -->|2. GET /api/templates/templateId| B
    C -->|3. Combine content + template| D
```

## Diagrams

### Sequence Diagram

```mermaid
sequenceDiagram
    participant C as Client
    participant MW as Auth Middleware
    participant API as Minimal API
    participant L as LoggingDecorator
    participant H as Handler
    participant US as ICurrentUserService
    participant DB as CVGeneratorDbContext

    C->>MW: GET /api/cv/{id} + Bearer
    MW->>MW: Validate JWT
    MW->>API: Request
    API->>L: HandleAsync(request)
    L->>H: HandleAsync(request)
    H->>US: Get UserId
    H->>DB: Find GeneratedCV by id + userId
    alt Not Found
        H-->>L: Result.Failure(NotFoundError)
        L-->>C: 404 Not Found
    end
    H-->>L: Result.Success(GeneratedCVResponse)
    L-->>C: 200 OK
```

### Client-Side Preview — Flowchart

```mermaid
flowchart LR
    A[GET /api/cv/id] --> B[Get CV content JSON]
    B --> C[Extract templateId]
    C --> D[GET /api/templates/templateId]
    D --> E[Get template HTML + CSS]
    E --> F[Inject CV content into template]
    F --> G[Render in iframe srcdoc]
    G --> H[User sees live preview]

    I[User edits content] --> J[PUT /api/cv/id/content]
    J --> K[Re-inject updated content]
    K --> G
```

## Error Codes

| Code | HTTP Status | When |
|------|-------------|------|
| `UNAUTHORIZED` | 401 | Missing or invalid JWT |
| `NOT_FOUND` | 404 | Generated CV not found or not owner |
