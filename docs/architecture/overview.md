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
│ Identity │ Profile  │ JobScraper │ Templates  │ CVGenerator  │
├──────────┴──────────┴────────────┴────────────┴──────────────┤
│                                                               │
│   Async: Wolverine + RabbitMQ (events, sagas, outbox)        │
│   Sync:  gRPC (real-time data fetch between modules)          │
│                                                               │
├──────────────────────────────────────────────────────────────┤
│     PostgreSQL (EF Core, separate schemas per module)         │
├──────────────────────────────────────────────────────────────┤
│     Redis │ Serilog │ OpenTelemetry │ Polly │ Hangfire        │
└──────────────────────────────────────────────────────────────┘
```

### Communication Rules

| Scenario | Pattern | Example |
|----------|---------|---------|
| Module publishes event | Wolverine + RabbitMQ | CVGenerator publishes `CVGeneratedEvent` |
| Module reacts to event | Wolverine handler | Dashboard listens to `CVGeneratedEvent` |
| Module needs data in real-time | gRPC | CVGenerator fetches Profile data during generation |
| Module needs data async | Event-driven | Profile publishes `ProfileUpdatedEvent` → CVGenerator listens |
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
│   │   ├── Events/
│   │   │   └── IntegrationEvent.cs              # IIntegrationEvent marker interface
│   │   ├── Interfaces/
│   │   │   ├── ICurrentUserService.cs           # UserId, Email, Role, IsAuthenticated
│   │   │   └── IDateTimeProvider.cs             # DateTimeOffset UtcNow
│   │   └── Primitives/
│   │       └── Entity.cs                        # Base entity (Guid.CreateVersion7())
│   │
│   ├── TailorCV.Infrastructure/                 # Shared infrastructure (all stubs)
│   │   ├── Persistence/MigrationExtensions.cs
│   │   ├── Storage/S3Service.cs + S3Configuration.cs
│   │   ├── OpenTelemetry/TracingConfiguration.cs
│   │   ├── Caching/RedisConfiguration.cs + CacheService.cs
│   │   ├── Logging/SerilogConfiguration.cs
│   │   └── Resilience/PollyConfiguration.cs
│   │
│   ├── protos/                                  # gRPC contracts (all empty placeholders)
│   │   ├── identity.proto
│   │   ├── profile.proto
│   │   ├── jobscraper.proto
│   │   ├── templates.proto
│   │   └── cvgenerator.proto
│   │
│   └── Modules/
│       ├── Identity/
│       │   ├── TailorCV.Identity/               # FULLY IMPLEMENTED
│       │   │   ├── Features/
│       │   │   │   ├── Register.cs
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
│       │   └── TailorCV.Identity.Contracts/
│       │       └── Events/.gitkeep              # (no events published)
│       │
│       ├── Profile/
│       │   ├── TailorCV.Profile/                # STUBS (domain scaffolded, features empty)
│       │   │   ├── Features/                    # 15 empty namespace stubs
│       │   │   ├── Domain/                      # Profile, Experience, Project, Skill, Education, etc.
│       │   │   ├── Infrastructure/
│       │   │   │   ├── ProfileDbContext.cs      # Empty stub
│       │   │   │   ├── AI/ResumeParserService.cs
│       │   │   │   └── Configurations/          # Empty directory
│       │   │   └── ModuleExtensions.cs          # Empty stub
│       │   └── TailorCV.Profile.Contracts/
│       │       └── Events/ProfileUpdatedEvent.cs  # Empty stub
│       │
│       ├── JobScraper/
│       │   ├── TailorCV.JobScraper/             # STUBS (domain scaffolded, features empty)
│       │   │   ├── Features/                    # 5 empty namespace stubs
│       │   │   ├── Domain/
│       │   │   ├── Infrastructure/
│       │   │   │   ├── JobScraperDbContext.cs   # Empty stub
│       │   │   │   ├── Scraping/PlaywrightScrapingService.cs
│       │   │   │   ├── AI/JobDescriptionParserService.cs
│       │   │   │   └── Configurations/          # Empty directory
│       │   │   └── ModuleExtensions.cs          # Empty stub
│       │   └── TailorCV.JobScraper.Contracts/
│       │       └── Events/JobDescriptionSavedEvent.cs  # Empty stub
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
│       └── jobscraper/ (5 feature docs)
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
└── Events/                           # (currently empty — Identity doesn't publish events)
    # Future: UserRegisteredEvent, UserDeletedEvent, etc.

TailorCV.Profile.Contracts/
└── Events/
    └── ProfileUpdatedEvent.cs        # Guid UserId, Guid ProfileId, DateTime UpdatedAt

TailorCV.JobScraper.Contracts/
└── Events/
    └── JobDescriptionSavedEvent.cs   # Guid JobId, Guid UserId, string Title, DateTime SavedAt

TailorCV.Templates.Contracts/
└── Events/                           # (currently empty — Templates don't publish events)

TailorCV.CVGenerator.Contracts/
└── Events/
    └── CVGeneratedEvent.cs           # Guid CVId, Guid UserId, string JobTitle, int MatchScore, DateTime GeneratedAt
```

