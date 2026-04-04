# CV Builder

A production-ready AI-powered CV generation platform built with .NET 8 Modular Monolith architecture and React.

## Features

### Architecture
- **Modular Monolith** - Five bounded contexts in a single deployment unit with schema-per-module PostgreSQL
- **Vertical Slice** - Feature-centric folders where every file for a use case lives together
- **CQRS Pattern** - MediatR for command/query dispatching with pipeline behaviors
- **Result Pattern** - ErrorOr discriminated union for error handling without exceptions
- **Background Jobs** - Hangfire with PostgreSQL persistence for AI generation

### Module Structure
- **Identity** - User authentication with JWT (RS256) + refresh token rotation
- **CVProfile** - CV profile management with typed JSON sections (jsonb)
- **JobDescription** - Job description input with Playwright URL scraping
- **CVGeneration** - AI-powered CV tailoring with GPT-4o
- **Templates** - Pre-defined template catalog with QuestPDF rendering
- **Export** - PDF (QuestPDF) and DOCX (Open XML SDK) generation

### Core Technologies

| Concern | Technology |
|---------|------------|
| Backend | ASP.NET Core 8.0, Minimal APIs, Carter |
| Database | PostgreSQL 16, Entity Framework Core 8 |
| CQRS | MediatR 12.x, FluentValidation 11.x |
| AI | OpenAI .NET SDK (GPT-4o, GPT-4o-mini) |
| Background Jobs | Hangfire 1.8.x |
| PDF Generation | QuestPDF 2024.x |
| DOCX Generation | DocumentFormat.OpenXml 3.x |
| OCR | Tesseract 5.x, PdfPig 0.1.x |
| Web Scraping | Microsoft.Playwright 1.x |
| Frontend | React 18.x, TypeScript 5.x, Vite 5.x |
| State Management | TanStack Query 5.x, Zustand 4.x |
| UI Components | shadcn/ui, Tailwind CSS 3.x |

### API Features
- **Minimal APIs** - Less ceremony than controllers with Carter endpoint grouping
- **RFC 7807 Problem Details** - Consistent error shape across all endpoints
- **Rate Limiting** - Per-policy limits (auth: 20/15min, AI generation: 10/hr, global: 100/min)
- **Health Checks** - `/health` with Npgsql and Hangfire readiness probes
- **Swagger/Scalar** - API documentation

### Security
- **JWT RS256** - Asymmetric tokens validated without signing key in frontend
- **Token Rotation** - Refresh tokens rotated on each use with revocation
- **Permission-based Auth** - DAC with resource:action format
- **Soft-delete** - All user-owned entities implement ISoftDeletable

## Project Structure

