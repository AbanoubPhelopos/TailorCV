# TailorCV — Feature Overview

## Modules

1. **Identity** — User management & authentication
2. **Profile** — User profiles with structured sections
3. **JobDescriptions** — Job description input & URL scraping
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

## JobDescriptions

- Manual job description text input
- URL input → Playwright scrapes and extracts the job description
- AI-powered JD parsing — extract: role title, required skills, responsibilities, qualifications
- Save JDs for later reuse
- JD history per user

---

## Templates

### User Features
- Browse available CV templates (filterable by category/style)
- Preview template with sample placeholder data (rendered HTML)
- Select template when generating a CV (template ID passed to generation endpoint)

### Admin Features
- Upload template thumbnails via presigned S3 URL (2 MB max, PNG/JPEG/WebP)
- Create new templates (HTML/CSS content + metadata + uploaded thumbnail)
- Update existing templates (full replacement of content/metadata + optional new thumbnail)
- Disable templates (soft-delete — hidden from users, reactivatable)
- Templates have: name, description, htmlContent, cssContent, thumbnailUrl, category, style, isActive

### Categories & Styles
- **Categories:** minimal, professional, creative
- **Styles:** modern, classic, bold

### Seeding
- On first migration, 3-5 default templates are inserted (one per category)
- Seeded via `TemplateSeeder` on startup

### Deferred
- Custom template upload by users
- Template versioning

---

## CVGenerator

### Generation
- Select profile + job description + template → generate tailored CV (async)
- AI-powered tailoring with natural language prompt steering (`tailoringPrompt`)
- Match score included in generation response (algorithmic: skills overlap, seniority match)
- Cover letter generation (tied to an existing CV, async)
- Profile and JD data snapshotted at generation time (decoupled from later edits)

### Editing & Preview
- Edit AI-generated content before export (`PUT /api/cv/{id}/content`)
- Client-side preview rendering: frontend combines CV content JSON + template HTML/CSS in iframe
- Content edits invalidate cached PDFs (re-export required)

### Export
- Export as PDF via PuppeteerSharp (async, cached until content changes)
- PDF stored in S3 (RustFS), served as download

### History & Management
- All generated CVs saved to history automatically
- Regenerate with a different template or new tailoring prompt (creates new CV, original preserved)
- Paginated CV history list
- Full CV details including snapshots, content, match score, cover letter

### Async Operations (Wolverine)
- CV generation, cover letter generation, and PDF export are all async with polling
- Background handlers fetch data via gRPC (Profile, JobDescriptions, Templates modules)

### Future
- Standalone match score endpoint
- Export as DOCX
- Delete generated CVs

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
