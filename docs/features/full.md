# TailorCV — Full Feature & User Story Specification

## Tech Decisions

| Decision | Choice |
|----------|--------|
| Auth (Phase 1) | Simple JWT (register, login, refresh rotation) |
| Auth (Phase 2) | Identity Server + OpenID Connect |
| Resume parsing | OpenAI API |
| Job scraping | Playwright (headless browser) |
| PDF generation | Puppeteer (HTML → PDF) |
| Templates | DB-seeded, admin-managed |
| Profile sharing | Public read-only link with visitor view |
| Cover letter | Included in CV generation |
| Event bus | RabbitMQ |
| Observability | OpenTelemetry |

---

## 1. Identity

### Phase 1 — Simple JWT (MVP)

#### I-1: Register
**As a** visitor  
**I want to** register with email and password  
**So that** I can create an account and use the platform  

**Acceptance Criteria:**
- POST `/api/auth/register` with `{ email, password, firstName, lastName }`
- Email must be valid format and unique (409 on duplicate)
- Password must meet strength rules (min 8 chars, uppercase, lowercase, digit, special)
- Returns `{ accessToken, refreshToken }` on success (201)
- Password stored hashed (bcrypt/Argon2)

#### I-2: Login
**As a** registered user  
**I want to** login with email and password  
**So that** I can access my account  

**Acceptance Criteria:**
- POST `/api/auth/login` with `{ email, password }`
- Returns `{ accessToken, refreshToken }` on success (200)
- Returns 401 on invalid credentials
- Returns 404 if email not found

#### I-3: Refresh Token
**As a** logged-in user  
**I want to** refresh my access token  
**So that** I stay authenticated without re-logging in  

**Acceptance Criteria:**
- POST `/api/auth/refresh` with `{ refreshToken }`
- Returns new `{ accessToken, refreshToken }` pair (simple rotation)
- Old refresh token is replaced (no blacklist/revocation in Phase 1)
- Returns 401 if refresh token is invalid or expired
- Refresh token has longer expiry than access token

#### I-4: Logout
**As a** logged-in user  
**I want to** logout  
**So that** my session ends  

**Acceptance Criteria:**
- POST `/api/auth/logout`
- Client discards both tokens
- No server-side revocation in Phase 1 (short access token expiry)
- Returns 200

### Phase 1 — Deferred

| # | Feature | Status |
|---|---------|--------|
| I-5 | Forgot password (email reset link) | Deferred |
| I-6 | Reset password | Deferred |
| I-7 | Refresh token revocation/blacklist | Deferred |

### Phase 2 — Future

| # | Feature | Status |
|---|---------|--------|
| I-8 | Identity Server implementation | Future |
| I-9 | OpenID Connect / OAuth2 flows | Future |
| I-10 | Social login integration | Future |
| I-11 | SSO support | Future |

---

## 2. Profile

### MVP

#### P-1: Create Profile
**As a** new user  
**I want to** create my professional profile  
**So that** I can build my data for CV generation  

**Acceptance Criteria:**
- POST `/api/profiles` — creates a single profile for the authenticated user
- One profile per user (409 if already exists)
- Profile has: `headline`, `summary`, `phone`, `location`, `website`, `linkedinUrl`, `githubUrl`
- Returns 201 with full profile

#### P-2: Get My Profile
**As a** user  
**I want to** view my profile  
**So that** I can see my current data  

**Acceptance Criteria:**
- GET `/api/profiles/me` — returns the authenticated user's profile
- Includes all sections (experience, projects, skills, education, certifications, languages)
- Returns 404 if profile doesn't exist yet

#### P-3: Update Profile
**As a** user  
**I want to** update my profile  
**So that** I can keep my information current  

**Acceptance Criteria:**
- PUT/PATCH `/api/profiles/me` — updates profile fields
- Can update headline, summary, contact info, URLs
- Only the owner can update their profile
- Returns updated profile

#### P-4: Manage Sections
**As a** user
**I want to** add, edit, reorder, and remove sections
**So that** I can structure my profile the way I want

**Acceptance Criteria:**
- PUT `/api/profiles/me/sections` — bulk upsert all sections (full state replacement)
  - `id: null` — creates new section
  - `id: "guid"` — updates existing section
  - Sections not in request are deleted
  - `order` field handles reordering (sequential 1..N)
  - `sectionType` cannot be changed on existing sections
- Single atomic transaction — all or nothing
- Returns updated sections + computed `completeness` percentage

**Section schema (shared across all types):**
```json
{
  "id": null,
  "sectionType": "Experience",
  "order": 1,
  "data": {
    "company": "Google",
    "role": "Senior Engineer",
    "startDate": "2022-01-01",
    "endDate": null,
    "description": "...",
    "isCurrent": true
  }
}
```

