# ScrapeJobUrl

## Summary

User provides a job posting URL. Backend uses Playwright to scrape the page, then OpenAI to parse the JD. Same async polling pattern as ParseJobDescription — shares the `parse_jobs` table and polling endpoint.

## Actor

Authenticated user

## Endpoints

### Trigger Scrape

```
POST /api/jobs/scrape
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "url": "https://linkedin.com/jobs/view/123456"
}
```

**202 Accepted**

```json
{
  "parseId": "guid"
}
```

Enqueues a **Hangfire job** that:
1. Validates URL reachability
2. Uses Playwright to render the page and extract visible text
3. Sends extracted text to OpenAI for structured parsing
4. Stores result in `jobscraper.parse_jobs`

### Poll Status

Same endpoint as ParseJobDescription:

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
    "requiredSkills": ["C#", "Azure", "SQL"],
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
  "error": "Could not access the page. It may require authentication or be behind a paywall."
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
| Url | Required, valid HTTP/HTTPS URL, max 2048 chars |

## Business Rules

- URL must be valid HTTP/HTTPS
- Playwright runs headless with 30-second page load timeout
- Falls back to raw page text if structured content extraction fails
- Supports major job boards (LinkedIn, Indeed, Glassdoor, etc.) but works with any URL
- Returns error if page is unreachable, blocked (403), or paywalled
- Uses Polly retry for Playwright (2 retries) and OpenAI (3 retries)
- Scraped text stored in `raw_input` field of `parse_jobs` table (no S3 — just text)
- ParseJob stored with `type = UrlScrape`
- Parse job timeout: 3 minutes (Hangfire) — longer than manual parse due to scraping
- Only the owner can poll their parse jobs

## Flow

1. Client sends `POST /api/jobs/scrape` with URL
2. **Auth middleware** validates JWT
3. **ValidationDecorator** validates URL format
4. **Handler** creates `ParseJob` (type=UrlScrape, status=Queued) + enqueues Hangfire job → returns parseId (202)
5. **Hangfire background job:**
   - Update status → Processing
   - Playwright: navigate to URL, extract visible text (Polly retry, 30s timeout)
   - On failure → status=Failed with descriptive error
   - Store scraped text in `raw_input`
   - Send text to OpenAI for structured extraction (Polly retry)
   - Map result to `ParsedJob` structure
   - Update status → Done + parsedData (or Failed + error)
6. Client polls `GET /api/jobs/parse/{parseId}/status` until DONE or FAILED
7. User reviews parsed data → saves via `SaveJobDescription`

## Inter-module Interactions

**None.** External dependencies: Playwright (headless browser) + OpenAI API.

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
    participant PW as Playwright
    participant AI as OpenAI API

    Note over C,AI: STEP 1 — Trigger Scrape
    C->>MW: POST /api/jobs/scrape + Bearer
    MW->>MW: Validate JWT
    MW->>API: Request
    API->>V: HandleAsync(request)
    V->>V: Validate URL
    alt Validation Failed
        V-->>C: 400 Bad Request
    end
    V->>L: HandleAsync(request)
    L->>H: HandleAsync(request)
    H->>DB: Create ParseJob type=UrlScrape status=Queued
    H->>HF: Enqueue background job
    H-->>C: 202 { parseId }

    Note over C,AI: STEP 2 — Background Processing
    HF->>DB: Update status=Processing
    HF->>PW: Navigate to URL + extract text
    Note over PW: Polly retry: 2 attempts<br/>30s timeout
    alt Page Failed
        PW-->>HF: error
        HF->>DB: Update status=Failed + error
    else Page OK
        PW-->>HF: pageText
        HF->>DB: Store pageText in raw_input
        HF->>AI: Send pageText for structured extraction
        Note over AI: Polly retry: 3 attempts
        AI-->>HF: structured JSON
        HF->>DB: Update status=Done + parsedData
    end

    Note over C,AI: STEP 3 — Poll Status
    loop Polling every 2s
        C->>API: GET /api/jobs/parse/{parseId}/status + Bearer
        API->>DB: Get ParseJob
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
    B --> C[Playwright: navigate to URL<br/>Polly: 2 retries, 30s timeout]
    C --> D{Page loaded?}
    D -->|No| E{Error type?}
    E -->|Timeout| F[status → Failed<br/>error: Page load timeout]
    E -->|403 or blocked| G[status → Failed<br/>error: Page blocked or requires auth]
    E -->|Network error| H[status → Failed<br/>error: Could not reach page]
    D -->|Yes| I[Extract visible text from page]
    I --> J[Store text in raw_input]
    J --> K[Send to OpenAI for parsing<br/>Polly: 3 retries]
    K --> L{OpenAI success?}
    L -->|No| M[status → Failed<br/>error: AI parsing failed]
    L -->|Yes| N[Map to ParsedJob]
    N --> O[status → Done + parsedData]
```

### Component Diagram

```mermaid
graph TD
    subgraph "JobScraper Module"
        A[ScrapeJobUrl Feature]
        B[ParseJobDescription Feature]
        C[Shared Poll Status Endpoint]
    end

    subgraph "Shared Infrastructure"
        D[Hangfire Background Jobs]
        E[Polly Resilience]
    end

    subgraph "External"
        F[Playwright Headless Browser]
        G[OpenAI API]
    end

    A --> D
    B --> D
    C --> A
    C --> B
    D --> F
    D --> G
    D --> E
    E -.->|wraps| F
    E -.->|wraps| G
```

## Error Codes

| Code | HTTP Status | When |
|------|-------------|------|
| `VALIDATION` | 400 | Invalid URL format |
| `UNAUTHORIZED` | 401 | Missing or invalid JWT |
| `NOT_FOUND` | 404 | parseId not found |
| — | Polling | `FAILED` status with error from background job |

## Security Considerations

- URL validation prevents internal network access (SSRF protection)
- Blocklist for internal IPs (127.0.0.1, 10.x, 172.x, 192.168.x)
- Rate limiting on scrape endpoint (prevent abuse)
- Playwright runs in sandboxed environment
- No cookies or auth forwarded to scraped pages
