# ParseJobDescription

## Summary

User pastes job description text. Backend processes it asynchronously via OpenAI through the JobDescriptions Worker. Client polls for status until result is ready. Not saved — user must explicitly save via SaveJobDescription.

## Actor

Authenticated user

## Endpoints

### Trigger Parse

```
POST /api/jobs/parse
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "rawText": "We are looking for a Senior Software Engineer with 5+ years of experience in C#..."
}
```

**202 Accepted**

```json
{
  "parseId": "guid"
}
```

Creates a `ParseJob` (status=Queued) and publishes `ParseJobText` command via **Wolverine + RabbitMQ**. The JobDescriptions Worker picks it up from the `job-description.commands` queue, calls OpenAI, and publishes the result back as an event.

### Poll Status

```
GET /api/jobs/parse/{parseId}/status
Authorization: Bearer {accessToken}
```

**200 OK — Processing**

```json
{
  "status": "PROCESSING"
}
```

**200 OK — Done**

```json
{
  "status": "DONE",
  "parsedJob": {
    "title": "Senior Software Engineer",
    "company": "Google",
    "location": "Mountain View, CA",
    "requiredSkills": ["C#", "Azure", "SQL", "Docker"],
    "responsibilities": ["Design and implement scalable systems", "Lead a team of engineers"],
    "qualifications": ["Bachelor's degree in CS", "5+ years experience"],
    "seniorityLevel": "Senior"
  }
}
```

**200 OK — Failed**

```json
{
  "status": "FAILED",
  "error": "Could not parse job description. Please try again."
}
```

**404 Not Found**

```json
{
  "code": "NOT_FOUND",
  "message": "Parse job not found"
}
```

## Validation Rules

| Field | Rules |
|-------|-------|
| RawText | Required, min 50 chars, max 10,000 chars |

## Business Rules

- Parsed result is **not saved** — user must explicitly save via `SaveJobDescription`
- Parse is idempotent — user can trigger multiple parses (each gets new parseId)
- ParseJob stored in `jobdescriptions.parse_jobs` with `type = ManualText`
- Only the owner can poll their parse jobs

## Flow

1. Client sends `POST /api/jobs/parse` with rawText
2. **Auth middleware** validates JWT
3. **ValidationDecorator** validates rawText length
4. **Handler** creates `ParseJob` (status=Queued) + publishes `ParseJobText` via Wolverine → returns parseId (202)
5. **JobDescriptions Worker** (separate process):
   - Consumes `ParseJobText` from `job-description.commands` queue
   - Calls OpenAI for structured extraction
   - Publishes `JobParsingCompleted` or `JobParsingFailed` to `job-description.events` queue
6. **API Wolverine handler** (`JobParsingCompletedHandler` / `JobParsingFailedHandler`):
   - Receives event from queue
   - Updates `ParseJob` status in DB (Done with parsedData, or Failed with error)
7. Client polls `GET /api/jobs/parse/{parseId}/status` until DONE or FAILED

## Inter-module Interactions

**None.** Only external dependency is OpenAI API via the Worker process.

## Diagrams

### Full Flow — Sequence Diagram

```mermaid
sequenceDiagram
    participant C as Client
    participant MW as Auth Middleware
    participant API as Minimal API
    participant V as ValidationDecorator
    participant L as LoggingDecorator
    participant H as ParseJobDescription Handler
    participant DB as JobDescriptionsDbContext
    participant WBus as Wolverine Bus
    participant W as JobDescriptions Worker
    participant AI as OpenAI API
    participant WH as Wolverine Handler

    Note over C,AI: STEP 1 — Trigger Parse
    C->>MW: POST /api/jobs/parse + Bearer
    MW->>MW: Validate JWT
    MW->>API: Request
    API->>V: HandleAsync(request)
    V->>V: Validate rawText
    alt Validation Failed
        V-->>C: 400 Bad Request
    end
    V->>L: HandleAsync(request)
    L->>H: HandleAsync(request)
    H->>DB: Create ParseJob type=ManualText status=Queued
    H->>WBus: PublishAsync(ParseJobText)
    H-->>C: 202 { parseId }

    Note over C,AI: STEP 2 — Worker Processing
    W->>WBus: Listen on job-description.commands
    WBus->>W: Deliver ParseJobText message
    W->>AI: Send rawText for structured extraction
    AI-->>W: structured JSON
    W->>WBus: PublishAsync(JobParsingCompleted)

    Note over C,AI: STEP 3 — Event Handler Updates DB
    API->>WBus: Listen on job-description.events
    WBus->>WH: Deliver JobParsingCompleted
    WH->>DB: Find ParseJob, update status=Done + parsedData
    WH->>DB: SaveChangesAsync()

    Note over C,AI: STEP 4 — Poll Status
    loop Polling every 2s
        C->>API: GET /api/jobs/parse/{parseId}/status + Bearer
        API->>DB: Get ParseJob by id
        alt Processing
            API-->>C: { status: "PROCESSING" }
        else Done
            API-->>C: { status: "DONE", parsedJob: {...} }
        else Failed
            API-->>C: { status: "FAILED", error: "..." }
        end
    end
```

### Background Job — Flowchart

```mermaid
flowchart TD
    A[Wolverine delivers ParseJobText] --> B[Call OpenAI for structured extraction]
    B --> C{OpenAI success?}
    C -->|No| D[Publish JobParsingFailed<br/>with error message]
    C -->|Yes| E[Map to ParsedJobData]
    E --> F[Publish JobParsingCompleted<br/>with parsed data]
    F --> G[Wolverine delivers event to API]
    G --> H[API updates ParseJob status in DB]
```

## Error Codes

| Code | HTTP Status | When |
|------|-------------|------|
| `VALIDATION` | 400 | rawText empty, too short (<50), or too long (>10000) |
| `UNAUTHORIZED` | 401 | Missing or invalid JWT |
| `NOT_FOUND` | 404 | parseId not found |

## Database Table

### parse_jobs (in jobdescriptions schema)

```
parse_jobs
├── id (guid, PK)
├── user_id (guid)
├── type (enum: ManualText, UrlScrape)
├── raw_text (text — rawText or URL)
├── status (enum: Queued, Processing, Done, Failed)
├── parsed_data (JSONB, null until Done)
├── error (string, null unless Failed)
├── source_url (uri, nullable)
├── created_at (datetime)
├── completed_at (datetime, null until Done/Failed)
```

Shared with ScrapeJobDescription — differentiated by `type` field.
