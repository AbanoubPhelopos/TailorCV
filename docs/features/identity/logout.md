# Logout

## Summary

User logs out. In Phase 1, this is client-side only — the client discards both tokens. The endpoint exists for API completeness and future server-side revocation.

## Actor

Authenticated user

## Request

```
POST /api/auth/logout
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "refreshToken": "string"
}
```

## Response

**200 OK**

```
(Empty response body)
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
| RefreshToken | Required, non-empty string |

## Business Rules

- **Phase 1 (MVP):** Client discards tokens. Endpoint is a no-op on the server — just returns 200.
- **Future:** Server-side refresh token revocation/blacklist will be added. The endpoint signature stays the same — only the handler logic changes.
- Requires valid JWT access token (authenticated)

## Flow

1. Client sends `POST /api/auth/logout` with refresh token
2. **Auth middleware** validates JWT → 401 if invalid
3. **ValidationDecorator** runs FluentValidation rules
4. **Handler** executes (Phase 1):
   - No-op: return success immediately
5. Client discards both `accessToken` and `refreshToken` locally
6. **LoggingDecorator** logs logout event

## Inter-module Interactions

**None.** Self-contained within Identity module.

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

    C->>MW: POST /api/auth/logout + Bearer token
    MW->>MW: Validate JWT
    alt Invalid Token
        MW-->>C: 401 Unauthorized
    end
    MW->>API: Authenticated request
    API->>V: HandleAsync(request)
    V->>V: Validate Request
    alt Validation Failed
        V-->>API: Result.Failure(ValidationError)
        API-->>C: 400 Bad Request
    end
    V->>L: HandleAsync(request)
    L->>H: HandleAsync(request)
    Note over H: Phase 1: No-op
    H-->>L: Result.Success()
    L->>L: Log logout
    L-->>API: Result.Success()
    API-->>C: 200 OK
    Note over C: Discard both tokens locally
```

### Flowchart — Phase 1 vs Future

```mermaid
flowchart TD
    A[POST /api/auth/logout] --> B{JWT valid?}
    B -->|No| C[Return 401 Unauthorized]
    B -->|Yes| D{Validate request}
    D -->|Invalid| E[Return 400]
    D -->|Valid| F{Phase?}
    F -->|Phase 1 MVP| G[Return 200 - no-op]
    F -->|Phase 2 Future| H[Delete refresh token from DB]
    H --> I[Add to blacklist if needed]
    I --> G
    G --> J[Client discards tokens]
```

## Error Codes

| Code | HTTP Status | When |
|------|-------------|------|
| `VALIDATION` | 400 | Missing or empty refresh token |
| `UNAUTHORIZED` | 401 | Invalid or missing access token |

## Design Note

The endpoint exists now so the API contract is stable. When server-side revocation is implemented in Phase 2, only the handler body changes — no endpoint or client changes needed.
