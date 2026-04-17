# UpdateProfile

## Summary

Authenticated user updates their profile's base fields (headline, summary, contact info, URLs). Section content is updated via separate Section CRUD features.

## Actor

Authenticated user (profile owner)

## Request

```
PUT /api/profiles/me
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "headline": "string",
  "summary": "string",
  "phone": "string",
  "location": "string",
  "website": "string",
  "linkedinUrl": "string",
  "githubUrl": "string"
}
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
  "completeness": 45,
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

## Validation Rules

| Field | Rules |
|-------|-------|
| Headline | Optional, max 200 chars |
| Summary | Optional, max 2000 chars |
| Phone | Optional, valid phone format |
| Location | Optional, max 200 chars |
| Website | Optional, valid URL |
| LinkedinUrl | Optional, valid URL |
| GithubUrl | Optional, valid URL |

## Business Rules

- Full replace of profile base fields (PUT semantics)
- Only the profile owner can update (enforced by `ICurrentUserService`)
- `updatedAt` is set automatically via `TimeProvider`
- Completeness is recalculated after update
- If profile doesn't exist → 404 (user must create first)

## Flow

1. Client sends `PUT /api/profiles/me`
2. **Auth middleware** validates JWT → 401 if invalid
3. **ValidationDecorator** runs FluentValidation rules
4. **Handler** executes:
   - Get `UserId` from `ICurrentUserService`
   - Find profile by userId → 404 if not found
   - Update all profile fields from request
   - Set `updatedAt` via `TimeProvider`
   - Save to `profile.profiles`
   - Publish `ProfileUpdatedEvent` via Wolverine
   - Recalculate completeness
   - Return updated profile
5. **LoggingDecorator** logs result

## Inter-module Interactions

### Async Event Published

```csharp
public record ProfileUpdatedEvent(Guid UserId, Guid ProfileId, DateTime UpdatedAt);
```

Published via **Wolverine + RabbitMQ** after successful update.

### Subscribers

| Module | Reaction |
|--------|----------|
| CVGenerator | Invalidate cached profile data if match scoring was computed |

## Diagrams

### Sequence Diagram

```mermaid
sequenceDiagram
    participant C as Client
    participant MW as Auth Middleware
    participant API as Minimal API
    participant V as ValidationDecorator
    participant L as LoggingDecorator
    participant H as Handler
    participant US as ICurrentUserService
    participant DB as ProfileDbContext
    participant W as Wolverine Bus

    C->>MW: PUT /api/profiles/me + Bearer
    MW->>MW: Validate JWT
    alt Invalid
        MW-->>C: 401 Unauthorized
    end
    MW->>API: Request
    API->>V: HandleAsync(request)
    V->>V: Validate
    alt Invalid
        V-->>C: 400 Bad Request
    end
    V->>L: HandleAsync(request)
    L->>H: HandleAsync(request)
    H->>US: Get UserId
    H->>DB: Find profile by userId
    alt Not Found
        H-->>C: 404 Not Found
    end
    H->>H: Update profile fields
    H->>H: Set updatedAt = now
    H->>DB: SaveChangesAsync()
    H->>W: PublishAsync(ProfileUpdatedEvent)
    H->>H: Recalculate completeness
    H-->>L: Result.Success(ProfileResponse)
    L-->>C: 200 OK
```

### Flowchart

```mermaid
flowchart TD
    A[PUT /api/profiles/me] --> B{JWT valid?}
    B -->|No| C[Return 401]
    B -->|Yes| D{Validate}
    D -->|Invalid| E[Return 400]
    D -->|Valid| F{Profile exists?}
    F -->|No| G[Return 404]
    F -->|Yes| H[Update profile fields]
    H --> I[Set updatedAt]
    I --> J[Save to DB]
    J --> K[Publish ProfileUpdatedEvent]
    K --> L[Recalculate completeness]
    L --> M[Return 200]
```

## Error Codes

| Code | HTTP Status | When |
|------|-------------|------|
| `VALIDATION` | 400 | Invalid URL format, field too long |
| `UNAUTHORIZED` | 401 | Missing or invalid JWT |
| `NOT_FOUND` | 404 | User has no profile yet |
