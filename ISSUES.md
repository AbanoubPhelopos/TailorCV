# Issues

## Bugs

### [BUG-001] `GetParseStatus` missing ownership check
**Severity:** High
**Module:** JobDescriptions
**File:** `src/Modules/JobDescriptions/TailorCV.JobDescriptions/Features/GetParseStatus.cs`
**Status:** Done

Fixed: Added `ICurrentUserService` injection and `UserId` filter to the query.

---

### [BUG-002] `CleanHtml` uses `string.Replace` instead of `Regex.Replace`
**Severity:** Medium
**Module:** JobDescriptions (Worker)
**File:** `src/Modules/JobDescriptions/TailorCV.JobDescriptions.Worker/Infrastructure/Scraping/PlaywrightScrapingService.cs`
**Status:** Done

Fixed: Changed to `Regex.Replace(html, @"\s+", " ")` for proper whitespace normalization.

---

### [BUG-003] PlaywrightScrapingService recursive retry can cause stack overflow
**Severity:** Medium
**Module:** JobDescriptions (Worker)
**File:** `src/Modules/JobDescriptions/TailorCV.JobDescriptions.Worker/Infrastructure/Scraping/PlaywrightScrapingService.cs`
**Status:** Done

Fixed: Refactored to iterative loop with `maxRetries = 3`, throws `InvalidOperationException` after exhaustion.

---

## Configuration Issues

### [CONFIG-001] Profile Worker not in docker-compose
**Severity:** Medium
**Module:** Profile
**Status:** Done

Added `profile-worker` service to `infra/docker-compose.yml` with its own Dockerfile.

---

### [CONFIG-002] Wolverine `ApplicationAssembly` set to wrong module
**Severity:** Medium
**Module:** API (Wolverine configuration)
**File:** `src/TailorCV.Api/Program.cs`
**Status:** Done

Changed to `typeof(TailorCV.Api.Services.CurrentUserService).Assembly`.

---

## Documentation Issues

### [DOC-001] `full.md` describes per-section CRUD, implementation uses bulk update
**Severity:** Low
**Module:** Profile
**Status:** Done

`docs/features/full.md` P-4 updated to describe `PUT /api/profiles/me/sections` bulk upsert. Individual CRUD endpoints removed from docs.

---

### [DOC-002] `docs/features/profile/section-crud.md` references non-existent endpoints
**Severity:** Low
**Module:** Profile
**Status:** Done

Rewritten as `UpdateSections` — single `PUT /api/profiles/me/sections` bulk upsert. `docs/features/profile/reorder-sections.md` deleted (redundant — reordering handled via `order` field in bulk upsert).

---

### [DOC-003] Architecture docs reference old JobScraper naming
**Severity:** Low
**Module:** Docs
**Status:** In Progress

Partially addressed: `docs/architecture/overview.md` Profile module table updated to show single `UpdateSections` endpoint. Remaining work:
- `JobScraper` → `JobDescriptions` in module diagrams and text
- `jobscraper.proto` → `jobdescriptions.proto`
- `jobscraper` schema → `jobdescriptions` schema
- `JobScraperDbContext` → `JobDescriptionsDbContext`
- `JobDescriptionSavedEvent` → `JobParsingCompleted` / `JobParsingFailed`
- Profile module: separate entity tables (Experience, Project, etc.) → JSONB sections

---

### [DOC-004] Dashboard module in docs but no code
**Severity:** Low
**Module:** Docs
**Status:** Open

`docs/features/overview.md` lists a "Dashboard" module (D-1 user story) but `src/Modules/Dashboard/` does not exist and no code has been written for it.

Either implement the Dashboard module or mark it as deferred in docs.

---

## Deferred / TODO Items

The following are known gaps but not bugs — tracking for planning purposes.

| ID | Item | Module | Notes |
|----|------|--------|-------|
| TODO-001 | Implement Templates module | Templates | Stub only — no code |
| TODO-002 | Implement CVGenerator module | CVGenerator | Stub only — no code |
| TODO-003 | Add gRPC stubs for all modules | All | Proto files exist but services not implemented |
| TODO-004 | OpenTelemetry instrumentation | All | Configured but may need verification |
| TODO-005 | Health check endpoint | API | `/health` not confirmed working |
| TODO-006 | Rate limiting middleware | API | Mentioned in docs but not verified in code |
