# TailorCV

An AI-powered CV generation platform that helps job seekers create tailored resumes by matching their existing CVs against job descriptions using GPT-4o.

## Core Idea

Job seekers spend excessive time customizing CVs for each application. TailorCV automates this process by:

1. **Importing** existing CVs via manual entry, text paste, or OCR from uploaded documents
2. **Matching** against job descriptions scraped from URLs or manually entered
3. **Generating** AI-tailored CV content optimized for ATS compatibility
4. **Exporting** professional PDF and DOCX documents with multiple template options

## Architecture

- **Modular Monolith** — Five bounded contexts (Identity, CVProfile, JobDescription, CVGeneration, Templates, Export) sharing a single database with schema-per-module isolation
- **Vertical Slice** — Feature folders containing controller, service, and repository for each use case
- **Controller-based API** — Traditional MVC controllers for clear endpoint grouping
- **CQRS** — MediatR for command/query handling with pipeline behaviors (validation, logging)
- **Result Pattern** — ErrorOr discriminated unions for explicit error handling

## Technology Stack

### Backend

| Concern | Technology |
|---------|------------|
| Framework | ASP.NET Core 8.0 |
| API Style | Controllers with action results |
| ORM | Entity Framework Core 8.x |
| Database | PostgreSQL 16 |
| CQRS/Mediator | MediatR 12.x |
| Validation | FluentValidation 11.x |
| Result Pattern | ErrorOr 2.x |
| Auth | JWT Bearer (RS256), Refresh Token Rotation |
| Background Jobs | Hangfire 1.8.x with PostgreSQL |
| AI Integration | OpenAI .NET SDK (GPT-4o, GPT-4o-mini) |
| PDF Generation | QuestPDF 2024.x |
| DOCX Generation | DocumentFormat.OpenXml 3.x |
| OCR | Tesseract 5.x, PdfPig 0.1.x |
| Web Scraping | Microsoft.Playwright 1.x |
| Logging | Serilog 3.x with Seq |
| API Documentation | Swagger |

### Frontend

| Concern | Technology |
|---------|------------|
| Framework | React 18.x |
| Language | TypeScript 5.x |
| Build Tool | Vite 5.x |
| Routing | React Router 6.x |
| Server State | TanStack Query 5.x |
| Client State | Zustand 4.x |
| Forms | React Hook Form 7.x with Zod |
| UI Components | shadcn/ui (Radix UI + Tailwind) |
| HTTP Client | Axios 1.x |
| Icons | Lucide React |

## Project Structure

```
TailorCV/
├── TailorCV.slnx
├── src/
│   ├── TailorCV.Api/                      # Controllers, middleware, composition root
│   │
│   ├── TailorCV.SharedKernel/             # Cross-cutting contracts
│   │   ├── Entity.cs                       # Base entity
│   │   ├── Result.cs, Error.cs             # Result<T> discriminated union
│   │   ├── Behaviors/                      # Validation, logging pipeline behaviors
│   │   └── Interfaces/                     # IDateTimeProvider, IApplicationDbContext
│   │
│   ├── TailorCV.Infrastructure/            # Shared infrastructure
│   │   ├── Database/                       # ApplicationDbContext, interceptors
│   │   ├── Authentication/                # JWT, password hashing, user context
│   │   └── Services/                      # File storage, email (future)
│   │
│   └── Modules/
│       ├── TailorCV.Modules.Identity/      # User auth, roles, permissions
│       ├── TailorCV.Modules.CVProfile/    # CV profiles and sections
│       ├── TailorCV.Modules.JobDescription/  # Job descriptions, URL scraping
│       ├── TailorCV.Modules.CVGeneration/ # AI generation, background jobs
│       ├── TailorCV.Modules.Templates/    # Template catalog
│       └── TailorCV.Modules.Export/       # PDF/DOCX rendering
│
├── client/                                # React frontend
│   └── src/
│       ├── api/                           # Axios client
│       ├── hooks/                         # TanStack Query hooks
│       ├── stores/                         # Zustand stores
│       ├── pages/                         # Route pages
│       └── components/                    # UI components
│
└── tests/                                 # Test projects
```

### Module Dependencies

```
TailorCV.Api → All Modules + SharedKernel + Infrastructure
Each Module → TailorCV.SharedKernel (only)
Infrastructure → TailorCV.SharedKernel

NO module references another module directly.
```

### Database Schema Strategy

| Module | Schema | Purpose |
|--------|--------|---------|
| Identity | `identity` | Users, roles, permissions |
| CVProfile | `cv_profile` | CV profiles and sections |
| JobDescription | `job` | Job descriptions |
| CVGeneration | `generation` | Generated CVs |
| Templates | `templates` | Template catalog |

## Key Features

### AI-Powered Generation
- GPT-4o tailors CV content section-by-section against job descriptions
- Parallel generation for faster processing
- Original vs generated diff view with regeneration options
- Keyword extraction from job descriptions for ATS optimization

### Import Pipeline
- Text paste with AI-structured parsing
- PDF text extraction (PdfPig) + OCR (Tesseract) for scanned documents
- User review before saving parsed sections

### Background Processing
- Hangfire handles AI generation as background jobs
- URL scraping with Playwright runs asynchronously
- Persistent job queue with retry policies

### Export Options
- PDF via QuestPDF with multiple template layouts
- DOCX via Open XML SDK for Word compatibility
- Live preview before download

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Docker & Docker Compose
- Node.js 18+ (for frontend)

### Configuration

**User secrets for local development:**

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=tailorcv;Username=tailorcv;Password=..."
dotnet user-secrets set "Jwt:SigningKey" "your-rsa-private-key-or-hmac-secret-at-least-32-chars"
dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
```

### Running with Docker

```bash
docker-compose up -d
```

Services:
- **API**: http://localhost:5000
- **PostgreSQL**: localhost:5432
- **Seq (Logs)**: http://localhost:5341

### Running Locally

**Backend:**
```bash
cd TailorCV
dotnet restore
dotnet build
dotnet run --project src/TailorCV.Api
```

**Frontend:**
```bash
cd TailorCV/client
npm install
npm run dev
```

## License

MIT