### Contracts Project Dependencies (.csproj)

```xml
<!-- TailorCV.CVGenerator.csproj — heaviest consumer of contracts -->
<ItemGroup>
  <ProjectReference Include="..\Profile\TailorCV.Profile.Contracts\TailorCV.Profile.Contracts.csproj" />
  <ProjectReference Include="..\JobScraper\TailorCV.JobScraper.Contracts\TailorCV.JobScraper.Contracts.csproj" />
  <ProjectReference Include="..\Templates\TailorCV.Templates.Contracts\TailorCV.Templates.Contracts.csproj" />
</ItemGroup>

<!-- TailorCV.Profile.csproj — only needs Identity contracts for gRPC responses -->
<ItemGroup>
  <ProjectReference Include="..\Identity\TailorCV.Identity.Contracts\TailorCV.Identity.Contracts.csproj" />
</ItemGroup>

<!-- Contracts projects themselves — NO references to other TailorCV projects -->
<!-- TailorCV.Profile.Contracts.csproj -->
<ItemGroup>
  <PackageReference Include="Wolverine" />  <!-- only if event base types are needed -->
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
    │  Identity       │  │  Profile   │  │  JobScraper     │
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
    │                │  │    JobScraper.Contracts        │
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

**Domain Entities:**
- `User` — Id, Email, PasswordHash, FirstName, LastName, Role, CreatedAt
- `RefreshToken` — Id, UserId, Token, ExpiresAt, CreatedAt

**Infrastructure:**
- `IdentityDbContext` (schema: `identity`)
- `JwtService` — token generation/validation
- PasswordHasher — PBKDF2 (Rfc2898DeriveBytes.Pbkdf2, HMAC-SHA256, 100k iterations, 128-bit salt, 256-bit hash)

**gRPC Service:** `identity.proto`
- `GetUserById(UserIdRequest) → UserResponse`

### Profile Module

**Schema:** `profile`  
**Responsibilities:** User profile CRUD, sections management, resume import, sharing

| Feature | Type | Endpoint | Description |
|---------|------|----------|-------------|
| CreateProfile | Command | `POST /api/profiles` | Create user profile |
| UpdateProfile | Command | `PUT /api/profiles/me` | Update profile fields |
| GetProfile | Query | `GET /api/profiles/me` | Get user's full profile |
| AddSection | Command | `POST /api/profiles/me/sections` | Add a section (any type: experience, project, skill, etc.) |
| UpdateSection | Command | `PUT /api/profiles/me/sections/{id}` | Update a section by ID |
| RemoveSection | Command | `DELETE /api/profiles/me/sections/{id}` | Remove a section by ID |
| ReorderSections | Command | `PUT /api/profiles/me/sections/reorder` | Reorder sections via SectionOrder |
| ImportResumeGetUploadUrl | Command | `POST /api/profiles/me/import/upload-url` | Get RustFS presigned upload URL |
| ImportResumeParse | Command | `POST /api/profiles/me/import/parse` | Trigger AI resume parsing (Hangfire) |
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

**gRPC Service:** `profile.proto`
- `GetProfileByUserId(UserIdRequest) → ProfileResponse`
- `GetProfileById(ProfileIdRequest) → ProfileResponse`

### JobScraper Module

**Schema:** `jobscraper`  
**Responsibilities:** Job description parsing, URL scraping, JD storage

| Feature | Type | Endpoint | Description |
|---------|------|----------|-------------|
| ParseJobDescription | Command | `POST /api/jobs/parse` | Parse raw text via AI |
| ScrapeJobUrl | Command | `POST /api/jobs/scrape` | Playwright scrape + AI parse |
| SaveJobDescription | Command | `POST /api/jobs` | Save parsed JD |
| ListJobs | Query | `GET /api/jobs` | Paginated JD list |
| GetJob | Query | `GET /api/jobs/{id}` | Get full JD |

**Domain Entities:**
- `JobDescription` — Id, UserId, Title, Company, Location, RequiredSkills (JSONB), Responsibilities (JSONB), Qualifications (JSONB), SeniorityLevel, Label, RawText, SourceUrl, CreatedAt

**Infrastructure:**
- `JobScraperDbContext` (schema: `jobscraper`)
- `PlaywrightScrapingService` — headless browser scraping
- `JobDescriptionParserService` — OpenAI API for structured extraction

**gRPC Service:** `jobscraper.proto`
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
| GenerateCV | Command | `POST /api/cv/generate` | AI-tailored CV |
| GenerateCoverLetter | Command | `POST /api/cv/cover-letter` | AI cover letter |
| GetMatchScore | Query | `POST /api/cv/match-score` | Profile vs JD score |
| PreviewCV | Query | `GET /api/cv/{id}/preview` | Rendered HTML preview |
| ExportPdf | Query | `GET /api/cv/{id}/export/pdf` | Download PDF |
| RegenerateCV | Command | `POST /api/cv/{id}/regenerate` | New template, same data |
| ListHistory | Query | `GET /api/cv` | Paginated history |
| GetGeneratedCV | Query | `GET /api/cv/{id}` | Get CV details |

**Domain Entities:**
- `GeneratedCV` — Id, UserId, ProfileSnapshot (JSONB), JobSnapshot (JSONB), TemplateId, Content (JSONB), MatchScore, CoverLetter, CreatedAt
- `CVContent` — Summary, Sections (ordered), highlighted skills, tailored descriptions
- `MatchScore` — Percentage, MatchingSkills, MissingSkills

**Infrastructure:**
- `CVGeneratorDbContext` (schema: `cvgenerator`)
- `CVTailoringService` — OpenAI API for CV tailoring
- `CoverLetterService` — OpenAI API for cover letter generation
- `PuppeteerPdfService` — HTML → PDF conversion

**Wolverine Event Handlers:**
- Publishes `CVGeneratedEvent` after successful generation

**Wolverine Saga:**
- `CVGenerationSaga` — orchestrates: fetch profile (gRPC) → fetch JD (gRPC) → fetch template (gRPC) → AI tailor → store → publish event

**gRPC Service:** `cvgenerator.proto`
- `GetGeneratedCVById(CVIdRequest) → GeneratedCVResponse`

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
| **Resilience** | Polly | Retry, circuit breaker for HTTP calls |
| **Caching** | Redis (StackExchange.Redis) | Response caching |
| **Logging** | Serilog | Structured logging via decorator |
| **Observability** | OpenTelemetry | Distributed tracing + metrics |
| **Health checks** | ASP.NET Core Health Checks | `/health` endpoint |
| **Background jobs** | Hangfire | Scheduled + long-running tasks |
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
    <PackageVersion Include="AspNetCore.HealthChecks.UI.Client" Version="9.0.0" />
    <PackageVersion Include="AspNetCore.HealthChecks.Uris" Version="9.0.0" />

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

    <!-- Background Jobs -->
    <PackageVersion Include="Hangfire.AspNetCore" Version="1.8.23" />
    <PackageVersion Include="Hangfire.PostgreSql" Version="1.21.1" />

    <!-- Authentication -->
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.6" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.6" />

    <!-- Caching -->
    <PackageVersion Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.0.6" />
    <PackageVersion Include="Microsoft.Extensions.Diagnostics.HealthChecks" Version="10.0.6" />

    <!-- Resilience -->
    <PackageVersion Include="Microsoft.Extensions.Http.Polly" Version="10.0.6" />

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
  ├── jobscraper schema
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

### identity.proto

```protobuf
syntax = "proto3";

