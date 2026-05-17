# GetProfile

## Summary

Authenticated user retrieves their full profile with all sections, ordered by `SectionOrder`.

## Actor

Authenticated user (profile owner)

## Request

```
GET /api/profiles/me
Authorization: Bearer {accessToken}
```

## Response

**200 OK**

```json
{
  "id": "guid",
  "headline": "string",
  "summary": "string",
  "phone": "string",
  "location": "string",
  "website": "string",
  "linkedinUrl": "string",
  "githubUrl": "string",
  "completeness": 65,
  "sections": [
    {
      "sectionType": "Experience",
      "sectionId": "guid",
      "order": 1,
      "data": { "company": "...", "role": "...", "startDate": "...", "endDate": "...", "description": "...", "isCurrent": true }
    },
    {
      "sectionType": "Custom",
      "sectionId": "guid",
      "order": 2,
      "data": { "title": "Publications", "items": [...] }
    },
    {
      "sectionType": "Skill",
      "sectionId": "guid",
      "order": 3,
      "data": { "category": "Languages", "items": ["C#", "Python"] }
    }
  ],
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

**404 Not Found**

```json
{
  "code": "NOT_FOUND",
  "message": "Profile not found"
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

- Returns all sections ordered by `SectionOrder.Order`
- Each section includes its `sectionType` discriminator so the frontend knows how to render it
- Only the profile owner can access this endpoint
- Completeness is recalculated on read

## Flow

1. Client sends `GET /api/profiles/me`
2. **Auth middleware** validates JWT → 401 if invalid
3. **LoggingDecorator** logs entry
4. **Handler** executes:
   - Get `UserId` from `ICurrentUserService`
   - Find profile by userId → 404 if not found
   - Load all sections (Experience, Project, Skill, Education, Certification, Language, CustomSection) by profileId
   - Load `SectionOrder` entries for this profile
   - Map each section to response with its type and order
   - Sort sections by `SectionOrder.Order`
   - Calculate completeness
   - Return full profile response
5. **LoggingDecorator** logs result

## Inter-module Interactions

**None directly from this endpoint.** Other modules access profile data via **gRPC** (`ProfileService.GetProfileById`).

```mermaid
graph LR
    subgraph "Profile Module"
        A[GetProfile HTTP Endpoint]
    end

    subgraph "Other Modules via gRPC"
        B[CVGenerator]
        C[Dashboard]
    end

    A -.->|no direct interaction| A
    B -->|gRPC: GetProfileById| A
    C -->|gRPC: GetProfileById| A
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
    participant DB as ProfileDbContext

    C->>MW: GET /api/profiles/me + Bearer
    MW->>MW: Validate JWT
    alt Invalid
        MW-->>C: 401 Unauthorized
    end
    MW->>API: Request
    API->>L: HandleAsync(request)
    L->>H: HandleAsync(request)
    H->>US: Get UserId
    H->>DB: Find profile by userId
    alt Not Found
        H-->>L: Result.Failure(NotFoundError)
        L-->>C: 404 Not Found
    end
    H->>DB: Load all sections by profileId
    H->>DB: Load SectionOrder for profile
    H->>H: Map sections by type + sort by order
    H->>H: Calculate completeness
    H-->>L: Result.Success(ProfileResponse)
    L-->>C: 200 OK
```

### Data Assembly Flowchart

```mermaid
flowchart LR
    subgraph "Profile DB Schema"
        P[Profile]
        E[Experiences]
        PR[Projects]
        SK[Skills]
        ED[Education]
        CE[Certifications]
        LA[Languages]
        CS[CustomSections]
        SO[SectionOrder]
    end

    P --> Q[Query: Get all by profileId]
    E --> Q
    PR --> Q
    SK --> Q
    ED --> Q
    CE --> Q
    LA --> Q
    CS --> Q
    SO --> Q

    Q --> M[Map each section to unified response shape]
    M --> R[Sort by SectionOrder.Order]
    R --> RES[ProfileResponse with ordered sections]
```

## Error Codes

| Code | HTTP Status | When |
|------|-------------|------|
| `UNAUTHORIZED` | 401 | Missing or invalid JWT |
| `NOT_FOUND` | 404 | User has no profile yet |
