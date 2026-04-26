# AGENTS.md — TailorCV

Compact reference for AI agents working in this repo. Read this before making changes.

## Build

```bash
dotnet build TailorCV.slnx
```

Must produce **0 errors, 0 warnings**. `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` + SonarAnalyzer are all active.

## Run

### Docker Compose (full stack)

```bash
# Start infra (postgres, rabbitmq, redis)
docker compose -f infra/docker-compose.yml up -d postgres rabbitmq redis

# Build and start API + Worker
docker compose -f infra/docker-compose.yml up -d --build
```

API: **http://localhost:8080** | Worker: runs as container | Dev port: **8080**

### Local Development (dotnet run)

```bash
# 1. Start infra via Docker
docker compose -f infra/docker-compose.yml up -d postgres rabbitmq redis

# 2. Set up secrets (once)
dotnet user-secrets init --project src/Modules/JobDescriptions/TailorCV.JobDescriptions.Worker
dotnet user-secrets set "OpenAI:ApiKey" "<your-openai-or-openrouter-key>" --project src/Modules/JobDescriptions/TailorCV.JobDescriptions.Worker

# 3. Run API locally
dotnet run --project src/TailorCV.Api
```

Auto-migrates on startup in Development. API: **http://localhost:8080**

## Local Development Config

### `appsettings.Development.json` (committed, no secrets)

Non-secret config lives in committed `appsettings.Development.json` files:
- `src/TailorCV.Api/appsettings.Development.json` — API connections, JWT, Redis, RustFS
- `src/Modules/JobDescriptions/TailorCV.JobDescriptions.Worker/appsettings.Development.json` — OpenAI endpoint, model, Playwright settings

### User Secrets (local only, never committed)

Only **OpenAI API key** is a secret. Set it once:

```bash
dotnet user-secrets init --project src/Modules/JobDescriptions/TailorCV.JobDescriptions.Worker
dotnet user-secrets set "OpenAI:ApiKey" "<your-openai-or-openrouter-key>" --project src/Modules/JobDescriptions/TailorCV.JobDescriptions.Worker
```

## Architecture Rules

- **Modular monolith** — vertical slice, one `.cs` file per feature (static class with nested `MapEndpoint`, `Request`, `Validator`, `Handler`, `Response`)
- **No repository pattern** — handlers use `DbContext` directly
- **No clean architecture layers** within a module
- **No in-process interface calls** between modules — async via Wolverine + RabbitMQ (API module) or gRPC (planned)
- **Separate PostgreSQL schema per module** (`identity`, `profile`, `jobdescription`, `templates`, `cvgenerator`)

## Critical Code Style Rules

These are enforced by `.editorconfig` + SonarAnalyzer at build time:

- **`var` is banned** — always use explicit types (`string`, `int`, `Result<T>`, etc.)
- **File-scoped namespaces** — `namespace Foo;` not `namespace Foo { }`
- **Braces required** on all `if` statements (even single-line)
- **No code comments** — do not add `//` comments unless explicitly asked
- **`using` directives outside namespace** — `csharp_using_directive_placement = outside_namespace`
- **Primary constructors** — allowed but not enforced (IDE0290 = none)

## Key Patterns

### Error handling — `Result<T>` + `Error`

```csharp
// Error has 3 fields: Code, Message, Type (ErrorType enum)
// ErrorType maps to HTTP status via ToHttpStatusCode()
Error.NotFound("USER_NOT_FOUND", "User not found")  // → 404
Error.Conflict("EMAIL_ALREADY_EXISTS", "...")        // → 409
Error.Unauthorized("INVALID_CREDENTIALS", "...")     // → 401
Error.Validation("Email is required")                // → 400 (single-param, code is always "VALIDATION")

// Handlers return Result<T>
return Result<T>.Failure(IdentityErrors.EmailAlreadyExists);
return Result<T>.Success(new Response(...));

// Endpoints convert to HTTP via ToProblemDetails()
result.ToProblemDetails();
```

### Business error codes — centralized per module

Each module has a `XxxErrors` static class in its `Domain/` folder:
```csharp
public static class IdentityErrors
{
    public static Error EmailAlreadyExists => Error.Conflict("EMAIL_ALREADY_EXISTS", "...");
}
```

### Entity pattern — rich domain, static factory

```csharp
public class User : Entity
{
    public string Email { get; private set; }
    // private setters, no public constructors

    public static Result<User> Create(string email, string passwordHash, ...) { ... }
}
```

Entities take **primitive values** (e.g., `DateTimeOffset now`), NOT `IDateTimeProvider`.

### CQRS handlers + decorators