**Data per sectionType:**

| SectionType | Fields |
|-------------|--------|
| Experience | company\*, role\*, startDate\*, endDate, description, isCurrent |
| Project | name\*, description, techStack[], role, url, startDate, endDate |
| Skill | category\*, items[] |
| Education | institution\*, degree\*, field\*, startDate\*, endDate, gpa |
| Certification | name\*, issuer\*, date\*, expiryDate, url |
| Language | languageName\*, proficiency\* |
| Custom | title\*, items[] |

\* = required field

#### P-5: Import Profile from Resume
**As a** user  
**I want to** upload my existing resume (PDF/DOCX)  
**So that** my profile is auto-filled and I don't have to type everything  

**Acceptance Criteria:**
- POST `/api/profiles/me/import` with multipart file upload
- Supported formats: PDF, DOCX
- Backend sends file content to OpenAI for structured extraction
- Returns parsed sections for user review
- User confirms → sections are saved to profile
- File size limit: 5MB

#### P-6: Export Profile as JSON
**As a** user  
**I want to** export my profile data as JSON  
**So that** I can back it up or transfer it  

**Acceptance Criteria:**
- GET `/api/profiles/me/export` — returns downloadable JSON
- Includes all sections and metadata
- Content-Disposition header for download

#### P-7: Profile Completeness Indicator
**As a** user  
**I want to** see how complete my profile is  
**So that** I know what sections need filling  

**Acceptance Criteria:**
- GET `/api/profiles/me/completeness` — returns `{ percentage, missingSections[] }`
- Based on: has headline, has summary, at least 1 experience, at least 3 skills, etc.
- Suggestions for improvement

#### P-8: Share Profile
**As a** user  
**I want to** share my profile via a public link  
**So that** others can view my professional information  

**Acceptance Criteria:**
- POST `/api/profiles/me/share` — generates/returns a unique shareable URL
- GET `/api/profiles/shared/{shareId}` — public endpoint, no auth required
- Visitor sees a read-only profile view (different UI from owner view)
- Owner can enable/disable sharing
- Share link includes: name, headline, experience, projects, skills (configurable by owner)

### Deferred

| # | Feature | Status |
|---|---------|--------|
| P-9 | Delete profile | Deferred |
| P-10 | Multiple profiles per user | Deferred |
| P-11 | Import from LinkedIn | Deferred |

---

## 3. JobDescriptions

### MVP

#### J-1: Manual Job Description
**As a** user  
**I want to** manually paste a job description  
**So that** I can tailor my CV to it  

**Acceptance Criteria:**
- POST `/api/jobs/parse` with `{ rawText }`
- Backend sends to AI for structured extraction
- Returns parsed: `{ title, company, location?, requiredSkills[], responsibilities[], qualifications[], seniorityLevel? }`
- All parsed fields are editable by the user before saving

#### J-2: Scrape Job Description from URL
**As a** user  
**I want to** paste a job posting URL  
**So that** the system extracts the JD for me  

**Acceptance Criteria:**
- POST `/api/jobs/scrape` with `{ url }`
- URL validation (must be valid HTTP/HTTPS)
- Backend uses Playwright to fetch and render the page
- Extracts visible text content from the page
- Sends extracted text to AI for structured parsing
- Returns same parsed structure as J-1
- Timeout: 30 seconds max
- Returns error if page is unreachable or blocked (403, paywall, etc.)

#### J-3: Save Job Description
**As a** user  
**I want to** save a parsed job description  
**So that** I can reuse it later  

**Acceptance Criteria:**
- POST `/api/jobs` with parsed JD data + optional `{ label }`
- Saved to user's account with timestamp
- Returns 201 with job ID

#### J-4: List Job Descriptions
**As a** user  
**I want to** view my saved job descriptions  
**So that** I can reuse one for CV generation  

**Acceptance Criteria:**
- GET `/api/jobs` — paginated list
- Each item shows: title, company, label, date saved, match score (if any)
- Sortable by date, title

#### J-5: Get Job Description
**As a** user  
**I want to** view a specific saved JD  
**So that** I can review its details  

**Acceptance Criteria:**
- GET `/api/jobs/{id}` — returns full parsed JD
- Only accessible by the owner

---

## 4. Templates

### MVP

#### T-1: Browse Templates
**As a** user  
**I want to** browse available CV templates  
**So that** I can choose one that fits my style  

**Acceptance Criteria:**
- GET `/api/templates` — returns list of available templates
- Each template has: `id, name, description, thumbnailUrl, category, style`
- Filterable by category/style
- Only shows enabled/active templates

