# ParseJobDescription

## Summary

User pastes job description text. Backend processes it asynchronously via OpenAI. Client polls for status until result is ready. Not saved — user must explicitly save via SaveJobDescription.

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

Enqueues a **Hangfire job** that sends rawText to OpenAI for structured extraction.

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
- Uses Polly retry for OpenAI API (3 attempts, exponential backoff)
- ParseJob stored in `jobscraper.parse_jobs` with `type = ManualText`
- Parse job timeout: 2 minutes (Hangfire)
- Only the owner can poll their parse jobs

## Flow

1. Client sends `POST /api/jobs/parse` with rawText
2. **Auth middleware** validates JWT
3. **ValidationDecorator** validates rawText length
4. **Handler** creates `ParseJob` (Queued) + enqueues Hangfire job → returns parseId (202)
5. **Hangfire background job:**
   - Update status → Processing
   - Send rawText to OpenAI for structured extraction (Polly retry)
   - Map result to `ParsedJob` structure
   - Update status → Done + parsedData (or Failed + error)
6. Client polls `GET /api/jobs/parse/{parseId}/status` until DONE or FAILED

## Inter-module Interactions

**None.** Only external dependency is OpenAI API.

## Diagrams

### Full Flow — Sequence Diagram

```mermaid
sequenceDiagram
    participant C as Client
    participant MW as Auth Middleware
    participant API as Minimal API
    participant V as ValidationDecorator
    participant L as LoggingDecorator
    participant H as TriggerHandler
    participant DB as JobScraperDbContext
    participant HF as Hangfire
    participant AI as OpenAI API

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
    H->>HF: Enqueue background job
    H-->>C: 202 { parseId }

    Note over C,AI: STEP 2 — Background Processing
    HF->>DB: Update status=Processing
    HF->>AI: Send rawText for structured extraction
    Note over AI: Polly retry: 3 attempts,<br/>exponential backoff
    AI-->>HF: structured JSON
    HF->>DB: Update status=Done + parsedData

    Note over C,AI: STEP 3 — Poll Status
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
    A[Hangfire picks up job] --> B[Update status → Processing]
    B --> C[Send rawText to OpenAI<br/>Polly: 3 retries]
    C --> D{OpenAI success?}
    D -->|No| E[Update status → Failed<br/>error: AI error message]
    D -->|Yes| F[Map to ParsedJob structure]
    F --> G[Update status → Done + parsedData]
```

## Error Codes

| Code | HTTP Status | When |
|------|-------------|------|
| `VALIDATION` | 400 | rawText empty, too short (<50), or too long (>10000) |
| `UNAUTHORIZED` | 401 | Missing or invalid JWT |
| `NOT_FOUND` | 404 | parseId not found |

## Database Table

### parse_jobs (in jobscraper schema)

```
parse_jobs
├── id (guid, PK)
├── user_id (guid)
├── type (enum: ManualText, UrlScrape)
├── raw_input (text — rawText or URL)
├── status (enum: Queued, Processing, Done, Failed)
├── parsed_data (JSONB, null until Done)
├── error (string, null unless Failed)
├── created_at (datetime)
├── completed_at (datetime, null until Done/Failed)
```

Shared with ScrapeJobUrl — differentiated by `type` field.