```
TailorCV/
├── TailorCV.slnx
├── src/
│   ├── TailorCV.Api/                      # Composition root
│   │   ├── Program.cs                     # Middleware pipeline, DI composition
│   │   ├── Middleware/                    # Global exception, request logging
│   │   ├── Extensions/                    # Service collection, migration helpers
│   │   └── Controllers/                   # (legacy, migrating to Carter)
│   │
│   ├── TailorCV.SharedKernel/             # Cross-cutting contracts
│   │   ├── Entity.cs                      # Base entity with Id
│   │   ├── Result.cs, Error.cs           # Result<T> discriminated union
│   │   ├── Messaging/                     # ICommand, IQuery, handlers
│   │   ├── Behaviors/                     # Validation, logging decorators
│   │   └── Interfaces/                    # IDateTimeProvider, IApplicationDbContext
│   │
│   ├── TailorCV.Infrastructure/           # Shared infrastructure
│   │   ├── Database/                      # ApplicationDbContext, interceptors
│   │   ├── Authentication/                # TokenProvider, PasswordHasher, UserContext
│   │   ├── Authorization/                  # Permission-based auth handlers
│   │   └── DomainEvents/                   # Event dispatcher
│   │
│   └── Modules/
│       ├── TailorCV.Modules.Identity/
│       │   ├── Domain/                    # ApplicationUser, RefreshToken
│       │   ├── Features/                  # Register, Login, Refresh, Logout
│       │   └── Services/                  # JwtTokenGenerator, CurrentUserService
│       │
│       ├── TailorCV.Modules.CVProfile/
│       │   ├── Domain/                    # CvProfile, CvSection, SectionType enum
│       │   ├── Features/                  # CreateProfile, GetProfile, UpdateSection
│       │   └── Services/                  # OcrService, AiCvTextParser
│       │
│       ├── TailorCV.Modules.JobDescription/
│       │   ├── Domain/                    # JobDescription, JobDescriptionSource enum
│       │   ├── Features/                  # SubmitJobDescription, ScrapeJobFromUrl
│       │   └── Services/                  # PlaywrightJobScraper
│       │
│       ├── TailorCV.Modules.CVGeneration/
│       │   ├── Domain/                    # GeneratedCv, GeneratedCvSection, GenerationStatus
│       │   ├── Features/                  # GenerateCv, GetGeneratedCv, RegenerateSection
│       │   ├── Services/                  # OpenAiCvGenerator, PromptBuilder
│       │   └── BackgroundJobs/            # ProcessCvGenerationJob
│       │
│       ├── TailorCV.Modules.Templates/
│       │   ├── Domain/                    # CvTemplate, TemplateCategory enum
│       │   └── Features/                  # ListTemplates, GetTemplate, SuggestTemplates
│       │
│       └── TailorCV.Modules.Export/
│           ├── Features/                  # ExportPdf, ExportDocx
│           └── Services/                  # QuestPdfTemplateRenderer, DocxRenderer
│
├── client/                                # React frontend
│   └── src/
│       ├── api/                          # Axios client with interceptors
│       ├── hooks/                        # TanStack Query hooks
│       ├── stores/                       # Zustand stores (auth, cv-editor)
│       ├── pages/                        # Route components
│       ├── components/                   # UI components (ui/, cv/, job/, generation/)
│       └── lib/                          # Utils, Zod validators
│
└── tests/                                # (infrastructure for tests)
```

### Module Dependency Rules

```
CVBuilder.Api → All Modules + SharedKernel + Infrastructure
Each Module → CVBuilder.SharedKernel (only)
Infrastructure → CVBuilder.SharedKernel

NO module references another module directly.
Inter-module communication uses MediatR notifications through SharedKernel.
```

### Database Schema Strategy

| Module | Schema | Purpose |
|--------|--------|---------|
| Identity | `identity` | Users, roles, permissions, refresh tokens |
| CVProfile | `cv_profile` | CV profiles and sections |
| JobDescription | `job` | Job descriptions with parsed keywords |
| CVGeneration | `generation` | Generated CVs and sections |
| Templates | `templates` | Template catalog |

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Docker & Docker Compose
- PostgreSQL 16+ (or use Docker)
- Node.js 18+ (for React frontend)

### Configuration

**Backend (`appsettings.json` or user secrets):**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=cvbuilder;Username=cvbuilder;Password=..."
  },
  "Jwt": {
    "Issuer": "CVBuilder",
    "Audience": "CVBuilder.Client",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "OpenAI": {
    "ApiKey": "sk-...",
    "Model": "gpt-4o",
    "FallbackModel": "gpt-4o-mini"
  },
  "Ocr": {
    "TessDataPath": "./tessdata"
  }
}
```

**Frontend (`client/.env`):**

```env
VITE_API_BASE_URL=http://localhost:5000/api
```

### Running with Docker

```bash
docker-compose up -d
```

Services:
- **API**: http://localhost:5000
- **Swagger**: http://localhost:5000/swagger
- **Scalar**: http://localhost:5000/scalar
- **Hangfire Dashboard**: http://localhost:5000/hangfire
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

The backend will:
1. Apply database migrations automatically on startup
2. Seed development template data
3. Run at http://localhost:5000

### Default Development Credentials

After seeding:
- **Email**: `admin@cvbuilder.local`
- **Password**: `Admin123!`

## API Endpoints

### Authentication (`/api/auth`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | Anonymous | Register new user |
| POST | `/api/auth/login` | Anonymous | Login, get JWT + refresh token |
| POST | `/api/auth/refresh` | Anonymous | Refresh access token |
| POST | `/api/auth/logout` | Bearer | Revoke refresh tokens |
| GET | `/api/auth/me` | Bearer | Get current user |

### CV Profile (`/api/cv-profiles`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/cv-profiles` | Bearer | Create CV profile |
| GET | `/api/cv-profiles` | Bearer | Get profile with all sections |
| PUT | `/api/cv-profiles/{id}` | Bearer | Update profile |
| POST | `/api/cv-profiles/{id}/sections` | Bearer | Add section |
| PUT | `/api/cv-profiles/{id}/sections/{sectionId}` | Bearer | Update section |
| DELETE | `/api/cv-profiles/{id}/sections/{sectionId}` | Bearer | Soft-delete section |
| POST | `/api/cv-profiles/{id}/import/text` | Bearer | Import from pasted text |
| POST | `/api/cv-profiles/{id}/import/ocr` | Bearer | Import from file (OCR) |