#### T-2: Preview Template
**As a** user  
**I want to** preview a template with sample data  
**So that** I can see how it looks before choosing  

**Acceptance Criteria:**
- GET `/api/templates/{id}/preview` — returns rendered HTML with placeholder data
- Shows layout, fonts, colors, section arrangement

#### T-3: Select Template for Generation
**As a** user  
**I want to** select a template when generating my CV  
**So that** the output matches my preferred style  

**Acceptance Criteria:**
- Template ID is passed as parameter to the CV generation endpoint
- Validates template exists and is active

#### T-4: Admin — Manage Templates
**As an** admin  
**I want to** add, edit, and disable templates  
**So that** I can manage available options for users  

**Acceptance Criteria:**
- Admin-only endpoints: POST/PUT/DELETE `/api/admin/templates`
- Template includes: HTML/CSS content, metadata, thumbnail
- Can enable/disable templates (disabled not shown to users)

#### T-5: Initial Seed
**Acceptance Criteria:**
- On first DB migration/seed, 3-5 default templates are inserted
- Each with distinct style (minimal, professional, creative)

---

## 5. CVGenerator

### MVP

#### G-1: Generate Tailored CV
**As a** user  
**I want to** generate a CV by combining my profile, a job description, and a template  
**So that** I get a tailored CV for a specific job  

**Acceptance Criteria:**
- POST `/api/cv/generate` with `{ profileId, jobId, templateId }`
- AI tailors content:
  - Generates custom summary/objective matching the JD
  - Highlights relevant experience & skills
  - Reorders sections to emphasize match
  - Omits irrelevant sections if they don't add value
- Returns generated CV content (structured) + match score
- Publishes `CVGenerated` event via RabbitMQ

#### G-2: Generate Cover Letter
**As a** user  
**I want to** generate a cover letter alongside my CV  
**So that** I have a complete application package  

**Acceptance Criteria:**
- POST `/api/cv/generate` with `{ ..., includeCoverLetter: true }`
- AI generates a cover letter based on profile + JD
- Cover letter is included in the response
- Can be generated independently: POST `/api/cv/cover-letter`

#### G-3: Match Score
**As a** user  
**I want to** see how well my profile matches a job description  
**So that** I know my chances  

**Acceptance Criteria:**
- Included in generation response: `{ matchScore: { percentage, matchingSkills[], missingSkills[] } }`
- Also available standalone: POST `/api/cv/match-score` with `{ profileId, jobId }`
- Score based on skills overlap, experience relevance, seniority match

#### G-4: Preview Generated CV
**As a** user  
**I want to** preview the generated CV in my browser  
**So that** I can review it before exporting  

**Acceptance Criteria:**
- GET `/api/cv/{id}/preview` — returns rendered HTML
- Full visual render using the selected template
- User can edit/tweak content fields before final export

#### G-5: Export as PDF
**As a** user  
**I want to** export my generated CV as a PDF  
**So that** I can submit it with my job application  

**Acceptance Criteria:**
- GET `/api/cv/{id}/export/pdf`
- Backend renders HTML via Puppeteer → PDF
- Clean output matching the template design
- File naming: `{firstName}_{lastName}_{jobTitle}_{date}.pdf`
- Returns PDF as downloadable file

#### G-6: Save CV to History
**As a** user  
**I want to** save my generated CVs  
**So that** I can re-download or review them later  

**Acceptance Criteria:**
- CVs are automatically saved on generation
- Metadata: profile snapshot, JD snapshot, template, date, match score
- Stored for retrieval and re-download

#### G-7: Regenerate with Different Template
**As a** user  
**I want to** regenerate my CV with a different template  
**So that** I can try different styles  

**Acceptance Criteria:**
- POST `/api/cv/{id}/regenerate` with `{ templateId }`
- Same profile + JD, new template
- Preserves any manual content edits if made
- Returns new CV with new ID

#### G-8: View CV History
**As a** user  
**I want to** view my past generated CVs  
**So that** I can track what I've created  

**Acceptance Criteria:**
- GET `/api/cv` — paginated list
- Each item: date, job title, company, template name, match score
- Sortable by date, score

---

## 6. Dashboard

### MVP

#### D-1: User Dashboard
**As a** user  
**I want to** see a dashboard with an overview of my activity  
**So that** I can quickly access key information  

**Acceptance Criteria:**
- GET `/api/dashboard` — returns:
  - Profile completeness %
  - Number of generated CVs
  - Average match score
  - Recent activity (last 5 generated CVs)
  - Quick actions: generate CV, edit profile, submit JD
