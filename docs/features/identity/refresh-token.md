# RefreshToken

## Summary

User sends a valid refresh token → gets a new JWT access token + new refresh token pair (simple rotation).

## Actor

Authenticated user (has a valid refresh token)

## Request

```
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshTokenValue": "string"
}
```

## Response

**200 OK**

```json
{
  "userId": "guid",
  "accessToken": "string (JWT)",
  "refreshToken": "string"
}
```

**401 Unauthorized**

```json
{
  "code": "REFRESH_TOKEN_NOT_FOUND",
  "message": "Invalid or expired refresh token"
}
```

> Expired tokens return `REFRESH_TOKEN_EXPIRED` (401). Deleted users return `USER_DELETED` (401).

## Validation Rules

| Field | Rules |
|-------|-------|
| RefreshTokenValue | Required, non-empty string |

## Business Rules

- Look up refresh token in DB
- If token not found → 401
- If token is expired (`expiresAt < now`) → 401
- On success: create a **new** refresh token, **delete** the old one (simple rotation — not revocation/blacklist)
- Generate new JWT access token
- No revocation/blacklist mechanism in Phase 1 — if a refresh token is valid and not expired, it works

## Flow

1. Client sends `POST /api/auth/refresh`
2. **ValidationDecorator** runs FluentValidation rules
3. **Handler** executes:
   - Look up refresh token in `identity.refresh_tokens`
   - If not found → return 401
   - If expired → return 401
   - Look up associated user
   - If user not found → return 401 (edge case: user deleted)
   - Delete old refresh token
   - Create new `RefreshToken` entity (new GUID, new expiry)
   - Save changes
   - Generate new JWT access token via `JwtService`
   - Return `AuthResponse`
4. **LoggingDecorator** logs result

## Inter-module Interactions

**None.** Self-contained within Identity module.

## Diagrams

### Sequence Diagram

```mermaid
sequenceDiagram
    participant C as Client
    participant API as Minimal API
    participant V as ValidationDecorator
    participant L as LoggingDecorator
    participant H as Handler
    participant DB as IdentityDbContext
    participant JWT as JwtService

    C->>API: POST /api/auth/refresh
    API->>V: HandleAsync(request)
    V->>V: Validate Request
    alt Validation Failed
        V-->>API: Result.Failure(ValidationError)
        API-->>C: 400 Bad Request
    end
    V->>L: HandleAsync(request)
    L->>H: HandleAsync(request)
    H->>DB: Find refresh token
    alt Token Not Found
        H-->>L: Result.Failure(UnauthorizedError)
        L-->>API: Result.Failure
        API-->>C: 401 Unauthorized
    end
    H->>H: Check if expired
    alt Token Expired
        H-->>L: Result.Failure(UnauthorizedError)
        L-->>API: Result.Failure
        API-->>C: 401 Unauthorized
    end
    H->>DB: Find user by token.UserId
    H->>DB: Remove old refresh token
    H->>DB: Add new refresh token
    H->>DB: SaveChangesAsync()
    H->>JWT: GenerateAccessToken(user)
    JWT-->>H: accessToken string
    H-->>L: Result.Success(AuthResponse)
    L->>L: Log success
    L-->>API: Result.Success(AuthResponse)
    API-->>C: 200 OK { userId, accessToken, refreshToken }
```

### Flowchart

```mermaid
flowchart TD
    A[POST /api/auth/refresh] --> B{Validate Request}
    B -->|Invalid| C[Return 400 Validation Error]
    B -->|Valid| D{Refresh token exists?}
    D -->|No| E[Return 401 Unauthorized]
    D -->|Yes| F{Token expired?}
    F -->|Yes| E
    F -->|No| G{User exists?}
    G -->|No| E
    G -->|Yes| H[Remove old refresh token]
    H --> I[Create new refresh token]
    I --> J[Save to DB]
    J --> K[Generate JWT access token]
    K --> L[Return 200 AuthResponse]
```

### State Diagram — Refresh Token Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Created: User registers or logs in
    Created --> Active: Token is valid and not expired
    Active --> Used: Token refreshed successfully
    Used --> Deleted: Old token removed from DB
    Active --> Expired: expiresAt less than now
    Expired --> [*]: Cleaned up later
    Deleted --> [*]
```

## Error Codes

| Code | HTTP Status | When |
|------|-------------|------|
| `VALIDATION` | 400 | Missing or empty refresh token value |
| `REFRESH_TOKEN_NOT_FOUND` | 404 | Token not found in database |
| `REFRESH_TOKEN_EXPIRED` | 401 | Token has expired |
| `USER_DELETED` | 401 | Associated user no longer exists |

## Security Considerations

- Old refresh token is deleted on rotation (prevents replay)
- No blacklist needed in Phase 1 — rotation + expiry is sufficient
- Rate limiting on this endpoint
- Refresh tokens are single-use by design (old one deleted)
