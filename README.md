# TailorCV

AI-powered CV generator. Users create profiles, input job descriptions, and generate tailored CVs.

## Architecture

Modular monolith (.NET 10) with vertical slice architecture. One `.cs` file per feature. Separate PostgreSQL schema per module. Modules communicate async via Wolverine + RabbitMQ (planned) or sync via gRPC (planned).

## Quick Start

```bash
# Start infrastructure (PostgreSQL only needed for dev)
docker compose -f infra/docker-compose.yml up -d postgres

# Run the API (auto-migrates in Development)
dotnet run --project src/TailorCV.Api

# API: http://localhost:5062
# Scalar docs: http://localhost:5062/scalar/v1
# OpenAPI JSON: http://localhost:5062/openapi/v1.json
# Health: http://localhost:5062/health
```

## Solution Structure

```
src/
├── TailorCV.Api/              # Host (Minimal APIs, JWT auth, Scalar OpenAPI)
├── TailorCV.Shared/           # Shared kernel (Result, Error, CQRS interfaces, decorators)
├── TailorCV.Infrastructure/   # Shared infra (S3, Redis, OTel — stubs)
├── protos/                    # gRPC contracts (empty placeholders)
└── Modules/
    ├── Identity/              # ✅ Implemented — register, login, refresh, logout
    ├── Profile/               # 🔧 Stubs — domain scaffolded, features empty
    ├── JobScraper/            # 🔧 Stubs — domain scaffolded, features empty
    ├── Templates/             # 📋 Directory only
    └── CVGenerator/           # 📋 Directory only
```

## Tech Stack

- **Runtime:** .NET 10, ASP.NET Core Minimal APIs
- **Database:** PostgreSQL + EF Core (snake_case via `UseSnakeCaseNamingConvention()`)
- **Auth:** JWT Bearer (PBKDF2 password hashing, refresh token rotation)
- **API Docs:** Microsoft.AspNetCore.OpenApi + Scalar
- **Observability:** Serilog + OpenTelemetry + Grafana LGTM stack
- **Analysis:** SonarAnalyzer.CSharp (warnings as errors)
- **Package Management:** Central Package Management (`Directory.Packages.props`)

## Build & Verify

```bash
dotnet build TailorCV.slnx   # Must pass with 0 errors, 0 warnings
```

## Migrations

Each module has its own `DbContext` with a dedicated PostgreSQL schema. Design-time factories use hardcoded dev connection strings.

```bash
# Example: adding a migration for a module
dotnet ef migrations add <Name> \
  --project src/Modules/<Module>/TailorCV.<Module> \
  --startup-project src/TailorCV.Api \
  --output-dir Infrastructure/Migrations
```

Auto-migration runs on startup in Development via `MigrateIdentityModuleAsync()`.

## Infrastructure

```bash
# Full stack
docker compose -f infra/docker-compose.yml up -d

# Individual services
docker compose -f infra/docker-compose.yml up -d postgres    # PostgreSQL (port 5432)
docker compose -f infra/docker-compose.yml up -d rabbitmq    # RabbitMQ (port 5672/15672)
docker compose -f infra/docker-compose.yml up -d redis       # Redis (port 6379)
```

## Documentation

- `docs/architecture/overview.md` — full architecture reference (~1600 lines)
- `docs/architecture/project-dependencies.md` — project references and NuGet packages
- `docs/features/` — per-feature specs with mermaid diagrams (identity, profile, jobscraper)
