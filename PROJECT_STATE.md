# OpsFlow — Project State & Decision Log

> Last updated: 2026-06-19
> Sessions covered: Waves 0–15 (initial build) + two design/data sessions

---

## 1. What OpsFlow Is

OpsFlow is an **operational checklist and task management platform** for multi-store food & beverage operators (current client: Bajco, a Domino's franchisee). It replaces paper-based daily ops sheets (like the F0890 daily page) with a digital system that:

- Assigns checklist tasks to stores on a recurring schedule
- Lets field employees complete tasks with structured form fields (yes/no, numeric, text)
- Gives managers and supervisors a real-time completion dashboard
- Triggers corrective actions when out-of-range values are submitted
- Tracks form submissions (incident reports, audits, etc.)

---

## 2. Architecture

### Stack

| Layer | Technology |
|---|---|
| Frontend — Admin Dashboard | Angular 21, Nx monorepo, `apps/dashboard` (port 4200) |
| Frontend — Field PWA | Angular 21, Nx monorepo, `apps/field-pwa` (port 4201) |
| Shared Libraries | `libs/data-access/auth`, `libs/data-access/tasks`, `libs/ui/template-builder` |
| Backend API | .NET 9, Vertical Slice Architecture, MediatR, Minimal APIs (port 5000) |
| Auth | Supabase Auth (JWT minting via backend, httpOnly refresh cookie) |
| Database | PostgreSQL via Supabase (dev) / Azure (prod) |
| Real-time | SignalR (`/hubs/taskboard`) |

### Angular Architecture Rules (from CLAUDE.md)
- Standalone Components only — no NgModules
- Angular Signals for all reactive state
- Strict TypeScript throughout
- Vertical slice: every feature spans UI + service + model simultaneously

### Backend Architecture Rules
- Vertical Slice Architecture: each feature has its own folder with `Handler`, `Command/Query`, `Validator`
- Repository + IUnitOfWork pattern
- Multi-tenant: every DB table has a `TenantId text` column
- 3 adapter interfaces: `IAuthProvider`, `IStorageProvider`, `INotificationService`

### Multi-Tenancy
- Tenant ID: `bajco-dev`
- All Supabase users have `tenant_id`, `role`, `store_id`, `region_id` in `user_metadata`
- Backend reads these from JWT claims minted at login

---

## 3. Domain Data Model

```
Regions (3)
  └── Stores (6)
       └── Checklists (15) — one per checklist type per store
            └── ChecklistTemplateItems — ordered junction to TaskTemplates
                 └── TaskTemplates (86) — with Fields JSONB
       └── RecurringAssignments (15) — schedule rules
       └── TaskInstances (103) — actual daily task records
            └── TaskCompletions — submitted field values
       └── UserProfiles (15) — role + storeId
       └── StoreSettings
FormTemplates (4)
FormSubmissions (8)
```

### Key Schema Notes
- `TaskTemplates.Fields` is **JSONB** — array of `{id, label, type, required, rangeMin, rangeMax, correctiveActionText, subItems}`
- Field types: `Boolean`, `Numeric`, `Text`, `Checklist` (with sub-items), `Photo`
- `ChecklistTemplateItems` is a junction table: `(ChecklistId, TemplateId, Order)` — no own PK beyond those columns
- `RefreshTokens` stores `StoreId` and `RegionId` so refresh can reconstruct the full JWT without re-hitting Supabase

---

## 4. Org Structure (Demo Data)

### Regions
| Region | Stores |
|---|---|
| North Region | Downtown Flagship (store1), Westside Branch (store2) |
| South Region | Southgate Mall, Brixton Road |
| East Region | Canary Wharf, Stratford City |

### Users (15 total)
| Role | Count | Notes |
|---|---|---|
| `admin` | 1 | `admin@bajco.net` — no store |
| `store_manager` | 6 | One per store |
| `store_employee` | 5 | One per store; main demo user: `employee@bajco.net` → Downtown Flagship |
| `supervisor` | 3 | One per region |

**All passwords: `Demo1234!`**

### Checklists per Store
- **Downtown Flagship (main):** Morning Opening, Evening Closing, Midday Safety & Compliance, Afternoon Stock Rotation, Pre-Close Manager Sign-Off
- **All other stores:** `{StoreName}: Morning Opening`, `{StoreName}: Evening Closing`

---

## 5. Real Checklist Content (from F0890 PDF)

The PDF `F0890 DP sample.pdf` is the actual Domino's-style daily operations page. We extracted all checklist items from it and seeded them as real `TaskTemplate` fields.

### Morning Opening (59 fields across 6 templates)

| Template | Fields | Key Items |
|---|---|---|
| Opening Tasks | 13 | Arrival time, security walkthrough, make line setup, MDOG print, expiry checks |
| 3-Day Dough & Cheese Management (MDOG) | 21 | Walk-in temp, makeline temp, on-hand counts + 3-day needs for 10"/12"/14"/16"/Dia dough + cheese |
| Opening Cash Management | 5 | Safe opening amount, Till A, Till B, manager initials, deposit confirmation |
| Set Up Cut Table | 6 | Cutter rack, garlic cups, wings sauce, side cups all dated |
| Set Up Sauce Station | 4 | Fresh sauce, BBQ sauce, garlic sauce, sanitizer buckets |
| Set Up Customer Lobby | 10 | Floor mat, window ledges, Pepsi cooler, parking lot, label check, 10 AM deadline items |

### Evening Closing (62 fields across 4 templates)

| Template | Fields | Key Items |
|---|---|---|
| Pre-Close Walk Through | 11 | Make line stocked, sweep, trash, dough pull, dishes — all by 3:30 PM |
| Deployment Guide | 15 | Pepsi FIFO, dishes, bathroom, walk-in mop, oven, monitors, phones, undershelves |
| Closing Checklist | 19 | Lobby, driver area, front counter, slap station, sauce station, make line, dish area |
| Closing Admin & Cash | 17 | Till A/B at target, final deposit, nightly numbers, bad order log, Instant Pay, clock out |

**Script:** `execution/seed_real_checklists.py`

---

## 6. Auth Flow

```
Login (POST /auth/login)
  → Supabase.SignIn → reads user_metadata (role, store_id, etc.)
  → Mint JWT (15 min, claims: sub, tenantId, role, storeId, regionId)
  → Store RefreshToken in DB (30 days, includes StoreId for later reconstruction)
  → Set httpOnly cookie: "refresh_token" = "{tenantId}:{rawToken}"
  → Return { accessToken, expiresIn }

Refresh (POST /auth/refresh)
  → Read cookie → parse tenantId + rawToken
  → Hash token → find in RefreshTokens (not used, not expired)
  → Mark old as used → issue new RefreshToken (rotation)
  → Set new cookie → mint new JWT with stored StoreId/RegionId
  → Return { accessToken, expiresIn }
```

### Cookie Config (key decision)
- `Secure = !env.IsDevelopment()` — must be `false` over `http://localhost` or the cookie is never sent
- `SameSite = Lax` in dev, `Strict` in prod

### Angular Auth (key decision)
- `_accessToken` is an in-memory Angular Signal — lost on page reload
- `provideAppInitializer()` (Angular 21 API) calls `auth.refresh()` at startup before any route guard runs
- `authGuard` is synchronous — reads signal directly, redirects to `/login` if not authenticated
- Token refresh runs proactively every 13 minutes in `TasksComponent` to keep session alive

---

## 7. Bug Fixes Landed

### DateTimeOffset UTC Bug
**File:** All four dashboard handlers + `GetTodayTasksHandler`

**Problem:** `DateTimeOffset.UtcNow.Date` returns a `DateTime` with `Kind=Unspecified`, which Npgsql rejects because PostgreSQL `timestamp with time zone` only accepts UTC offsets.

**Fix:**
```csharp
// Before (bug):
var today = DateTimeOffset.UtcNow.Date;

// After (fix):
var today    = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero);
var tomorrow = today.AddDays(1);
```

**Affected:** `GetTodayTasksHandler`, `GetSystemDashboardHandler`, `GetStoreDashboardHandler`, `GetRegionDashboardHandler`

### Refresh Cookie Never Sent Over Localhost
**File:** `TokenService.cs`

**Problem:** `Secure = true` prevents browsers from sending the cookie over `http://` (localhost dev).

**Fix:** `Secure = !env.IsDevelopment()` — conditional on environment.

### Angular APP_INITIALIZER Broken on Angular 21
**File:** `libs/data-access/auth/src/lib/auth.initializer.ts`

**Problem:** The old `APP_INITIALIZER + useFactory + inject()` pattern is unreliable in Angular 21. Caused a race condition where the router guard ran before `auth.refresh()` completed, letting unauthenticated users through to the tasks page — which then showed "No store assigned to your account."

**Fix:** Switched to `provideAppInitializer()` (Angular 19+ API):
```typescript
export function provideAuthInitializer() {
  return provideAppInitializer(() => {
    const auth = inject(AuthService);
    return auth.refresh();
  });
}
```

### "No Store Assigned" Instead of Redirect
**File:** `tasks.component.ts`

**Problem:** When the session expired, `load()` showed a cryptic error instead of redirecting.

**Fix:** `if (!storeId) { this.router.navigate(['/login']); return; }`

---

## 8. Design System — Meal Dynamics

The UI uses the **Meal Dynamics design system** — a warm editorial aesthetic built for food & beverage brands.

### Tokens
```scss
--cream:       #F5EFE0   // page background
--cream-deep:  #EDE5D0   // hover states, table rows
--paper:       #FEFCF8   // card backgrounds
--ink:         #15131F   // primary text, dark buttons
--ink-soft:    #3D3A4A   // secondary text
--indigo:      #5B4FE9   // primary accent (light surfaces only)
--amber:       #FBBE3D   // accent on dark surfaces (ink bg)
--amber-deep:  #C47A1E   // amber on light surfaces
--rust:        #C84A2C   // errors, alerts
--green:       #2A7F4F   // success states
--muted:       #6B6678   // placeholder text, labels
--line:        #E8E2D6   // borders, dividers
```

### Typography
- **Serif:** Fraunces (variable) — headings, stats, numbers. Weight 280–400. Never bold.
- **Sans:** Manrope — body, labels, buttons. Weight 400–700.
- **Mono:** JetBrains Mono — eyebrow labels, badges, meta text. Always uppercase + letter-spacing.

### Key Design Rules
- Indigo (`--indigo`) only on **light** backgrounds (cream/paper)
- Amber (`--amber`) only on **dark** backgrounds (ink sidebar, dark cards)
- All buttons are **pill-shaped** (`border-radius: 100px`)
- Eyebrow labels always have `::before` line: `width: 14px; height: 1px; background: currentColor`
- Fraunces headings use `font-variation-settings: 'opsz' 72, 'SOFT' 30` for editorial softness
- Stat numbers: Fraunces italic, amber-deep color, weight ~360

### Files Updated

| File | Status |
|---|---|
| `field-pwa/src/styles.scss` | ✅ Full rewrite — all tokens, utilities, pills, cards |
| `dashboard/src/styles.scss` | ✅ Full rewrite — tokens + data-table, form fields |
| `field-pwa/src/index.html` | ✅ Google Fonts import |
| `dashboard/src/index.html` | ✅ Google Fonts import |
| `field-pwa/src/app/login/login.component.scss` | ✅ Cream bg, radial glow, paper card, brand mark |
| `field-pwa/src/app/login/login.component.html` | ✅ Brand mark + eyebrow + serif title |
| `dashboard/src/app/login/login.component.scss` | ✅ Same as field-pwa, 420px width |
| `dashboard/src/app/login/login.component.html` | ✅ Brand mark + eyebrow + serif title |
| `field-pwa/src/app/tasks/tasks.component.scss` | ✅ Cream bg, MDOG eyebrow, paper group cards, indigo progress bar |
| `field-pwa/src/app/app.scss` | ✅ Indigo FAB, blur backdrop, paper bottom sheet |
| `field-pwa/src/app/task-detail/task-detail.component.scss` | ✅ Full Meal Dynamics redesign |
| `dashboard/src/app/admin/admin-shell/admin-shell.component.scss` | ✅ Ink sidebar, amber active border, radial glow |
| `dashboard/src/app/admin/admin-shell/admin-shell.component.html` | ✅ New brand mark HTML structure |
| `dashboard/src/app/admin/overview/overview.component.scss` | ✅ Serif stats, alerts panel, region table |
| `dashboard/src/app/admin/shared/admin.scss` | ✅ Full rewrite — BEM page structure, tables, buttons, slide-overs, form fields |

### Files Still Using Old Styles (Remaining Work)

| File | Notes |
|---|---|
| `dashboard/src/app/admin/overview/overview.component.html` | Uses `page-title`/`page-subtitle` flat names; admin.scss uses BEM `page__title` |
| `dashboard/src/app/admin/stores/` | All pages use old HTML class structure |
| `dashboard/src/app/admin/users/` | All pages use old HTML class structure |
| `dashboard/src/app/admin/checklists/` | All pages use old HTML class structure |
| `dashboard/src/app/admin/templates/` | All pages use old HTML class structure |
| `dashboard/src/app/admin/regions/` | All pages use old HTML class structure |
| `dashboard/src/app/admin/form-submissions/` | Old styles |
| `field-pwa/src/app/submissions/` | Old styles |
| `field-pwa/src/app/tasks/tasks.component.html` | Eyebrow/title section could be enhanced |

---

## 9. Execution Scripts

All scripts in `execution/`. No interactive prompts; all read `.env` for credentials.

| Script | Purpose |
|---|---|
| `seed_demo.py` | Seeds initial org structure: 1 region, 2 stores, 4 users, checklists, templates, recurring assignments, task instances |
| `enrich_demo.py` | Adds 2 more regions, 4 more stores, 10 more users, richer task instances (73 total), extra form templates and submissions |
| `seed_real_checklists.py` | **Key script** — replaces placeholder template items on Morning Opening and Evening Closing checklists with real fields from F0890 PDF |
| `check_db.py` | Quick DB health check |
| `check_columns.py` | Inspect table schemas |

---

## 10. Dev Server Setup

```bash
# Backend (.NET 9)
cd backend && dotnet run --project OpsFlow.Api
# → http://localhost:5000

# Frontend (both apps)
cd frontend
npx nx serve field-pwa     # → http://localhost:4201
npx nx serve dashboard     # → http://localhost:4200

# Build check
cd frontend && NX_NO_CLOUD=true npx nx run-many --target=build --projects=field-pwa,dashboard
```

### Proxy Config
Both apps proxy `/api/*` → `http://localhost:5000` (strips `/api` prefix) and `/hubs/*` → `http://localhost:5000` (WebSocket).

---

## 11. Remaining Work (Prioritized)

### P1 — Verify (Quick)
- [ ] Tap a Morning Opening task in field-pwa → confirm 6 sections + real fields render
- [ ] Confirm Evening Closing task renders 4 sections
- [ ] Log out and log back in → confirm no stale session error

### P2 — Dashboard Design Alignment (Medium)
- [ ] Fix HTML class name mismatch: overview component uses `page-title`/`page-subtitle` but admin.scss now uses `page__title`/`page__subtitle` (BEM). Either update all HTML or revert admin.scss to flat names.
- [ ] Apply Meal Dynamics to: Stores, Users, Checklists, Templates, Regions, Form Submissions pages

### P3 — Data (Low)
- [ ] Seed real fields into the other-store checklists (Westside, Southgate, Brixton, Canary, Stratford) — currently have placeholder templates. Could reuse the same templates via `ChecklistTemplateItems` rather than duplicating.

### P4 — Features (Future)
- [ ] Photo upload for `Photo`-type fields
- [ ] Push notifications when tasks become overdue
- [ ] Manager dashboard for per-store task board
- [ ] Form submission approval workflow (schema exists, UI incomplete)

---

## 12. Security Notes

> **Action required after each session:**
> - Rotate the Supabase service role key (was exposed in chat)
> - Reset DB password (was exposed in chat)
> - Regenerate JWT secret (saved to `.env`)

**Never commit:**
- `.env` (gitignored) — contains `SUPABASE_SERVICE_ROLE_KEY`, `MASTER_DB_CONNECTION_STRING`, `TENANT_DB_CONNECTION_STRING`, `JWT_SECRET`
- `credentials.json`, `token.json` — Google OAuth files

---

## 13. Key Architectural Decisions

| Decision | What | Why |
|---|---|---|
| JWT + httpOnly cookie | Access token in memory (Signal), refresh token in httpOnly cookie | Security: access token never touches localStorage; refresh survives page reload |
| `Secure=false` in dev | `Secure = !env.IsDevelopment()` | Browsers reject `Secure` cookies over `http://localhost` |
| Token rotation on refresh | Refresh token marked `IsUsed=true` on every use | Prevents replay attacks |
| StoreId in RefreshToken row | DB stores StoreId with the refresh token | Allows JWT reconstruction without re-calling Supabase on every refresh |
| `provideAppInitializer` | Angular 21 API (not `APP_INITIALIZER`) | Old pattern unreliable in Angular 19+; causes race between guard and refresh |
| JSONB for template fields | `TaskTemplates.Fields` is JSONB array | Avoids EAV table explosion; fields are schema-flexible per template type |
| ChecklistTemplateItems junction | Checklist → ordered list of TaskTemplates | Same template can be reused across multiple checklists (MDOG appears in both Morning and could appear elsewhere) |
| Store-prefixed checklist names | "Westside: Morning Opening" instead of "Morning Opening" | DB UNIQUE constraint on `(TenantId, Name, Scope)` prevents duplicates |
| Vertical slices in .NET | Each feature: Handler + Command/Query + Validator in same folder | Cohesion over layers; avoids the "service sprawl" of horizontal architecture |
| Meal Dynamics on ink sidebar | Amber active border, amber section labels | Core design rule: indigo only on light; amber only on dark |
