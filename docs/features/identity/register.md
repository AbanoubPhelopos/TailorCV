# Register

## Summary

Visitor registers with email, password, first name, last name → gets JWT access token + refresh token.

## Actor

Anonymous visitor (no auth required)

## Request

```
POST /api/auth/register
Content-Type: application/json

{
  "email": "string",
  "password": "string",
  "firstName": "string",
  "lastName": "string"
}
```

## Response

**201 Created**

```json
{
  "userId": "guid",
  "accessToken": "string (JWT)",
  "refreshToken": "string"
}
```

**400 Bad Request**

```json
{
  "code": "VALIDATION",
  "message": "Email is invalid; Password must contain..."
}
```

**409 Conflict**

```json
{
  "code": "CONFLICT",
  "message": "A user with this email already exists"
}
```

## Validation Rules

| Field | Rules |
|-------|-------|
| Email | Required, valid email format, max 256 chars |
| Password | Required, min 8 chars, at least 1 uppercase, 1 lowercase, 1 digit, 1 special char |
| FirstName | Required, max 100 chars |
| LastName | Required, max 100 chars |

## Business Rules

- Email must be unique (case-insensitive)
- Password stored as BCrypt hash (never plaintext)
- User role defaults to `User` (not Admin)
- On registration, a `RefreshToken` is automatically created
- Access token expiry: 15 minutes
- Refresh token expiry: 7 days

## Flow

1. Client sends `POST /api/auth/register`
2. **ValidationDecorator** runs FluentValidation rules
3. **Handler** executes:
   - Check if email exists in DB → return 409 if duplicate
   - Hash password with BCrypt
   - Create `User` entity (email, hash, firstName, lastName, role=User, createdAt)
   - Create `RefreshToken` entity (userId, token=GUID, expiresAt=now+7days)
   - Save both to `identity.users` and `identity.refresh_tokens`
   - Generate JWT access token via `JwtService`
   - Return `AuthResponse`
4. **LoggingDecorator** logs result

## Inter-module Interactions

**None.** Register is fully self-contained within the Identity module.

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

    C->>API: POST /api/auth/register
    API->>V: HandleAsync(request)
    V->>V: Validate Request
    alt Validation Failed
        V-->>API: Result.Failure(ValidationError)
        API-->>C: 400 Bad Request
    end
    V->>L: HandleAsync(request)
    L->>H: HandleAsync(request)
    H->>DB: AnyAsync(email exists?)
    alt Email Exists
        H-->>L: Result.Failure(ConflictError)
        L-->>V: Result.Failure
        V-->>API: Result.Failure(ConflictError)
        API-->>C: 409 Conflict
    end
    H->>H: BCrypt.HashPassword(password)
    H->>DB: Users.Add(user)
    H->>DB: RefreshTokens.Add(refreshToken)
    H->>DB: SaveChangesAsync()
    H->>JWT: GenerateAccessToken(user)
    JWT-->>H: accessToken string
    H-->>L: Result.Success(AuthResponse)
    L->>L: Log success
    L-->>V: Result.Success(AuthResponse)
    V-->>API: Result.Success(AuthResponse)
    API-->>C: 201 Created { userId, accessToken, refreshToken }
```

### Flowchart

```mermaid
flowchart TD
    A[POST /api/auth/register] --> B{Validate Request}
    B -->|Invalid| C[Return 400 Validation Error]
    B -->|Valid| D{Email exists in DB?}
    D -->|Yes| E[Return 409 Conflict]
    D -->|No| F[Hash password with BCrypt]
    F --> G[Create User entity]
    G --> H[Create RefreshToken entity]
    H --> I[Save to DB]
    I --> J{Save successful?}
    J -->|No| K[Return 500 Internal Error]
    J -->|Yes| L[Generate JWT access token]
    L --> M[Return 201 AuthResponse]
```

### Component Diagram

```mermaid
graph LR
    subgraph "Identity Module"
        A[Register Feature]
        B[User Entity]
        C[RefreshToken Entity]
        D[JwtService]
        E[IdentityDbContext]
    end

    subgraph "Shared Kernel"
        F[ICommandHandler]
        G[Result T]
        H[ValidationDecorator]
        I[LoggingDecorator]
    end

    A --> B
    A --> C
    A --> D
    A --> E
    A --> F
    A --> G
    H --> A
    I --> H
```

## Error Codes

| Code | HTTP Status | When |
|------|-------------|------|
| `VALIDATION` | 400 | Invalid email, weak password, empty names |
| `CONFLICT` | 409 | Email already registered |

## Security Considerations

- Password never logged or returned in response
- BCrypt work factor: 12 (adjustable)
- Rate limiting on this endpoint (prevent brute-force registration)
- Email normalization (trim + lowercase) before uniqueness check