### Job Description (`/api/job-descriptions`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/job-descriptions` | Bearer | Submit job description |
| POST | `/api/job-descriptions/scrape` | Bearer | Scrape from URL (background) |
| GET | `/api/job-descriptions` | Bearer | List (paginated) |
| GET | `/api/job-descriptions/{id}` | Bearer | Get single |
| DELETE | `/api/job-descriptions/{id}` | Bearer | Delete |

### CV Generation (`/api/generation`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/generation` | Bearer | Generate CV (background job) |
| GET | `/api/generation/{id}` | Bearer | Get with status |
| GET | `/api/generation` | Bearer | List (paginated) |
| POST | `/api/generation/{id}/sections/{sectionId}/regenerate` | Bearer | Regenerate single section |
| PUT | `/api/generation/{id}/sections/{sectionId}` | Bearer | Manual edit |
| DELETE | `/api/generation/{id}` | Bearer | Delete |

### Templates (`/api/templates`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/templates` | Bearer | List (filterable) |
| GET | `/api/templates/{id}` | Bearer | Get details |
| GET | `/api/templates/suggest?jobDescriptionId={id}` | Bearer | Get suggestions |

### Export (`/api/export`)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/export/{generatedCvId}/pdf?templateId={id}` | Bearer | Export as PDF |
| GET | `/api/export/{generatedCvId}/docx?templateId={id}` | Bearer | Export as DOCX |

## AI Integration

### Model Usage

| Use Case | Model | Justification |
|----------|-------|---------------|
| Full CV regeneration | GPT-4o | Best quality for nuanced writing |
| Section regeneration | GPT-4o | Same quality, smaller scope |
| Text/OCR parsing | GPT-4o-mini | Cheaper, simpler extraction |
| Keyword extraction | GPT-4o-mini | Classification task |

### Generation Flow

1. User submits `POST /api/generation` with cvProfileId, jobDescriptionId, templateId
2. Backend creates `GeneratedCv` with `Status = Pending`
3. Hangfire background job processes:
   - Load CV profile + job description
   - Extract keywords (GPT-4o-mini)
   - Generate sections in parallel (GPT-4o)
   - Persist results, set `Status = Completed`
4. Frontend polls `GET /api/generation/{id}` until status = Completed

### Cost Estimation

| Operation | Model | Approximate Cost |
|-----------|-------|------------------|
| Full CV (5 sections) | GPT-4o | ~$0.10 |
| Single section regenerate | GPT-4o | ~$0.02 |
| Keyword extraction | GPT-4o-mini | ~$0.001 |
| Text parsing (OCR/paste) | GPT-4o-mini | ~$0.002 |

## Section Types

CV sections are stored as typed JSON in `jsonb` columns:

```json
// Experience
{ "entries": [{ "jobTitle": "...", "company": "...", "startDate": "2021-03", "bullets": [...] }] }

// Education
{ "entries": [{ "degree": "...", "institution": "...", "startDate": "2014-09", "endDate": "2018-06" }] }

// Skills
{ "categories": [{ "name": "Programming Languages", "skills": ["C#", "TypeScript"] }] }
```

## License

MIT