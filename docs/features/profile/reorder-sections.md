# ReorderSections

## Summary

User reorders their profile sections globally (across all types — predefined + custom) via a drag-and-drop style update.

## Actor

Authenticated user (profile owner)

## Request

```
PATCH /api/profiles/me/sections/reorder
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "orders": [
    { "sectionId": "guid-1", "order": 1 },
    { "sectionId": "guid-2", "order": 2 },
    { "sectionId": "guid-3", "order": 3 }
  ]
}
```

Client sends the **full desired order** — not a partial update. This makes drag-and-drop simple: frontend sends the complete new arrangement.

## Response

**200 OK**

```json
{
  "orders": [
    { "sectionType": "Experience", "sectionId": "guid-1", "order": 1 },
    { "sectionType": "Custom", "sectionId": "guid-2", "order": 2 },
    { "sectionType": "Skill", "sectionId": "guid-3", "order": 3 }
  ]
}
```

**404 Not Found**

```json
{
  "code": "NOT_FOUND",
  "message": "Profile not found"
}
```

**400 Bad Request**

```json
{
  "code": "VALIDATION",
  "message": "Must include all sections"
}
```

## Validation Rules

| Rule | Description |
|------|-------------|
| All sectionIds present | Must send every sectionId that belongs to the profile |
| Sequential orders | Orders must start from 1 with no gaps |
| No duplicate sectionIds | Each sectionId appears exactly once |
| No duplicate orders | Each order value appears exactly once |
| Non-empty | `orders` array must have at least 1 item |

## Business Rules

- Must send **all** section IDs for the profile (cannot reorder a subset)
- Orders must be sequential starting from 1 (no gaps)
- All sectionIds must belong to the current user's profile
- No duplicate sectionIds or duplicate order values allowed
- This is a single atomic update — all or nothing

## Flow

1. Client sends `PATCH /api/profiles/me/sections/reorder`
2. **Auth middleware** validates JWT
3. **ValidationDecorator** validates: no duplicates, sequential orders, non-empty
4. **Handler** executes:
   - Get `UserId` from `ICurrentUserService`
   - Find profile by userId → 404 if not found
   - Load all `SectionOrder` entries for this profile
   - Verify all requested sectionIds belong to this profile → 400 if mismatch
   - Verify count matches (all sections included) → 400 if missing some
   - Bulk update all `SectionOrder` entries with new order values
   - Save to DB
   - Publish `ProfileUpdatedEvent`
   - Return updated order list
5. **LoggingDecorator** logs result

## Inter-module Interactions

### Async Event Published

```csharp
public record ProfileUpdatedEvent(Guid UserId, Guid ProfileId, DateTime UpdatedAt);
```

Published via **Wolverine + RabbitMQ** after successful reorder.

### Subscribers

| Module | Reaction |
|--------|----------|
| CVGenerator | Invalidate cached profile data |

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

    C->>MW: PATCH .../sections/reorder + Bearer
    MW->>MW: Validate JWT
    MW->>API: Request
    API->>V: HandleAsync(request)
    V->>V: Validate: no duplicates, sequential orders
    alt Validation Failed
        V-->>C: 400 Bad Request
    end
    V->>L: HandleAsync(request)
    L->>H: HandleAsync(request)
    H->>US: Get UserId
    H->>DB: Find profile by userId
    alt Not Found
        H-->>C: 404 Not Found
    end
    H->>DB: Load all SectionOrder entries for profile
    H->>H: Verify all sectionIds belong to profile
    alt Mismatch
        H-->>C: 400 Invalid sectionIds
    end
    H->>H: Verify count matches
    alt Count Mismatch
        H-->>C: 400 Must include all sections
    end
    H->>DB: Bulk update SectionOrder entries
    H->>DB: SaveChangesAsync()
    H->>W: PublishAsync(ProfileUpdatedEvent)
    H-->>L: Result.Success(orders)
    L-->>C: 200 OK
```

### Flowchart

```mermaid
flowchart TD
    A[PATCH /sections/reorder] --> B{JWT valid?}
    B -->|No| C[Return 401]
    B -->|Yes| D{Validate: no dupes, sequential}
    D -->|Invalid| E[Return 400]
    D -->|Valid| F{Profile exists?}
    F -->|No| G[Return 404]
    F -->|Yes| H{All sectionIds belong to profile?}
    H -->|No| I[Return 400 Invalid sections]
    H -->|Yes| J{Count matches?}
    J -->|No| K[Return 400 Must include all]
    J -->|Yes| L[Bulk update SectionOrder]
    L --> M[SaveChangesAsync]
    M --> N[Publish ProfileUpdatedEvent]
    N --> O[Return 200 with new order]
```

## Error Codes

| Code | HTTP Status | When |
|------|-------------|------|
| `VALIDATION` | 400 | Duplicate IDs/orders, non-sequential, missing sections |
| `UNAUTHORIZED` | 401 | Missing or invalid JWT |
| `NOT_FOUND` | 404 | Profile not found |
