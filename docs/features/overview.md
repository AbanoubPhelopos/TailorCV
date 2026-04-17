# TailorCV — Feature Overview

## Modules

1. **Identity** — User management & authentication
2. **Profile** — User profiles with structured sections
3. **JobScraper** — Job description input & URL scraping
4. **Templates** — CV template management
5. **CVGenerator** — AI-powered CV generation engine
6. **Dashboard** — User activity overview

---

## Identity

### Phase 1 (MVP) — Simple JWT
- User registration & login
- JWT token generation & validation
- User roles (admin, user)
- Refresh tokens (simple rotation, no revocation)
- Logout (client-side token discard)

### Deferred
- Forgot/reset password
- Refresh token revocation/blacklist

### Phase 2 (Future) — Identity Server + OpenID Connect
- Full Identity Server implementation
- OpenID Connect / OAuth2 flows
- Social login integration
- SSO support

---

## Profile

### Entry Points
- **Manual entry** — Fill sections by hand
- **Resume upload** — Import from existing resume (PDF/DOCX) via OpenAI parsing

### Features
- Create & edit profile (one profile per user)
- Profile completeness indicator
- Export profile as JSON
- Share profile via public link (read-only visitor view)

### Sections
- **Experience** — company, role, duration, description
- **Projects** — name, tech stack, description, links
- **Skills** — categorized (languages, frameworks, tools, etc.)
- **Education** — degree, institution, year
- **Certifications**
- **Languages**
- Sections are reorderable and can be added/removed

### Deferred
- Delete profile
- Multiple profiles per user
- Import from LinkedIn

---

## JobScraper

- Manual job description text input
- URL input → Playwright scrapes and extracts the job description
- AI-powered JD parsing — extract: role title, required skills, responsibilities, qualifications
- Save JDs for later reuse
- JD history per user

---

## Templates

- Browse available CV templates
- Preview template with sample data
- Template categories/styles (minimal, professional, creative, etc.)
- Admin dashboard for managing templates
- Initial DB seed with default templates

### Future
- Custom template upload

---

## CVGenerator

### Core
- Select profile + job description + template → generate tailored CV
- AI-powered tailoring with custom summary/objective per JD
- Cover letter generation
- Match score (how well the profile matches the JD)

### Export
- Export as PDF via Puppeteer (HTML → PDF)

### Management
- Save generated CVs for re-download (versioning)
- Regenerate with a different template
- CV generation history

### Future
- Export as DOCX

---

## Dashboard

- Overview: profile completeness, recent CVs, average match score
- Recent activity feed
- Quick actions: generate CV, edit profile, submit JD

---

## Cross-cutting Concerns

- **RabbitMQ** — event bus for inter-module communication
- **OpenTelemetry** — distributed tracing & metrics across all modules
- **Dark/light mode** — frontend theme toggle

---

> See [full.md](./full.md) for detailed user stories and acceptance criteria.
> See [../architecture/overview.md](../architecture/overview.md) for architecture, project structure, and technology decisions.
