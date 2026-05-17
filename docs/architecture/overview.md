# TailorCV — Architecture Overview

## Table of Contents

- [Architecture Style](#architecture-style)
- [Module Communication](#module-communication)
- [Project Structure](#project-structure)
- [Feature File Pattern](#feature-file-pattern)
- [Shared Kernel](#shared-kernel)
- [Module Details](#module-details)
- [Technology Stack](#technology-stack)
- [Cross-cutting Concerns](#cross-cutting-concerns)
- [Database Design](#database-design)
- [gRPC Contracts](#grpc-contracts)
- [Event Flow](#event-flow)
- [Microservice Readiness](#microservice-readiness)

---

## Architecture Style

**Modular Monolith** with **Vertical Slice Architecture**.

- Each module is a self-contained unit owning its own data, logic, and endpoints
- Modules communicate via **async events** (Wolverine + RabbitMQ) or **sync gRPC** calls
- No in-process interface calls between modules — everything goes through messaging or gRPC
- Designed for future extraction into microservices with minimal changes

### Key Principles

1. **One file per feature** — each feature is a single `.cs` static class file
2. **No repository pattern** — handlers use `DbContext` directly
3. **No clean architecture layers** — vertical slices group all concerns per feature
4. **Separate DB schemas per module** — true data isolation
5. **Result pattern** for error handling — no exceptions for business logic flow
6. **Async-first** — use Wolverine events for cross-module communication; gRPC only when synchronous is required

---

## Module Communication

```
┌──────────────────────────────────────────────────────────────┐
│                       TailorCV.Api (Host)                     │
│              Minimal APIs + Wolverine + gRPC                  │
├──────────┬──────────┬────────────┬────────────┬──────────────┤
│ Identity │ Profile  │ JobDescriptions │ Templates  │ CVGenerator  │
├──────────┴──────────┴────────────┴────────────┴──────────────┤
│                                                               │
│   Async: Wolverine + RabbitMQ (events, sagas, outbox)        │
│   Sync:  gRPC (real-time data fetch between modules)          │
│                                                               │
├──────────────────────────────────────────────────────────────┤
│     PostgreSQL (EF Core, separate schemas per module)         │
├──────────────────────────────────────────────────────────────┤
│     Redis │ Serilog │ OpenTelemetry │ Wolverine │
└──────────────────────────────────────────────────────────────┘
```

### Communication Rules

| Scenario | Pattern | Example |
|----------|---------|---------|
| Module publishes event | Wolverine + RabbitMQ | Identity publishes `UserRegistered` |
| Module reacts to event | Wolverine handler | Profile listens to `UserRegistered` |
| Module needs data in real-time | gRPC | CVGenerator fetches Profile data during generation |
| Module needs data async | Event-driven | Profile publishes `ProfileUpdated` → CVGenerator listens (future) |
| Long-running process | Wolverine saga | CV generation pipeline (scrape → parse → generate → export) |

### When to Use What

**Use async (Wolverine + RabbitMQ) when:**
- The consumer doesn't need an immediate response
- Fire-and-forget notifications (CV generated, profile updated)
- Event-driven side effects (update dashboard, send notifications)
- Saga orchestration across multiple modules

**Use sync (gRPC) when:**
- The caller needs data immediately to proceed
- Request/response pattern with strict latency requirements
- e.g., CVGenerator needs full profile + JD data before generating

---

## Project Structure

```
TailorCV/
├── .editorconfig                                 # var banned, braces required, SonarAnalyzer
├── Directory.Packages.props                      # Central package management (CPM)
├── TailorCV.slnx                                 # Solution file
├── src/
│   ├── Directory.Build.props                     # net10.0, SonarAnalyzer, nullable, treat warnings as errors
│   │
│   ├── TailorCV.Api/                            # Host / entry point
│   │   ├── Program.cs                           # App bootstrap, module registration, OpenAPI/Scalar
│   │   ├── appsettings.json                     # Connection strings, JWT, Redis, RustFS, Serilog config
│   │   ├── Middleware/
│   │   │   └── ExceptionMiddleware.cs           # Unhandled exceptions → JSON error response
│   │   ├── OpenApi/
│   │   │   └── BearerSecuritySchemeTransformer.cs  # JWT Bearer OpenAPI security scheme
│   │   ├── Services/
│   │   │   ├── CurrentUserService.cs            # ICurrentUserService impl (claims extraction)
│   │   │   └── DateTimeProvider.cs              # IDateTimeProvider impl (TimeProvider.System)
│   │   └── Properties/
│   │       └── launchSettings.json              # Dev ports, environment
│   │
│   ├── TailorCV.Shared/                         # Shared kernel (no module dependencies)
│   │   ├── CQRS/
│   │   │   ├── ICommandHandler.cs               # ICommandHandler<TCommand, TResult>
│   │   │   ├── IQueryHandler.cs                 # IQueryHandler<TQuery, TResult>
│   │   │   ├── ValidationDecorator.cs           # CommandValidationDecorator + QueryValidationDecorator
│   │   │   └── LoggingDecorator.cs              # CommandLoggingDecorator + QueryLoggingDecorator
│   │   ├── Results/
│   │   │   ├── Result.cs                        # Result + Result<T>
│   │   │   ├── Error.cs                         # Error record + ErrorType enum + ToHttpStatusCode()
│   │   │   └── ResultExtensions.cs              # ToProblemDetails() → IResult
│   │   ├── Pagination/
│   │   │   └── OffsetPagedList.cs               # OffsetPagedList<T> + PagingInfo
│   │   ├── Interfaces/
│   │   │   ├── ICurrentUserService.cs           # UserId, Email, Role, IsAuthenticated
│   │   │   └── IDateTimeProvider.cs             # DateTimeOffset UtcNow
│   │   └── Primitives/
│   │       └── Entity.cs                        # Base entity (Guid.CreateVersion7())
│   │
│   ├── TailorCV.Infrastructure/                 # Shared infrastructure
│   │   ├── Persistence/MigrationExtensions.cs
│   │   ├── Storage/S3Service.cs + S3Configuration.cs
│   │   ├── OpenAi/OpenAiClientExtensions.cs
│   │   ├── Caching/RedisConfiguration.cs + CacheService.cs
│   │   └── Logging/SerilogConfiguration.cs
│   │
│   ├── protos/                                  # gRPC contracts
│   │   ├── profile.proto
│   │   ├── jobscraper.proto
│   │   └── templates.proto
│   │
│   └── Modules/
│       ├── Identity/
│       │   ├── TailorCV.Identity/               # FULLY IMPLEMENTED
│       │   │   ├── Features/
│       │   │   │   ├── Register.cs
│       │   │   │   ├── UpdateUserName.cs
│       │   │   │   ├── Login.cs
│       │   │   │   ├── RefreshToken.cs
│       │   │   │   └── Logout.cs
│       │   │   ├── Domain/
│       │   │   │   ├── User.cs                  # Rich entity, static Create() factory
│       │   │   │   ├── RefreshToken.cs          # Entity, Create(userId, now), IsExpired(now)
│       │   │   │   ├── IdentityErrors.cs        # Centralized business error codes
│       │   │   │   └── Enums/UserRole.cs
│       │   │   ├── Infrastructure/
│       │   │   │   ├── IdentityDbContext.cs     # schema: "identity"
│       │   │   │   ├── IdentityDbContextFactory.cs  # Design-time factory for dotnet ef
│       │   │   │   ├── PasswordHasher.cs        # PBKDF2 (Rfc2898DeriveBytes), built-in .NET
│       │   │   │   ├── JwtService.cs            # IJwtService + JwtSettings + JwtService
│       │   │   │   ├── Configurations/
│       │   │   │   │   ├── UserConfiguration.cs
│       │   │   │   │   └── RefreshTokenConfiguration.cs
│       │   │   │   └── Migrations/              # EF Core generated (InitialCreate)
│       │   │   └── ModuleExtensions.cs          # AddIdentityModule(), TryDecorate(), MigrateIdentityModuleAsync()
│       │   │   ├── IdentityWolverineExtension.cs  # Publishes UserRegistered/UserNameUpdated to identity.events
│       │   │   └── AssemblyInfo.cs             # [WolverineModule] for handler discovery
│       │   └── TailorCV.Identity.Contracts/
│       │       └── Events/
│       │           ├── UserRegistered.cs
│       │           └── UserNameUpdated.cs
│       │
│       ├── Profile/
│       │   ├── TailorCV.Profile/                # FULLY IMPLEMENTED
│       │   │   ├── Features/
│       │   │   │   ├── GetSharedProfile.cs     # Includes firstName/lastName from local ProfileUser
│       │   │   ├── GetProfile.cs
│       │   │   │   ├── CreateProfile.cs
│       │   │   │   ├── UpdateProfile.cs
│       │   │   │   ├── UpdateSections.cs
│       │   │   │   ├── ShareProfile.cs
│       │   │   │   ├── GetCompleteness.cs
│       │   │   │   ├── ExportProfile.cs
│       │   │   │   ├── ImportResumeGetUploadUrl.cs
│       │   │   │   ├── ImportResumeParse.cs
│       │   │   │   ├── ImportResumeParseStatus.cs
│       │   │   │   ├── ImportResumeConfirm.cs
│       │   │   │   └── GetSharedProfile.cs
│       │   │   ├── Domain/
│       │   │   │   ├── Profile.cs, Experience.cs, Project.cs, Skill.cs, Education.cs, Certification.cs, Language.cs
│       │   │   │   └── ProfileUser.cs           # UserId, FirstName, LastName (populated via events)
│       │   │   ├── Events/
│       │   │   │   ├── UserRegisteredHandler.cs  # Creates ProfileUser on UserRegistered
│       │   │   │   ├── UserNameUpdatedHandler.cs  # Updates ProfileUser on UserNameUpdated
│       │   │   │   ├── ResumeParsingCompletedHandler.cs
│       │   │   │   └── ResumeParsingFailedHandler.cs
│       │   │   ├── Infrastructure/
│       │   │   │   ├── ProfileDbContext.cs      # schema: "profile"
│       │   │   │   ├── ProfileDbContextFactory.cs
│       │   │   │   ├── Configurations/          # EF Core entity configs
│       │   │   │   │   ├── ProfileConfiguration.cs
│       │   │   │   │   ├── ProfileUserConfiguration.cs
│       │   │   │   │   └── ...
│       │   │   │   └── Migrations/
│       │   │   ├── ProfileWolverineExtension.cs  # Publishes ProfileUpdated, listens on identity.events + profile.events
│       │   │   └── ModuleExtensions.cs          # AddProfileModule(), MigrateProfileModuleAsync()
│       │   └── TailorCV.Profile.Contracts/
│       │       └── Events/
│       │           ├── ProfileUpdated.cs
│       │           ├── ResumeParsingCompleted.cs
│       │           └── ResumeParsingFailed.cs
│       │
│       ├── JobDescriptions/
│       │   ├── TailorCV.JobDescriptions/               # FULLY IMPLEMENTED
│       │   │   ├── Features/
│       │   │   │   ├── ParseJobDescription.cs
│       │   │   │   ├── ScrapeJobDescription.cs
│       │   │   │   ├── GetParseStatus.cs
│       │   │   │   ├── SaveJobDescription.cs
│       │   │   │   ├── ListJobs.cs
│       │   │   │   └── GetJob.cs
│       │   │   ├── Domain/
│       │   │   │   ├── JobDescription.cs              # Rich entity, static Create()
│       │   │   │   ├── ParseJob.cs                     # Entity for async parse tracking
│       │   │   │   ├── JobDescriptionErrors.cs         # Centralized business error codes
│       │   │   │   └── Enums/                          # ParseJobStatus, ParseJobType, SeniorityLevel
│       │   │   ├── Infrastructure/
│       │   │   │   ├── JobDescriptionsDbContext.cs     # schema: "jobdescriptions"
│       │   │   │   ├── JobDescriptionDbContextFactory.cs  # Design-time factory
│       │   │   │   ├── Configurations/                # EF Core entity configs
│       │   │   │   └── Migrations/                     # EF Core migrations
│       │   │   ├── Events/                             # Wolverine event handlers
│       │   │   │   ├── JobParsingCompletedHandler.cs
│       │   │   │   └── JobParsingFailedHandler.cs
│       │   │   └── ModuleExtensions.cs                # AddJobDescriptionsModule(), MigrateJobDescriptionsModuleAsync()
│       │   ├── TailorCV.JobDescriptions.Contracts/
│       │   │   ├── Commands/
│       │   │   │   ├── ParseJobText.cs                 # Publish to worker queue
│       │   │   │   └── ScrapeJobUrl.cs                  # Publish to worker queue
│       │   │   ├── Dto/
│       │   │   │   └── ParsedJobDataDto.cs
│       │   │   └── Events/
│       │   │       ├── JobParsingCompleted.cs          # Published by worker
│       │   │       └── JobParsingFailed.cs              # Published by worker
│       │   │
│       │   └── TailorCV.JobDescriptions.Worker/        # Wolverine host, separate process
│       │       ├── Program.cs                          # Wolverine + RabbitMQ config
│       │       ├── ModuleExtensions.cs                 # DI for AI, scraping, rate limiting
│       │       ├── Handlers/
│       │       │   ├── ParseJobTextHandler.cs           # OpenAI parsing
│       │       │   └── ScrapeJobUrlHandler.cs           # Playwright scraping → chains to ParseJobText
│       │       └── Infrastructure/
│       │           ├── AI/                              # OpenAI integration
│       │           │   ├── IJobDescriptionParserService.cs
│       │           │   ├── OpenAiJobParserService.cs
│       │           │   └── OpenAiOptions.cs
│       │           ├── Scraping/                        # Playwright headless browser
│       │           │   ├── IPlaywrightScrapingService.cs
│       │           │   ├── PlaywrightScrapingService.cs
│       │           │   └── PlaywrightOptions.cs
│       │           └── RateLimiting/                    # Per-domain rate limiting
│       │               ├── DomainExtractor.cs
│       │               └── DomainRateLimiter.cs
│       │
│       ├── Templates/
│       │   └── TailorCV.Templates/              # DIRECTORY ONLY (no .cs source files)
│       │
│       └── CVGenerator/
│           └── TailorCV.CVGenerator/            # DIRECTORY ONLY (no .cs source files)
│
├── infra/
│   ├── Dockerfile
│   ├── docker-compose.yml                       # PostgreSQL, RabbitMQ, Redis, RustFS, OTel, Prometheus, Loki, Tempo, Grafana
│   ├── otel-collector/config.yaml
│   ├── prometheus/prometheus.yml
│   ├── loki/config.yaml
│   ├── tempo/config.yaml
│   └── grafana/provisioning/datasources/datasources.yaml
│
├── docs/                                        # Architecture + feature documentation
│   ├── architecture/
│   │   ├── overview.md                          # This file
│   │   └── project-dependencies.md
│   └── features/
│       ├── overview.md
│       ├── full.md
│       ├── identity/ (register, login, refresh-token, logout)
│       ├── profile/ (10 feature docs)
│   └── job-descriptions/ (6 feature docs)
│
├── tests/                                       # NOT YET CREATED
└── frontend/                                    # NOT YET CREATED
```

---

## Module Contracts

Each module that publishes integration events or exposes DTOs for cross-module communication has a dedicated `.Contracts` project. These projects contain **only** plain data types — no logic, no dependencies.

### Why Separate Contracts Projects?

- **Microservice readiness:** When splitting a module into a separate service, its Contracts project becomes a shared NuGet package that other services reference. No code changes needed in consumers.
- **Dependency isolation:** Modules never reference each other's full project — only Contracts. This prevents accidental coupling to internal domain or infrastructure code.
- **Versioning:** Contracts can be versioned independently. A consumer only needs to update when the event schema changes.

### Contracts Project Contents

```
TailorCV.Identity.Contracts/
└── Events/
    ├── UserRegistered.cs           # Guid UserId, string FirstName, string LastName
    └── UserNameUpdated.cs          # Guid UserId, string FirstName, string LastName

TailorCV.Profile.Contracts/
└── Events/
    ├── ProfileUpdated.cs            # Guid UserId, Guid ProfileId, DateTimeOffset UpdatedAt
    ├── ResumeParsingCompleted.cs     # Guid ParseJobId, ParsedResumeData Data
    └── ResumeParsingFailed.cs       # Guid ParseJobId, string Error

TailorCV.JobDescriptions.Contracts/
└── Events/
    ├── JobParsingCompleted.cs       # Guid ParseJobId, ParsedJobDataDto Data, string? RawText, Uri? SourceUrl
    └── JobParsingFailed.cs          # Guid ParseJobId, string Error

TailorCV.Templates.Contracts/
└── Events/                           # (currently empty — Templates don't publish events)

TailorCV.CVGenerator.Contracts/
└── Events/
    ├── CVTailoringCompleted.cs      # Guid CVId, ...
    ├── CVTailoringFailed.cs          # Guid CVId, string Error
    ├── CoverLetterCompleted.cs      # Guid CVId, ...
    ├── CoverLetterFailed.cs         # Guid CVId, string Error
    ├── CvPdfExportCompleted.cs      # Guid CVId, ...
    └── CvPdfExportFailed.cs         # Guid CVId, string Error
```

### Contracts Project Dependencies (.csproj)

```xml
<!-- TailorCV.CVGenerator.csproj — heaviest consumer of contracts -->
<ItemGroup>
  <ProjectReference Include="..\Profile\TailorCV.Profile.Contracts\TailorCV.Profile.Contracts.csproj" />
  <ProjectReference Include="..\JobDescriptions\TailorCV.JobDescriptions.Contracts\TailorCV.JobDescriptions.Contracts.csproj" />
  <ProjectReference Include="..\Templates\TailorCV.Templates.Contracts\TailorCV.Templates.Contracts.csproj" />
</ItemGroup>

<!-- TailorCV.Profile.csproj — needs Identity contracts for user events -->
<ItemGroup>
  <ProjectReference Include="..\Identity\TailorCV.Identity.Contracts\TailorCV.Identity.Contracts.csproj" />
</ItemGroup>

<!-- Contracts projects themselves — NO references to other TailorCV projects, NO NuGet packages except Google.Protobuf/Grpc.Tools for proto generation -->
<!-- Example: TailorCV.Profile.Contracts.csproj -->
<ItemGroup>
  <ProjectReference Include="..\..\..\TailorCV.Shared\TailorCV.Shared.csproj" />
  <PackageReference Include="Google.Protobuf" />
  <PackageReference Include="Grpc.AspNetCore" />
  <PackageReference Include="Grpc.Tools" />
</ItemGroup>
```

### Dependency Graph

```
                    ┌─────────────────────┐
                    │ TailorCV.Shared      │
                    │ (Primitives, Results) │
                    └──────────┬───────────┘
                               │ (referenced by ALL projects)
              ┌────────────────┼────────────────┐
              │                │                │
    ┌─────────▼──────┐  ┌─────▼──────┐  ┌──────▼──────────┐
    │  Identity       │  │  Profile   │  │  JobDescriptions     │
    │  .Contracts     │  │  .Contracts│  │  .Contracts     │
    └───────┬────────┘  └─────┬──────┘  └──────┬──────────┘
            │                 │                │
            │                 │                │
            ▼                 ▼                ▼
    ┌───────────────┐  ┌──────────────────────────────┐
    │  Profile       │  │  CVGenerator                  │
    │  (full module) │  │  (full module)                │
    │                │  │  references:                  │
    │                │  │    Profile.Contracts           │
    │                │      │  JobDescriptions.Contracts        │
    │                │  │    Templates.Contracts         │
    └───────────────┘  └──────────────────────────────┘
```

---

## Feature File Pattern

Each feature is a **single `.cs` file** containing everything for that use case. The file is a static class with nested types.

### Command Feature Example

```csharp
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TailorCV.Identity.Domain;
using TailorCV.Identity.Infrastructure;
using TailorCV.Shared.Primitives;
using TailorCV.Shared.Results;

namespace TailorCV.Identity.Features;

public static class Register
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", async (
                Request request,
                ICommandHandler<Request, AuthResponse> handler,
                CancellationToken ct) =>
            {
                Result<AuthResponse> result = await handler.HandleAsync(request, ct);

                return result.IsSuccess
                    ? Results.Created($"/api/auth/users/{result.Value.UserId}", result.Value)
                    : Results.BadRequest(result.Error);
            })
            .WithName("Register")
            .WithTags("Auth")
            .AllowAnonymous();
    }

    public record Request(
        string Email,
        string Password,
        string FirstName,
        string LastName);

    public record AuthResponse(
        Guid UserId,
        string AccessToken,
        string RefreshToken);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Must(p => p.Any(char.IsUpper))
                .Must(p => p.Any(char.IsLower))
                .Must(p => p.Any(char.IsDigit))
                .Must(p => p.Any(c => !char.IsLetterOrDigit(c)));
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        }
    }

    public class Handler : ICommandHandler<Request, AuthResponse>
    {
        private readonly IdentityDbContext _dbContext;
        private readonly JwtService _jwtService;
        private readonly TimeProvider _timeProvider;

        public Handler(
            IdentityDbContext dbContext,
            JwtService jwtService,
            TimeProvider timeProvider)
        {
            _dbContext = dbContext;
            _jwtService = jwtService;
            _timeProvider = timeProvider;
        }

        public async Task<Result<AuthResponse>> HandleAsync(Request request, CancellationToken ct)
        {
            if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email, ct))
                return Result.Failure<AuthResponse>(Error.Conflict("User already exists"));

            var user = User.Create(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                _timeProvider.GetUtcNow());

            _dbContext.Users.Add(user);

            var refreshToken = RefreshToken.Create(user.Id, _timeProvider.GetUtcNow());
            _dbContext.RefreshTokens.Add(refreshToken);

            await _dbContext.SaveChangesAsync(ct);

            string accessToken = _jwtService.GenerateAccessToken(user);
            string refreshTokenValue = refreshToken.Token;

            return new AuthResponse(user.Id, accessToken, refreshTokenValue);
        }
    }
}
```

### Query Feature Example

```csharp
// See docs/features/profile/get-profile.md for the actual response shape.
// GetProfile uses a unified section model with sectionType discriminator
// and SectionOrder for ordering, not per-type navigation properties.
// The example below is a simplified illustration.

// Actual response shape (from feature docs):
// {
//   "id": "guid",
//   "headline": "...",
//   "summary": "...",
//   "sections": [
//     { "id": "guid", "sectionType": "Experience", "order": 1, "data": { ... } },
//     { "id": "guid", "sectionType": "Project", "order": 2, "data": { ... } },
//     ...
//   ]
// }
```

---

## Shared Kernel

### CQRS Interfaces

```csharp
// TailorCV.Shared/CQRS/ICommandHandler.cs
namespace TailorCV.Shared.CQRS;

public interface ICommandHandler<in TCommand, TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct);
}
```

```csharp
// TailorCV.Shared/CQRS/IQueryHandler.cs
namespace TailorCV.Shared.CQRS;

public interface IQueryHandler<in TQuery, TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken ct);
}
```

### Result Type

```csharp
// TailorCV.Shared/Results/Result.cs
namespace TailorCV.Shared.Results;

public class Result
{
    public bool IsSuccess { get; }
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
}

public class Result<T> : Result
{
    public T Value { get; }

    private Result(T value) : base(true, Error.None) => Value = value;
    private Result(Error error) : base(false, error) => Value = default;

    public static Result<T> Success(T value) => new(value);
    public new static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
```

```csharp
// TailorCV.Shared/Results/Error.cs
namespace TailorCV.Shared.Results;

public enum ErrorType
{
    None,
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict
}

public record Error(string Code, string Message, ErrorType Type)
{
    public static Error None => new(string.Empty, string.Empty, ErrorType.None);
    public static Error Validation(string message) => new("VALIDATION", message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);
}

// ErrorType maps to HTTP status codes via ToHttpStatusCode():
// None → 200, Validation → 400, Unauthorized → 401, Forbidden → 403, NotFound → 404, Conflict → 409
```

### Standard Pagination

All paginated endpoints use the same request/response shape.

**Request query parameters (always required):**

| Parameter | Rules |
|-----------|-------|
| page | Required, positive integer (min 1) |
| pageSize | Required, 1-50 |

**Response:**

```json
{
  "items": [ ... ],
  "pagingInfo": {
    "hasNext": true,
    "hasPrevious": false,
    "page": 1,
    "pageSize": 10,
    "total": 25
  }
}
```

```csharp
// TailorCV.Shared/Pagination/OffsetPagedList.cs
namespace TailorCV.Shared.Pagination;

public record PagingInfo(
    bool HasNext,
    bool HasPrevious,
    int Page,
    int PageSize,
    int Total);

public class OffsetPagedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public PagingInfo PagingInfo { get; }

    public OffsetPagedList(IReadOnlyList<T> items, int page, int pageSize, int total)
    {
        Items = items;
        PagingInfo = new PagingInfo(
            HasNext: page * pageSize < total,
            HasPrevious: page > 1,
            Page: page,
            PageSize: pageSize,
            Total: total);
    }
}
```

**Used by:** ListJobs, ListHistory, BrowseTemplates, and all future paginated endpoints.
### Scrutor Decorators

```csharp
// TailorCV.Shared/CQRS/ValidationDecorator.cs
namespace TailorCV.Shared.CQRS;

public class CommandValidationDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _inner;
    private readonly IEnumerable<IValidator<TCommand>> _validators;

    public CommandValidationDecorator(
        ICommandHandler<TCommand, TResult> inner,
        IEnumerable<IValidator<TCommand>> validators)
    {
        _inner = inner;
        _validators = validators;
    }

    public async Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct)
    {
        var failures = _validators
            .Select(v => v.Validate(command))
            .Where(r => !r.IsValid)
            .ToList();

        if (failures.Any())
        {
            var errors = string.Join("; ", failures.SelectMany(f => f.Errors.Select(e => e.ErrorMessage)));
            return Result.Failure<TResult>(Error.Validation(errors));
        }

        return await _inner.HandleAsync(command, ct);
    }
}

// Separate QueryValidationDecorator<TQuery, TResult> for query handlers (same pattern)
```

```csharp
// TailorCV.Shared/CQRS/LoggingDecorator.cs
namespace TailorCV.Shared.CQRS;

public class CommandLoggingDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _inner;
    private readonly ILogger<CommandLoggingDecorator<TCommand, TResult>> _logger;

    public CommandLoggingDecorator(
        ICommandHandler<TCommand, TResult> inner,
        ILogger<CommandLoggingDecorator<TCommand, TResult>> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Handling {CommandType}", typeof(TCommand).Name);
        try
        {
            var result = await _inner.HandleAsync(command, ct);
            _logger.LogInformation("Handled {CommandType}: {Status}", typeof(TCommand).Name,
                result.IsSuccess ? "Success" : "Failure");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {CommandType}", typeof(TCommand).Name);
            throw;
        }
    }
}

// Separate QueryLoggingDecorator<TQuery, TResult> for query handlers (same pattern)
```

### Registration Pattern (per module)

```csharp
// TailorCV.Identity/ModuleExtensions.cs
namespace TailorCV.Identity;

public static class ModuleExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity"))
            .UseSnakeCaseNamingConvention());

        services.Scan(scan => scan
            .FromAssemblyOf<IdentityDbContext>()
            .AddClasses(classes => classes.AssignableToAny(
                typeof(ICommandHandler<,>),
                typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Scan(scan => scan
            .FromAssemblyOf<IdentityDbContext>()
            .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        TryDecorate(services, typeof(ICommandHandler<,>), typeof(CommandValidationDecorator<,>));
        TryDecorate(services, typeof(ICommandHandler<,>), typeof(CommandLoggingDecorator<,>));
        TryDecorate(services, typeof(IQueryHandler<,>), typeof(QueryValidationDecorator<,>));
        TryDecorate(services, typeof(IQueryHandler<,>), typeof(QueryLoggingDecorator<,>));

        services.Configure<JwtSettings>(config.GetSection("JwtSettings"));
        services.AddSingleton<IJwtService, JwtService>();

        return services;
    }

    private static void TryDecorate(IServiceCollection services, Type serviceType, Type decoratorType)
    {
        bool hasRegistration = services.Any(s => s.ServiceType.IsGenericType
            && s.ServiceType.GetGenericTypeDefinition() == serviceType);
        if (hasRegistration)
        {
            services.Decorate(serviceType, decoratorType);
        }
    }

    public static async Task MigrateIdentityModuleAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return;
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        Features.Register.MapEndpoint(app);
        Features.Login.MapEndpoint(app);
        Features.RefreshToken.MapEndpoint(app);
        Features.Logout.MapEndpoint(app);
        return app;
    }
}
```

---

## Module Details

### Identity Module

**Schema:** `identity`  
**Responsibilities:** User registration, login, JWT management, refresh tokens

| Feature | Type | Endpoint | Description |
|---------|------|----------|-------------|
| Register | Command | `POST /api/auth/register` | Create account, return JWT |
| Login | Command | `POST /api/auth/login` | Authenticate, return JWT |
| RefreshToken | Command | `POST /api/auth/refresh` | Rotate refresh token |
| Logout | Command | `POST /api/auth/logout` | Client-side token discard |
| UpdateUserName | Command | `PUT /api/auth/user/name` | Update first/last name (requires auth) |

**Domain Entities:**
- `User` — Id, Email, PasswordHash, FirstName, LastName, Role, CreatedAt
- `RefreshToken` — Id, UserId, Token, ExpiresAt, CreatedAt

**Infrastructure:**
- `IdentityDbContext` (schema: `identity`)
- `JwtService` — token generation/validation
- PasswordHasher — PBKDF2 (Rfc2898DeriveBytes.Pbkdf2, HMAC-SHA256, 100k iterations, 128-bit salt, 256-bit hash)

**gRPC Service:** None (Identity publishes events via Wolverine instead)

**Published Events:**
- `UserRegistered(userId, firstName, lastName)` — published on registration
- `UserNameUpdated(userId, firstName, lastName)` — published on name update

### Profile Module

**Schema:** `profile`  
**Responsibilities:** User profile CRUD, sections management, resume import, sharing

| Feature | Type | Endpoint | Description |
|---------|------|----------|-------------|
| CreateProfile | Command | `POST /api/profiles` | Create user profile |
| UpdateProfile | Command | `PUT /api/profiles/me` | Update profile fields |
| GetProfile | Query | `GET /api/profiles/me` | Get user's full profile |
| UpdateSections | Command | `PUT /api/profiles/me/sections` | Bulk upsert all sections (add, update, remove, reorder) |
| ImportResumeGetUploadUrl | Command | `POST /api/profiles/me/import/upload-url` | Get RustFS presigned upload URL |
| ImportResumeParse | Command | `POST /api/profiles/me/import/parse` | Trigger AI resume parsing (Wolverine) |
| ImportResumeParseStatus | Query | `GET /api/profiles/me/import/parse/{parseId}/status` | Poll parsing job status |
| ImportResumeConfirm | Command | `POST /api/profiles/me/import/confirm` | Confirm parsed data import |
| ExportProfile | Command | `POST /api/profiles/me/export` | Export profile as JSON |
| GetCompleteness | Query | `GET /api/profiles/me/completeness` | Get profile completeness score |
| ShareProfile | Command | `POST /api/profiles/me/share` | Generate share link |
| GetSharedProfile | Query | `GET /api/profiles/shared/{token}` | View shared profile |

**Domain Entities:**
- `Profile` — Id, UserId, Headline, Summary, Phone, Location, Website, LinkedinUrl, GithubUrl, ShareId, IsShared
- `Experience` — Id, ProfileId, Company, Role, StartDate, EndDate, Description, IsCurrent, Order
- `Project` — Id, ProfileId, Name, Description, TechStack (JSONB), Role, Url, StartDate, EndDate, Order
- `Skill` — Id, ProfileId, Category, Items (JSONB), Order
- `Education` — Id, ProfileId, Institution, Degree, Field, StartDate, EndDate, Gpa, Order
- `Certification` — Id, ProfileId, Name, Issuer, Date, ExpiryDate, Url, Order
- `Language` — Id, ProfileId, LanguageName, Proficiency, Order

**Infrastructure:**
- `ProfileDbContext` (schema: `profile`)
- `ResumeParserService` — OpenAI API integration for resume parsing

**Event Handlers (subscribes to):**
- `UserRegistered` → creates ProfileUser record
- `UserNameUpdated` → updates ProfileUser record

**Local User Data:**
- `ProfileUser` — UserId, FirstName, LastName (populated via events, no gRPC needed for shared profile)

**gRPC Service:** `profile.proto`
- `GetProfileById(GetProfileByIdRequest) → GetProfileByIdResponse`

### JobDescriptions Module

**Schema:** `jobdescriptions`  
**Responsibilities:** Job description parsing, URL scraping, JD storage

**Architecture:** API module + separate Worker process (Wolverine host)

API publishes commands to `job-description.commands` queue → Worker consumes → OpenAI/Playwright → publishes events to `job-description.events` queue → API handlers update DB.

| Feature | Type | Endpoint | Description |
|---------|------|----------|-------------|
| ParseJobDescription | Command | `POST /api/jobs/parse` | Publish `ParseJobText` command → Worker handles OpenAI parsing |
| ScrapeJobDescription | Command | `POST /api/jobs/scrape` | Publish `ScrapeJobUrl` command → Worker handles Playwright + OpenAI |
| GetParseStatus | Query | `GET /api/jobs/parse/{parseId}/status` | Poll parse job status |
| SaveJobDescription | Command | `POST /api/jobs` | Save parsed JD to DB (synchronous, no messaging) |
| ListJobs | Query | `GET /api/jobs` | Paginated JD list |
| GetJob | Query | `GET /api/jobs/{id}` | Get full JD |

**Domain Entities:**
- `JobDescription` — Id, UserId, Title, Company, Location, RequiredSkills (JSONB), Responsibilities (JSONB), Qualifications (JSONB), SeniorityLevel, SourceUrl, Label, RawText, CreatedAt, UpdatedAt
- `ParseJob` — Id, UserId, Type (ManualText/UrlScrape), RawText, Status (Queued/Processing/Done/Failed), ParsedData (JSONB), Error, SourceUrl, CreatedAt, CompletedAt

**Infrastructure (API):**
- `JobDescriptionsDbContext` (schema: `jobdescriptions`)
- Wolverine handlers: `JobParsingCompletedHandler`, `JobParsingFailedHandler` — update ParseJob status from worker events

**Infrastructure (Worker):**
- `PlaywrightScrapingService` — headless Chromium, stealth, per-domain rate limiting
- `OpenAiJobParserService` — ChatClient with JSON schema response format
- `DomainRateLimiter` — TokenBucket per domain

**gRPC Service:** `jobdescriptions.proto`
- `GetJobById(JobIdRequest) → JobDescriptionResponse`

### Templates Module

**Schema:** `templates`  
**Responsibilities:** Template CRUD, preview, seeding

| Feature | Type | Endpoint | Description |
|---------|------|----------|-------------|
| BrowseTemplates | Query | `GET /api/templates` | List active templates |
| GetTemplate | Query | `GET /api/templates/{id}` | Get template details |
| PreviewTemplate | Query | `GET /api/templates/{id}/preview` | Render with sample data |
| CreateTemplate | Command | `POST /api/admin/templates` | Admin: add template |
| UpdateTemplate | Command | `PUT /api/admin/templates/{id}` | Admin: edit template |
| DisableTemplate | Command | `DELETE /api/admin/templates/{id}` | Admin: disable template |

**Domain Entities:**
- `Template` — Id, Name, Description, HtmlContent, CssContent, ThumbnailUrl, Category, Style, IsActive, CreatedAt, UpdatedAt

**Infrastructure:**
- `TemplatesDbContext` (schema: `templates`)
- `TemplateSeeder` — initial seed data on startup

**gRPC Service:** `templates.proto`
- `GetTemplateById(TemplateIdRequest) → TemplateResponse`

### CVGenerator Module

**Schema:** `cvgenerator`  
**Responsibilities:** CV generation, cover letters, matching, PDF export

| Feature | Type | Endpoint | Description |
|---------|------|----------|-------------|
| GenerateCV | Command | `POST /api/cv/generate` | AI-tailored CV (async — poll status) |
| GetGenerationStatus | Query | `GET /api/cv/generate/{generationId}/status` | Poll CV generation status |
| GetGeneratedCV | Query | `GET /api/cv/{id}` | Get full CV details |
| RegenerateCV | Command | `POST /api/cv/{id}/regenerate` | New CV with different template/prompt |
| UpdateCVContent | Command | `PUT /api/cv/{id}/content` | Edit AI-generated content |
| GenerateCoverLetter | Command | `POST /api/cv/{id}/cover-letter` | AI cover letter (async — poll status) |
| ExportPdf | Command | `POST /api/cv/{id}/export/pdf` | Trigger PDF export (async — poll status) |
| ExportPdfStatus | Query | `GET /api/cv/{id}/export/status` | Poll PDF export status |
| DownloadPdf | Query | `GET /api/cv/{id}/export/pdf` | Download generated PDF |
| ListHistory | Query | `GET /api/cv` | Paginated CV history |

**Domain Entities:**
- `GeneratedCV` — Id, UserId, ProfileSnapshot (JSONB), JobSnapshot (JSONB), TemplateId, Content (JSONB), MatchScore, CoverLetter, CreatedAt
- `CVContent` — Summary, Sections (ordered), highlighted skills, tailored descriptions
- `MatchScore` — Percentage, MatchingSkills, MissingSkills

**Infrastructure:**
- `CVGeneratorDbContext` (schema: `cvgenerator`)
- `CVTailoringService` — OpenAI API for CV tailoring
- `CoverLetterService` — OpenAI API for cover letter generation
- `PuppeteerPdfService` — HTML → PDF conversion

**Wolverine Published Events:**
- `CVTailoringCompleted` / `CVTailoringFailed` — after CV tailoring
- `CoverLetterCompleted` / `CoverLetterFailed` — after cover letter generation
- `CvPdfExportCompleted` / `CvPdfExportFailed` — after PDF export

**gRPC Service:** None (CVGenerator fetches data from other modules via gRPC, does not expose its own gRPC endpoint)

---

## Technology Stack

| Category | Technology | Purpose |
|----------|-----------|---------|
| **Runtime** | .NET 10 | Target framework |
| **HTTP** | Minimal APIs | Endpoint definitions |
| **CQRS** | Custom `ICommandHandler` / `IQueryHandler` | Command/query dispatch |
| **Decoration** | Scrutor | Cross-cutting handler decorators |
| **Persistence** | EF Core + Npgsql | PostgreSQL access |
| **Database** | PostgreSQL | Primary data store |
| **Schema isolation** | EF Core schema-per-module | Separate schemas per module |
| **Messaging** | Wolverine + RabbitMQ | Async events, sagas, outbox |
| **Sagas** | Wolverine sagas | Long-running process orchestration |
| **Outbox** | Wolverine built-in | Guaranteed message delivery |
| **Sync inter-module** | gRPC (Grpc.Net) | Real-time cross-module calls |
| **Validation** | FluentValidation | Input validation via decorator |
| **Error handling** | `Result<T>` pattern | Explicit error returns |
| **Resilience** | Wolverine built-in retry | Retry for external API calls |
| **Caching** | Redis (StackExchange.Redis) | Response caching |
| **Logging** | Serilog | Structured logging via decorator |
| **Observability** | OpenTelemetry | Distributed tracing + metrics |
| **Health checks** | ASP.NET Core Health Checks | `/health` endpoint |
| **Background jobs** | Wolverine | Async message processing, sagas |
| **API versioning** | Asp.Versioning.Http | Versioned endpoints |
| **Rate limiting** | ASP.NET Core Rate Limiting | Endpoint protection |
| **Correlation IDs** | Custom middleware | Request tracing |
| **Specification pattern** | Custom | Complex query building |
| **Time** | TimeProvider | Testable time abstraction |
| **AI** | OpenAI API | Resume parsing, JD parsing, CV tailoring |
| **Scraping** | Playwright | Job page scraping |
| **PDF** | PuppeteerSharp | HTML → PDF export |
| **Object storage** | RustFS (S3-compatible, Docker) | File uploads (resumes, photos, thumbnails) |
| **S3 SDK** | AWSSDK.S3 | Presigned URLs, upload, download, delete |
| **File processing** | PdfPig + DocumentFormat.OpenXml | PDF/DOCX text extraction |
| **Frontend** | Next.js (App Router, TypeScript) | Web UI |

---

## Cross-cutting Concerns

### Global Middleware (in TailorCV.Api)

```
Request → CorrelationId → ExceptionHandling → Auth → RateLimiting → Endpoint
```

1. **CorrelationIdMiddleware** — assigns or forwards `X-Correlation-Id` header
2. **ExceptionHandlingMiddleware** — catches unhandled exceptions → ProblemDetails response
3. **Authentication/Authorization** — JWT validation middleware
4. **RateLimitingMiddleware** — rate limit auth endpoints and scraping

### Handler Decorators (via Scrutor)

Applied to all `ICommandHandler` and `IQueryHandler` registrations:

1. **ValidationDecorator** — runs FluentValidation validators before handler
2. **LoggingDecorator** — logs handler entry/exit/failure with Serilog

### OpenTelemetry

Traces propagate across:
- HTTP requests (incoming)
- gRPC calls (inter-module)
- Wolverine messages (RabbitMQ)
- Database queries (EF Core instrumentation)
- Redis calls
- External HTTP calls (OpenAI, scraping)

Export to: OTel Collector → Grafana LGTM stack (Prometheus for metrics, Loki for logs via Serilog.Sinks.OpenTelemetry, Tempo for traces, Grafana for visualization)

### Health Checks

```
GET /health → { status, checks: [ postgres, rabbitmq, redis, opentelemetry ] }
```

### Central Package & Build Management

#### Directory.Build.props

Centralizes common build properties across all projects. Placed at repo root.

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AnalysisLevel>latest</AnalysisLevel>
    <AnalysisMode>All</AnalysisMode>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <CodeAnalysisTreatWarningsAsErrors>true</CodeAnalysisTreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
  <ItemGroup>
    <GlobalAnalyzerConfigFiles Include="$(MSBuildThisFileDirectory)../.editorconfig" />
  </ItemGroup>
</Project>
```

All child projects inherit these automatically — no need to repeat `TargetFramework`, `Nullable`, etc. in every `.csproj`.

#### Directory.Packages.props

Central Package Management (CPM). All NuGet package versions defined in one place.

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- API Versioning -->
    <PackageVersion Include="Asp.Versioning.Http" Version="8.1.1" />

    <!-- Health Checks -->
    <PackageVersion Include="AspNetCore.HealthChecks.NpgSql" Version="9.0.0" />
    <PackageVersion Include="AspNetCore.HealthChecks.Rabbitmq" Version="9.0.0" />
    <PackageVersion Include="AspNetCore.HealthChecks.Redis" Version="9.0.0" />

    <!-- S3 / Object Storage -->
    <PackageVersion Include="AWSSDK.S3" Version="4.0.21.2" />

    <!-- File Processing -->
    <PackageVersion Include="DocumentFormat.OpenXml" Version="3.5.1" />

    <!-- EF Core & PostgreSQL -->
    <PackageVersion Include="EFCore.NamingConventions" Version="10.0.1" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.6" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.6">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageVersion>
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.1" />

    <!-- Validation -->
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="12.1.1" />

    <!-- gRPC -->
    <PackageVersion Include="Grpc.AspNetCore" Version="2.76.0" />
    <PackageVersion Include="Grpc.Net.Client" Version="2.76.0" />

    <!-- Background Jobs: Handled by Wolverine built-in (no external package needed) -->

    <!-- Authentication -->
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.6" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.6" />

    <!-- Caching: StackExchange.Redis is used directly; Microsoft.Extensions.Caching.StackExchangeRedis was removed -->

    <!-- Resilience: Wolverine handles retries; Microsoft.Extensions.Http.Polly was removed -->

    <!-- Scraping -->
    <PackageVersion Include="Microsoft.Playwright" Version="1.59.0" />

    <!-- AI -->
    <PackageVersion Include="OpenAI" Version="2.10.0" />

    <!-- Observability -->
    <PackageVersion Include="OpenTelemetry" Version="1.15.2" />
    <PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.15.2" />
    <PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.15.2" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.15.1" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.15.0-beta.1" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.GrpcNetClient" Version="1.15.0-beta.1" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.Http" Version="1.15.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.StackExchangeRedis" Version="1.15.0-beta.1" />

    <!-- PDF -->
    <PackageVersion Include="PdfPig" Version="0.1.14" />
    <PackageVersion Include="PuppeteerSharp" Version="24.40.0" />

    <!-- Decoration / DI -->
    <PackageVersion Include="Scrutor" Version="7.0.0" />

    <!-- OpenAPI -->
    <PackageVersion Include="Scalar.AspNetCore" Version="2.14.0" />

    <!-- Logging -->
    <PackageVersion Include="Serilog" Version="4.3.1" />
    <PackageVersion Include="Serilog.AspNetCore" Version="10.0.0" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="6.1.1" />
    <PackageVersion Include="Serilog.Sinks.OpenTelemetry" Version="4.2.0" />

    <!-- Caching (Redis) -->
    <PackageVersion Include="StackExchange.Redis" Version="2.12.14" />

    <!-- Authentication (JWT) -->
    <PackageVersion Include="System.IdentityModel.Tokens.Jwt" Version="8.17.0" />

    <!-- Static Analysis -->
    <PackageVersion Include="SonarAnalyzer.CSharp" Version="10.19.0.132793" />

    <!-- Wolverine & Messaging -->
    <PackageVersion Include="WolverineFx" Version="5.31.1" />
    <PackageVersion Include="WolverineFx.RabbitMQ" Version="5.31.1" />
  </ItemGroup>
</Project>
```

Individual `.csproj` files only reference packages by name (no version):

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="FluentValidation" />
    <PackageReference Include="Scrutor" />
  </ItemGroup>
</Project>
```

Benefits:
- Single source of truth for all package versions
- No version conflicts across projects
- Easy upgrades (change one file)
- Clear overview of all dependencies

---

## Database Design

### Schema Isolation

Each module has its own PostgreSQL schema. Tables are prefixed by module:

```
 PostgreSQL Database
 ├── identity schema
 │   ├── users
 │   └── refresh_tokens
 │
 ├── profile schema
 │   ├── profiles
 │   ├── users                                   # Event-driven copy of Identity user names
 │   ├── experiences
 │   ├── projects
 │   ├── skills
 │   ├── education
 │   ├── certifications
 │   ├── languages
 │   ├── custom_sections
 │   ├── section_orders
│   └── parse_jobs
  │
  ├── jobdescriptions schema
  │   ├── job_descriptions
  │   └── parse_jobs                                    # Async parsing status tracking
  │
 ├── templates schema
 │   └── templates
 │
 ├── cvgenerator schema
 │   ├── generated_cvs
 │   └── cover_letters
 │
 └── wolverine schema
     ├── inbox
     └── outbox
```

### Migration Strategy

Each module owns its own migrations:

```
dotnet ef migrations add InitialCreate \
  --project src/Modules/Identity/TailorCV.Identity \
  -- --schema identity
```

### Entity Configuration Example

```csharp
// TailorCV.Identity/Infrastructure/Configurations/UserConfiguration.cs
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "identity");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Role).IsRequired();
    }
}
```

---

## gRPC Contracts

### profile.proto

```protobuf
syntax = "proto3";

package profile;

service ProfileService {
  rpc GetProfileById (ProfileIdRequest) returns (ProfileResponse);
}

message UserIdRequest {
  string user_id = 1;
}

message ProfileIdRequest {
  string profile_id = 1;
}

message ProfileResponse {
  string profile_id = 1;
  string user_id = 2;
  string headline = 3;
  string summary = 4;
  repeated ExperienceResponse experiences = 5;
  repeated ProjectResponse projects = 6;
  repeated SkillResponse skills = 7;
  repeated EducationResponse education = 8;
  repeated CertificationResponse certifications = 9;
  repeated LanguageResponse languages = 10;
}

message ExperienceResponse {
  string company = 1;
  string role = 2;
  string start_date = 3;
  string end_date = 4;
  string description = 5;
  bool is_current = 6;
}

message ProjectResponse {
  string name = 1;
  string description = 2;
  repeated string tech_stack = 3;
  string url = 4;
}

message SkillResponse {
  string category = 1;
  repeated string items = 2;
}

message EducationResponse {
  string institution = 1;
  string degree = 2;
  string field = 3;
  string start_date = 4;
  string end_date = 5;
}

message CertificationResponse {
  string name = 1;
  string issuer = 2;
  string date = 3;
}

message LanguageResponse {
  string language = 1;
  string proficiency = 2;
}
```

### jobdescriptions.proto

```protobuf
syntax = "proto3";

package jobdescriptions;

service JobDescriptionsService {
  rpc GetJobById (JobIdRequest) returns (JobDescriptionResponse);
}

message JobIdRequest {
  string job_id = 1;
}

message JobDescriptionResponse {
  string job_id = 1;
  string title = 2;
  string company = 3;
  string location = 4;
  repeated string required_skills = 5;
  repeated string responsibilities = 6;
  repeated string qualifications = 7;
  string seniority_level = 8;
  string raw_text = 9;
}
```

### templates.proto

```protobuf
syntax = "proto3";

package templates;

service TemplatesService {
  rpc GetTemplateById (TemplateIdRequest) returns (TemplateResponse);
}

message TemplateIdRequest {
  string template_id = 1;
}

message TemplateResponse {
  string template_id = 1;
  string name = 2;
  string html_content = 3;
  string css_content = 4;
  string category = 5;
  string style = 6;
}
```

---

## Event Flow

### Integration Events (via Wolverine + RabbitMQ)

```
┌──────────────────────┐   UserRegistered           ┌──────────────────┐
│  Identity            │ ────────────────────────────→ │  Profile         │
│  publishes from:     │   UserNameUpdated             │   listens via:   │
│  .Identity.Contracts │                              │  ProfileWolverineExtension │
└──────────────────────┘                              └──────────────────┘

┌──────────────────────┐   JobParsingCompleted       ┌──────────────────┐
│  JobDescriptions     │ ────────────────────────────→ │  CVGenerator     │
│  publishes from:     │                              │   listens via:   │
│  .JobDescriptions.  │   JobParsingFailed           │  CVGenerator     │
│  Contracts           │ ────────────────────────────→ │  WolverineExt    │
└──────────────────────┘                              └──────────────────┘

┌──────────────────────┐   CVTailoringCompleted      ┌──────────────────┐
│  CVGenerator         │ ────────────────────────────→ │  (future:         │
│  (no consumer yet)   │   CoverLetterCompleted       │   Dashboard,      │
│  .CVGenerator.       │   CvPdfExportCompleted       │   notifications)  │
│  Contracts           │                              │                  │
└──────────────────────┘                              └──────────────────┘
```

### Event Definitions (in per-module Contracts projects)

Each module that publishes events defines them in its own `.Contracts` project. Consumers reference the publisher's Contracts project — never the full module.

```
TailorCV.Identity.Contracts/Events/UserRegistered.cs
TailorCV.Identity.Contracts/Events/UserNameUpdated.cs
TailorCV.Profile.Contracts/Events/ProfileUpdated.cs
TailorCV.Profile.Contracts/Events/ResumeParsingCompleted.cs
TailorCV.Profile.Contracts/Events/ResumeParsingFailed.cs
TailorCV.JobDescriptions.Contracts/Events/JobParsingCompleted.cs
TailorCV.JobDescriptions.Contracts/Events/JobParsingFailed.cs
TailorCV.CVGenerator.Contracts/Events/CVTailoringCompleted.cs
TailorCV.CVGenerator.Contracts/Events/CVTailoringFailed.cs
TailorCV.CVGenerator.Contracts/Events/CoverLetterCompleted.cs
TailorCV.CVGenerator.Contracts/Events/CoverLetterFailed.cs
TailorCV.CVGenerator.Contracts/Events/CvPdfExportCompleted.cs
TailorCV.CVGenerator.Contracts/Events/CvPdfExportFailed.cs
```

```csharp
// TailorCV.Identity.Contracts — published when user registers or updates name
public record UserRegistered(Guid UserId, string FirstName, string LastName);
public record UserNameUpdated(Guid UserId, string FirstName, string LastName);

// TailorCV.Profile.Contracts — published when profile changes or resume parsed
public record ProfileUpdated(Guid UserId, Guid ProfileId, DateTimeOffset UpdatedAt);
public record ResumeParsingCompleted(Guid ParseJobId, ParsedResumeData Data);
public record ResumeParsingFailed(Guid ParseJobId, string Error);

// TailorCV.JobDescriptions.Contracts — published when a JD parse completes (success or failure)
public record JobParsingCompleted(Guid ParseJobId, ParsedJobDataDto Data, string? RawText = null, Uri? SourceUrl = null);
public record JobParsingFailed(Guid ParseJobId, string Error);

// TailorCV.CVGenerator.Contracts — published when CV/cover-letter/PDF operations complete
public record CVTailoringCompleted(Guid CVId, ...);
public record CVTailoringFailed(Guid CVId, string Error);
public record CoverLetterCompleted(Guid CVId, ...);
public record CoverLetterFailed(Guid CVId, string Error);
public record CvPdfExportCompleted(Guid CVId, string PdfUrl);
public record CvPdfExportFailed(Guid CVId, string Error);
```

**Contracts project references (who references whom):**

| Module | References |
|--------|-----------|
| CVGenerator | `TailorCV.Profile.Contracts`, `TailorCV.JobDescriptions.Contracts`, `TailorCV.Templates.Contracts` |
| Profile | `TailorCV.Identity.Contracts` (for user name via events) |
| JobDescriptions | _(none — no incoming events)_ |
| Templates | _(none — no incoming events)_ |
| Identity | _(does not consume events; publishes via Wolverine)_ |

> **Rule:** A module's Contracts project contains only `record` types (events + DTOs). No logic, no dependencies on other TailorCV projects. This keeps them safe to share across service boundaries when splitting into microservices.

### CV Generation Flow (Wolverine)

```
GenerateCV command received
  │
  ├── gRPC: Fetch Profile from Profile module
  ├── gRPC: Fetch JobDescription from JobDescriptions module
  ├── gRPC: Fetch Template from Templates module
  ├── AI: Tailor CV content (OpenAI)
  ├── AI: Calculate match score (OpenAI)
  ├── Store: Save GeneratedCV to database
  ├── Publish: CVTailoringCompleted via Wolverine
  └── Return: Generated CV result (poll status via GetGenerationStatus)
```

**Cover letter flow:** Similar pattern → `CoverLetterCompleted`  
**PDF export flow:** Similar pattern → `CvPdfExportCompleted`

---

## Microservice Readiness

When the time comes to split modules into separate services:

| What Changes | Effort |
|-------------|--------|
| Each module becomes its own ASP.NET Core host (Program.cs) | Medium |
| gRPC stays the same (just point to new addresses) | Low |
| Wolverine + RabbitMQ stays the same (same broker) | None |
| Each module's DB schema → separate database | Medium (migration) |
| Shared kernel → shared NuGet package or git submodule | Low |
| Contracts projects → shared NuGet packages (already isolated) | Low |
| Frontend → point API calls to new service URLs or add API gateway | Medium |

**What does NOT change:**
- Feature files (handlers, endpoints, validators) — zero changes
- gRPC contracts (proto files) — zero changes
- Event contracts — zero changes
- Wolverine messaging — zero changes
- Domain logic — zero changes

This is the key benefit of the modular monolith approach — the split is mostly infrastructure, not code.