package identity;

service IdentityService {
  rpc GetUserById (UserIdRequest) returns (UserResponse);
}

message UserIdRequest {
  string user_id = 1;
}

message UserResponse {
  string user_id = 1;
  string email = 2;
  string first_name = 3;
  string last_name = 4;
  string role = 5;
}
```

### profile.proto

```protobuf
syntax = "proto3";

package profile;

service ProfileService {
  rpc GetProfileByUserId (UserIdRequest) returns (ProfileResponse);
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

### jobscraper.proto

```protobuf
syntax = "proto3";

package jobscraper;

service JobScraperService {
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

### cvgenerator.proto

```protobuf
syntax = "proto3";

package cvgenerator;

service CVGeneratorService {
  rpc GetGeneratedCVById (CVIdRequest) returns (GeneratedCVResponse);
}

message CVIdRequest {
  string cv_id = 1;
}

message GeneratedCVResponse {
  string cv_id = 1;
  string user_id = 2;
  string content_json = 3;
  string cover_letter = 4;
  int32 match_score = 5;
  string template_id = 6;
  string created_at = 7;
}
```

---

## Event Flow

### Integration Events (via Wolverine + RabbitMQ)

```
┌──────────────────────┐                              ┌──────────────────┐
│  CVGenerator         │   CVGeneratedEvent           │   Dashboard       │
│  publishes from:     │ ────────────────────────────→ │   listens via:   │
│  .CVGenerator.Contracts │                            │  ref: .CVGenerator.Contracts │
└──────────────────────┘                              └──────────────────┘

┌──────────────────────┐   ProfileUpdatedEvent        ┌──────────────────┐
│  Profile             │ ────────────────────────────→ │  CVGenerator     │
│  publishes from:     │                              │   listens via:   │
│  .Profile.Contracts  │                              │  ref: .Profile.Contracts │
└──────────────────────┘                              └──────────────────┘

┌──────────────────────┐  JobDescriptionSavedEvent    ┌──────────────────┐
│  JobScraper          │ ────────────────────────────→ │  CVGenerator     │
│  publishes from:     │                              │   listens via:   │
│  .JobScraper.Contracts │                            │  ref: .JobScraper.Contracts │
└──────────────────────┘                              └──────────────────┘
```

### Event Definitions (in per-module Contracts projects)

Each module that publishes events defines them in its own `.Contracts` project. Consumers reference the publisher's Contracts project — never the full module.

```
TailorCV.Profile.Contracts/Events/ProfileUpdatedEvent.cs
TailorCV.JobScraper.Contracts/Events/JobDescriptionSavedEvent.cs
TailorCV.CVGenerator.Contracts/Events/CVGeneratedEvent.cs
```

```csharp
// TailorCV.Profile.Contracts — published when profile is created/updated
public record ProfileUpdatedEvent(Guid UserId, Guid ProfileId, DateTime UpdatedAt);

// TailorCV.JobScraper.Contracts — published when a JD is saved/parsed
public record JobDescriptionSavedEvent(Guid JobId, Guid UserId, string Title, DateTime SavedAt);

// TailorCV.CVGenerator.Contracts — published when a CV is generated
public record CVGeneratedEvent(Guid CVId, Guid UserId, string JobTitle, int MatchScore, DateTime GeneratedAt);
```

**Contracts project references (who references whom):**

| Module | References |
|--------|-----------|
| CVGenerator | `TailorCV.Profile.Contracts`, `TailorCV.JobScraper.Contracts`, `TailorCV.Templates.Contracts` |
| Dashboard (if separate) | `TailorCV.CVGenerator.Contracts` |
| Profile | `TailorCV.Identity.Contracts` (for user name via gRPC — not events) |
| JobScraper | _(none — no incoming events)_ |
| Templates | _(none — no incoming events)_ |
| Identity | _(none — publishes no events)_ |

> **Rule:** A module's Contracts project contains only `record` types (events + DTOs). No logic, no dependencies on other TailorCV projects. This keeps them safe to share across service boundaries when splitting into microservices.

### CV Generation Saga (Wolverine)

```
GenerateCV command received
  │
  ├── gRPC: Fetch Profile from Profile module
  ├── gRPC: Fetch JobDescription from JobScraper module
  ├── gRPC: Fetch Template from Templates module
  ├── AI: Tailor CV content (OpenAI)
  ├── AI: Calculate match score (OpenAI)
  ├── Store: Save GeneratedCV to database
  ├── Publish: CVGeneratedEvent via Wolverine
  └── Return: Generated CV result
```

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
