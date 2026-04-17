# CreateProfile

## Summary

Authenticated user creates their single professional profile. Sections (predefined + custom) are added separately via Section CRUD features.

## Actor

Authenticated user (no existing profile)

## Request

```
POST /api/profiles
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

**201 Created**

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
  "completeness": 10,
  "sections": [],
  "createdAt": "datetime"
}
```

**409 Conflict**

```json
{
  "code": "CONFLICT",
  "message": "Profile already exists for this user"
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

- One profile per user — returns 409 if profile already exists
- Profile starts empty (sections added separately via Section CRUD features)
- `UserId` is extracted from JWT claims via `ICurrentUserService` (not from request body)
- Initial completeness is calculated based on provided fields (sections not counted yet)

## Flow

1. Client sends `POST /api/profiles`
2. **Auth middleware** validates JWT → 401 if invalid
3. **ValidationDecorator** runs FluentValidation rules
4. **Handler** executes:
   - Get `UserId` from `ICurrentUserService`
   - Check if profile already exists for this user → return 409
   - Create `Profile` entity with all provided fields
   - Save to `profile.profiles`
   - Calculate initial completeness
   - Return created profile with completeness
5. **LoggingDecorator** logs result

## Inter-module Interactions

**None.** Profile creation is self-contained.

**Future:** May publish `ProfileCreatedEvent` via Wolverine when other modules need to react (e.g., Dashboard pre-population).

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

    C->>MW: POST /api/profiles + Bearer token
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
    H->>US: Get UserId from claims
    US-->>H: userId
    H->>DB: AnyAsync(userId exists?)
    alt Profile Exists
        H-->>L: Result.Failure(ConflictError)
        L-->>API: Result.Failure
        API-->>C: 409 Conflict
    end
    H->>H: Create Profile entity
    H->>DB: Profiles.Add(profile)
    H->>DB: SaveChangesAsync()
    H->>H: Calculate completeness
    H-->>L: Result.Success(ProfileResponse)
    L->>L: Log success
    L-->>API: Result.Success
    API-->>C: 201 Created { profile }
```

### Flowchart

```mermaid
flowchart TD
    A[POST /api/profiles] --> B{JWT valid?}
    B -->|No| C[Return 401]
    B -->|Yes| D{Validate request}
    D -->|Invalid| E[Return 400]
    D -->|Valid| F{Profile exists for user?}
    F -->|Yes| G[Return 409 Conflict]
    F -->|No| H[Create Profile entity]
    H --> I[Save to DB]
    I --> J[Calculate completeness]
    J --> K[Return 201 ProfileResponse]
```

### ER Diagram — Full Profile Module

```mermaid
erDiagram
    PROFILE ||--o{ EXPERIENCE : has
    PROFILE ||--o{ PROJECT : has
    PROFILE ||--o{ SKILL : has
    PROFILE ||--o{ EDUCATION : has
    PROFILE ||--o{ CERTIFICATION : has
    PROFILE ||--o{ LANGUAGE : has
    PROFILE ||--o{ CUSTOM_SECTION : has
    PROFILE ||--o{ SECTION_ORDER : orders

    PROFILE {
        guid id PK
        guid userId UK
        string headline
        string summary
        string phone
        string location
        string website
        string linkedinUrl
        string githubUrl
        string shareId
        bool isShared
        datetime createdAt
        datetime updatedAt
    }

    EXPERIENCE {
        guid id PK
        guid profileId FK
        string company
        string role
        date startDate
        date endDate
        string description
        bool isCurrent
    }

    PROJECT {
        guid id PK
        guid profileId FK
        string name
        string description
        jsonb techStack
        string role
        string url
        date startDate
        date endDate
    }

    SKILL {
        guid id PK
        guid profileId FK
        string category
        jsonb items
    }

    EDUCATION {
        guid id PK
        guid profileId FK
        string institution
        string degree
        string field
        date startDate
        date endDate
        string gpa
    }

    CERTIFICATION {
        guid id PK
        guid profileId FK
        string name
        string issuer
        date date
        date expiryDate
        string url
    }

    LANGUAGE {
        guid id PK
        guid profileId FK
        string languageName
        string proficiency
    }

    CUSTOM_SECTION {
        guid id PK
        guid profileId FK
        string title
        jsonb items
    }

    SECTION_ORDER {
        guid id PK
        guid profileId FK
        string sectionType
        guid sectionId
        int order
    }
```

## Error Codes

| Code | HTTP Status | When |
|------|-------------|------|
| `VALIDATION` | 400 | Invalid URL format, field too long |
| `UNAUTHORIZED` | 401 | Missing or invalid JWT |
| `CONFLICT` | 409 | User already has a profile |