- `ICommandHandler<TCommand, TResult>` and `IQueryHandler<TQuery, TResult>` in Shared
- **4 decorator classes** in 2 files: `CommandValidationDecorator`, `QueryValidationDecorator`, `CommandLoggingDecorator`, `QueryLoggingDecorator`
- Registered via Scrutor `FromAssemblyOf<SomeDbContext>()` (not `ModuleExtensions` — it's static)
- `TryDecorate()` helper guards against decorating when no registrations exist

### Module registration — `ModuleExtensions.cs`

Each module has `AddXxxModule(IServiceCollection, IConfiguration)` and `MapXxxEndpoints(IEndpointRouteBuilder)`. Wire both in `Program.cs`.

Auto-migration: `MigrateXxxModuleAsync(WebApplication)` — gated by `IsDevelopment()`.

### Password hashing

Uses **PBKDF2** (`Rfc2898DeriveBytes.Pbkdf2`, HMAC-SHA256, 100k iterations). NOT BCrypt. `PasswordHasher` is a static class in module Infrastructure.

### IDs

`Guid.CreateVersion7()` everywhere. The `Entity` base class uses it for `Id`. Never `Guid.NewGuid()`.

### Time

`IDateTimeProvider` → `DateTimeProvider` (wraps `TimeProvider.System`). Returns `DateTimeOffset`, never `DateTime`.

### Pagination

`OffsetPagedList<T>` with `{ items, pagingInfo: { hasNext, hasPrevious, page, pageSize, total } }`. `page` and `pageSize` always required.

## JobDescriptions Module Architecture

The JobDescriptions module is split into two projects:

- **`TailorCV.JobDescriptions`** — API module (HTTP endpoints, domain, DB)
- **`TailorCV.JobDescriptions.Worker`** — Background worker (Wolverine host, Playwright scraping, OpenAI parsing)

### Messaging pattern (Wolverine + RabbitMQ)

```
API                                          Worker
 │                                              │
 │  Publish ScrapeJobUrl ──→ queue ──────────→ │
 │  Publish ParseJobText ──→ queue ──────────→ │
 │                                              │
 │  ←─────────── JobParsingCompleted ──────────│
 │  ←─────────── JobParsingFailed ─────────────│
 │                                              │
```

- API publishes commands to `job-description.commands` queue
- Worker listens on `job-description.commands` queue (max 3 concurrent)
- Worker publishes events to `job-description.events` queue
- API listens on `job-description.events` queue and updates database

### Worker project structure

```
TailorCV.JobDescriptions.Worker/
├── Program.cs                    # Host.CreateApplicationBuilder + Wolverine
├── ModuleExtensions.cs           # AddJobDescriptionWorkerServices
├── Handlers/
│   ├── ScrapeJobUrlHandler.cs
│   └── ParseJobTextHandler.cs
└── Infrastructure/
    ├── AI/
    │   ├── IJobDescriptionParserService.cs
    │   ├── OpenAiJobParserService.cs        # Real OpenAI with JSON schema
    │   └── OpenAiOptions.cs                  # ApiKey, Endpoint, ModelId, MaxTokens
    ├── Scraping/
    │   ├── IPlaywrightScrapingService.cs
    │   ├── PlaywrightScrapingService.cs      # Real Playwright + stealth
    │   └── PlaywrightOptions.cs              # MaxConcurrency, RequestTimeoutMs
    └── RateLimiting/
        ├── DomainRateLimiter.cs              # Per-domain TokenBucketRateLimiter
        └── DomainExtractor.cs                # Extract domain from URI
```

### Worker Docker image

Uses `playwright/dotnet` base image with .NET 10 runtime installed on top. Playwright Chromium is pre-bundled. Set `PLAYWRIGHT_SKIP_INSTALL=true` env var in production Docker runs.

### OpenAI Configuration

```json
{
  "OpenAI": {
    "ApiKey": "",           // User Secrets (never committed)
    "Endpoint": "https://openrouter.ai/api/v1",
    "ModelId": "gpt-4o",
    "MaxTokens": 2048
  }
}
```

## EF Core

- `UseSnakeCaseNamingConvention()` (NOT `UseSnakeCaseNaming()`)
- Migrations history table per schema: `MigrationsHistoryTable("__EFMigrationsHistory", "schema_name")`
- Design-time factories: one per module `DbContext`, hardcoded dev connection string
- Generated migration files live in `Infrastructure/Migrations/` — never modify them; suppress style rules via `.editorconfig` `[**/Migrations/**]` section

## NuGet Packages

- Central Package Management: `Directory.Packages.props` at repo root
- Add packages via `dotnet add package <Name>` only — this updates both CPM and csproj
- Build properties: `src/Directory.Build.props` (net10.0, SonarAnalyzer, nullable, treat warnings as errors)

## Common Suppressions

Use `#pragma warning disable/restore` per-file when needed:
- `CA1308` — for `ToLowerInvariant()` on email normalization
- `CA1862` — for intentional lowercase comparisons
- `S2139` — in logging decorator (log and rethrow)
- `CA1873` — expensive log args in decorator

## OpenAPI + Scalar

- `Scalar.AspNetCore` for API docs UI at `/scalar/v1` (dev only)
- `Microsoft.AspNetCore.OpenApi` for spec generation at `/openapi/v1.json`
- JWT Bearer security scheme via `BearerSecuritySchemeTransformer` (DI-activated, auto-detects auth schemes)
- Tag endpoints with `.WithTags("ModuleName")`, `.WithName()`, `.WithSummary()`, `.WithDescription()`

## What NOT to do

- Do NOT add `BCrypt.Net-Next` — password hashing uses built-in PBKDF2
- Do NOT use `var` — explicit types only
- Do NOT add `ValueObject` or `StronglyTypedId` base classes — use records when needed, plain `Guid` for IDs
- Do NOT reference another module's full project — only `.Contracts`
- Do NOT inject `IDateTimeProvider` into entities — pass `DateTimeOffset now` as a parameter
- Do NOT modify generated migration files
- Do NOT commit `obj/` or `bin/` directories
- Do NOT add code comments unless explicitly asked
- Do NOT commit secrets to `appsettings.json` or `appsettings.Development.json` — use User Secrets for secrets (OpenAI API key), non-secret config goes in `appsettings.Development.json`
