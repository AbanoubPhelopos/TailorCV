# DisableTemplate

## Summary

Admin disables a template by setting `IsActive = false`. Disabled templates are hidden from users but not deleted — they can be reactivated via UpdateTemplate.

## Actor

Admin user (role = `Admin`)

## Request

```
DELETE /api/admin/templates/{id}
Authorization: Bearer {accessToken}
```

## Response

**204 No Content**

(no body)

**401 Unauthorized**

```json
{
  "code": "UNAUTHORIZED",
  "message": "Not authenticated"
}
```

**403 Forbidden**

```json
{
  "code": "FORBIDDEN",
  "message": "Admin access required"
}
```

**404 Not Found**

```json
{
  "code": "NOT_FOUND",
  "message": "Template not found"
}
```

## Business Rules

- Only admin users can disable templates
- Sets `IsActive = false` and updates `UpdatedAt` — does not delete the row
- Disabled templates are hidden from BrowseTemplates and GetTemplate (for non-admin)
- Already-disabled templates return 204 (idempotent)
- Can be reactivated via UpdateTemplate (set `isActive: true`)

## Flow

1. Client sends `DELETE /api/admin/templates/{id}`
2. **Auth middleware** validates JWT → 401 if invalid
3. **Authorization** checks user role is `Admin` → 403 if not
4. **Handler** executes:
   - Find template by id → 404 if not found
   - Set `IsActive = false`
   - Update `UpdatedAt` to current time
   - Save to `templates.templates`
   - Return 204 No Content
5. **LoggingDecorator** logs result

## Inter-module Interactions

**None.** Self-contained within Templates module.

## Diagrams

### Sequence Diagram

```mermaid
sequenceDiagram
    participant C as Admin Client
    participant MW as Auth Middleware
    participant API as Minimal API
    participant L as LoggingDecorator
    participant H as Handler
    participant DB as TemplatesDbContext

    C->>MW: DELETE /api/admin/templates/{id} + Bearer
    MW->>MW: Validate JWT
    alt Invalid Token
        MW-->>C: 401 Unauthorized
    end
    MW->>API: Request
    API->>API: Check user role == Admin
    alt Not Admin
        API-->>C: 403 Forbidden
    end
    API->>L: HandleAsync(request)
    L->>H: HandleAsync(request)
    H->>DB: Find template by id
    alt Not Found
        H-->>L: Result.Failure(NotFoundError)
        L-->>C: 404 Not Found
    end
    H->>H: Set IsActive = false
    H->>DB: SaveChangesAsync()
    H-->>L: Result.Success()
    L->>L: Log success
    L-->>C: 204 No Content
```

### Flowchart

```mermaid
flowchart TD
    A[DELETE /api/admin/templates/id] --> B{JWT valid?}
    B -->|No| C[Return 401]
    B -->|Yes| D{User is Admin?}
    D -->|No| E[Return 403]
    D -->|Yes| F{Template exists?}
    F -->|No| G[Return 404]
    F -->|Yes| H[Set IsActive = false]
    H --> I[Save to DB]
    I --> J[Return 204]
```

## Error Codes

| Code | HTTP Status | When |
|------|-------------|------|
| `UNAUTHORIZED` | 401 | Missing or invalid JWT |
| `FORBIDDEN` | 403 | User is not an admin |
| `NOT_FOUND` | 404 | Template not found |
