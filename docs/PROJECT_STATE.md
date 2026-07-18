# OpsFlow — Project State & Decision Log

> Last updated: 2026-07-18 · Moved here from the repo root (was `/PROJECT_STATE.md`) on 2026-07-14.
>
> **This is a running log, not the spec.** It records what shipped, when, and why. For the
> authoritative current picture, use:
> - [ARCHITECTURE.md](ARCHITECTURE.md) — system architecture, stack, auth flow, deployment (canonical, correct in place)
> - [product/OpsFlow_PRD_V2.md](product/OpsFlow_PRD_V2.md) — as-built feature/role spec
> - [product/OpsFlow_Release_Notes_2026-07.md](product/OpsFlow_Release_Notes_2026-07.md) — latest shipped changes
> - [adr/](adr/) — architecture decision records
> - `.claude/skills/opsflow-design-system/SKILL.md` — canonical design tokens/components
>
> If something here contradicts one of those, the other doc wins — correct this file to match rather than the reverse.

---

## 1. What OpsFlow Is

OpsFlow is a multi-tenant **operational-compliance platform for multi-store food & beverage operators** (current tenant: Bajco, a Domino's franchisee — `bajco-dev`). It replaces paper checklists and spreadsheets with recurring digital tasks, structured field completion, corrective actions, and real-time dashboards. Full capability list: [ARCHITECTURE.md §1](ARCHITECTURE.md).

---

## 2. Current Snapshot (as of 2026-07-18)

- **Roles:** 6 roles — `super_admin`, `admin` (region-**set**-scoped), `supervisor` (single region), `store_manager` (one or more stores), `store_employee` (one store), `store_kiosk` (shared device, claim-by-name). Cut over on the live `bajco-dev` DB 2026-06-28; settled, not in progress. See [ADR-0001](adr/0001-six-role-multi-region-authorization.md). **Region scope is a set, not a scalar** — a bug class has already come from code assuming otherwise; see [ADR-0002](adr/0002-scope-authorizer-pure-module.md) for the fix.
- **Deployment:** live in production. Backend on Azure Container Apps, dashboard + field-pwa on Vercel, Postgres + Auth on Supabase. Deploy details: [ARCHITECTURE.md §6](ARCHITECTURE.md). `render.yaml` and the Render deploy job are gone as of 2026-07-14 — don't look there.
- **Latest release (PRs #90/#91/#92, ✅ live in production as of 2026-07-14):** standalone/notes-only tasks, scored checklists with auto-generated corrective tasks (replaces the never-built "Manager Walk"), a single unified "+ Create" entry point, working photo upload, admin-triggered password reset, missed-deposit alerting, real dashboards for every role, and kiosk sessions that no longer drop after ~15 minutes. Backed by one additive migration (`SafeReleaseSchema`), applied to `bajco-dev`; smoke-tested directly against prod. Full detail: [release notes](product/OpsFlow_Release_Notes_2026-07.md).
- **Deferred out of that release:** multi-store recurring broadcast (would require dropping a live `StoreId` column — too risky, single-store recurring is unaffected), self-service email password reset, and the one-time walk→scored-checklist data migration (`execution/migrate_flat_walks_to_checklists.py`) hasn't run against production yet. `execution/` seed scripts were updated 2026-07-15 to emit the scored-checklist shape (one atomic, scored `ChecklistTemplateItem` per check, matching `ImportTemplatesHandler`'s `ImportChecklist`) instead of the old flat-template shape.
- **Found during go-live, fixed:** an unclaimed Nx Cloud workspace was hard-failing CI (`nxCloudId` removed from `frontend/nx.json`, PR #91). **Found, not yet fixed:** `VERCEL_TOKEN`/`VERCEL_ORG_ID`/`VERCEL_PROJECT_ID_*` were never configured as repo secrets, so `deploy.yml`'s Vercel auto-deploy has never actually worked — every frontend deploy so far has been manual via the Vercel CLI (recipe: [ARCHITECTURE.md §6](ARCHITECTURE.md)).
- **Live since 2026-07-16 (PR #93 and its predecessor, `feat/track-b-post-prd-audit` → `main`):** a separate "Admin UI/UX overhaul" — IA restructure, org-wide tenant defaults (additive migration), the pre-existing supervisor `regionId`/`regionIds` bug fix, `RecordDeposit` scope fix, 404-vs-500 fix, WCAG pass across admin listing pages, a recovered `email`-JWT-claim auth refactor (see the decision table below), and the 2026-07-16 repo hygiene pass (§10 of `ARCHITECTURE.md`).
- **Live since 2026-07-18 (commits `e150f20`/`89037e7`, `feat/track-b-post-prd-audit`):** a full mobile-experience audit found and fixed a genuine, previously-undiagnosed production auth bug — **a refresh-token rotation race causing frequent random logouts on mobile**, ~90-100% session-drop rate on any page reload that happened to race a second reload (very common on mobile, where backgrounded tabs get reloaded constantly). Root cause: `login.component.ts`'s intentional post-login hard reload (`window.location.assign`, for locale re-bootstrap) plus any near-simultaneous second reload both presented the same pre-rotation refresh cookie; whichever the backend processed second saw an already-rotated token and hard-failed the user out, even though the session was fine. Fixed two ways: (1) frontend `AuthService.refresh()` now dedupes concurrent in-flight calls within one page instance; (2) backend `RefreshHandler` gained a 15-second reuse-grace window (`RefreshToken.UsedAt`/`ReplacedByTokenId`, migration `AddRefreshTokenReuseGrace`, applied to `bajco-dev`) that recognizes token-reuse-within-window as this benign race rather than theft and recovers the session — the same pattern used by Auth0/Supabase for exactly this scenario, not a security weakening (reuse outside the window still hard-fails as before). Verified via automated mobile-reload sweep: session-drop rate went from ~90-100% to 0% (super_admin) / <20% residual (supervisor, manager). Same pass also fixed 5 real mobile CSS/layout bugs (stat-tile strips clipped via `overflow:hidden` with no wrap in 3 places, Overview's fixed-px grid not collapsing on mobile, Templates/Form-Templates filter row squeezing the search box unusably, a leftover "Task Templates" heading after the earlier tab rename, Form Submissions showing raw user GUIDs instead of names).
- **Live since 2026-07-18 (commit `9dfca3f`, `feat/track-b-post-prd-audit`):** a mobile design sprint — fixed 7 mobile UX bugs (Checklists/Recurring runtime tables overflowing on mobile + missing create-shortcut buttons, Overview dashboard not modular on mobile, Store/User/Region detail drawers clipping text on the left, Templates' 4-tab toggle wrapping to 2 lines, template list-cards not fully clickable on mobile, More-sheet "+ Create" rendering as an unstyled white block, missing nav icons in the More sheet) and completed the cream-design re-skin of Settings/Roster/Deposit. **Bigger finding along the way:** the entire supervisor and manager shell chrome (sidebar, tabbar, page background) had been hardcoded to an unrelated slate-blue palette this whole time, unrelated to the cream design system — fixed to match admin-shell. See [ARCHITECTURE.md §5](ARCHITECTURE.md) for the reconciled drift list and the design system skill for the shell-chrome convention now documented. Also found (not yet fixed): a genuine deploy-tooling gotcha where `vercel --prod` run from a directory with a stale/missing `.vercel` link can silently deploy to the wrong Vercel project — see [ARCHITECTURE.md §6](ARCHITECTURE.md)'s new "Deploy-directory gotcha" note.
- **Live since 2026-07-17 (PRs #95–#104, same branch):** a dozen small fixes found during an onboarding walkthrough (admin Tasks "Upcoming" tab, super_admin template create/edit, camelCase form-approval JSON, auto-confirmed admin-created users, un-hid escalated field-pwa tasks, deposit-input crash, org-wide Forms Submissions view for super_admin, cosmetic list cleanup), plus a bundle of everything below in **PR #104**:
  - Login errors now surface correctly (401, not a generic 500) for bad password / unconfirmed account
  - Supervisor task-create/detail routing fixed; assign-to dropdown bug fixed
  - Checklists template picker + Form Templates listing gained real search/pagination
  - Template detail: full-row-click + in-page edit; "Task Templates" tab renamed to "Tasks"
  - Field-builder premature-submit bug fixed (see §5)
  - **New feature:** role-scoped mini-dashboards for Tasks, Checklists, and Recurring — same treatment the Forms page already had (stats strip / dedicated page), scoped server-side by role, no schema changes
  - **Fixed a real production bug:** a malformed recurring-assignment cron expression was silently blocking scheduled checklist generation tenant-wide for ~4 weeks (see §5)
  - A meaningful chunk of this had been deployed to production directly from an uncommitted working tree across multiple prior sessions; it's now been committed and merged retroactively (see `ARCHITECTURE.md` §11) — don't assume "verified live" means "in git" without checking.

---

## 3. Dev Credentials (`bajco-dev` tenant)

All passwords: `Demo1234!`. Role/scope live in the Supabase Auth user's `user_metadata` — `UserProfiles` is a display mirror only, not the source of truth.

| Role | Email | Scope |
|---|---|---|
| Super Admin | `superadmin@bajco.net` | Global |
| Admin | `admin@bajco.net` | North Region |
| Supervisor | `supervisor@bajco.net` | One region |
| Store Manager | `manager@bajco.net` | Downtown Flagship |
| Employee | `employee@bajco.net` | Downtown Flagship |

Additional store-specific managers/employees follow `manager.<store>@bajco.net` / `employee.<store>@bajco.net` (e.g. `employee.canary@bajco.net`).

### Org structure
| Region | Stores |
|---|---|
| North | Downtown Flagship (main demo store), Westside Branch |
| South | Southgate Mall, Brixton Road |
| East | Canary Wharf, Stratford City |

Stores can now be deactivated/reactivated (scope-bound to the actor's region) rather than only ever existing.

---

## 4. Real Checklist Content (from the F0890 PDF)

`docs/design/F0890 DP sample (1).pdf` is the real Domino's-style daily ops page. Its items were extracted and seeded as `TaskTemplate` fields via `execution/seed_real_checklists.py`, currently only on the Downtown Flagship store's checklists:

| Checklist | Templates | Fields |
|---|---|---|
| Morning Opening | Opening Tasks, 3-Day Dough & Cheese (MDOG), Opening Cash Management, Set Up Cut Table, Set Up Sauce Station, Set Up Customer Lobby | 59 across 6 templates |
| Evening Closing | Pre-Close Walk Through, Deployment Guide, Closing Checklist, Closing Admin & Cash | 62 across 4 templates |

Other stores' checklists still use placeholder templates — reusing the same templates via `ChecklistTemplateItems` (rather than duplicating) remains a candidate approach, not yet done.

---

## 5. Historical Bug Fixes

Landed early in the build; kept as a record since the root causes are non-obvious and could recur if similar code is written elsewhere.

**`DateTimeOffset.UtcNow.Date` rejected by Npgsql** — it produces `Kind=Unspecified`, which Postgres `timestamptz` rejects. Fixed in all dashboard handlers + `GetTodayTasksHandler` by wrapping explicitly: `new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero)`.

**Refresh cookie never sent over `http://localhost`** — `Secure=true` on the cookie is silently dropped by browsers over plain HTTP. Fixed with `Secure = !env.IsDevelopment()` in `TokenService.cs`.

**`APP_INITIALIZER` race on Angular 21** — the old `APP_INITIALIZER + useFactory + inject()` pattern let the router guard run before `auth.refresh()` completed, letting unauthenticated users through. Fixed by switching to `provideAppInitializer()` (Angular 19+ API) in `auth.initializer.ts`.

**"No store assigned" instead of redirect on expired session** — `tasks.component.ts` showed a confusing error instead of bouncing to `/login`. Fixed with an explicit `if (!storeId) { router.navigate(['/login']); return; }` guard.

More recently: a scope-authz audit (2026-07-06) found and fixed 11 `GetChecklistHandler`-class bugs where region-scoped `admin`/`supervisor` callers were checked as if they had a single `StoreId`/`RegionId` instead of a set — see §2 and [ADR-0002](adr/0002-scope-authorizer-pure-module.md).

**`GenerateTaskInstancesJob`'s per-assignment loop had no error handling (2026-07-17)** — `TenantIteratingJob` only catches exceptions at the whole-tenant level, so one `RecurringAssignment` with an invalid `CronExpression` threw and silently aborted generation for every other assignment in that tenant, on every 15-minute tick. Found while validating the new Recurring dashboard: one assignment ("Daily Cash Management") had a 5-field Unix-style cron (`"0 8 * * *"`), invalid for Quartz's 6/7-field format — this had been blocking real stores' scheduled checklists for about a month. Fixed by wrapping the per-assignment body in its own try/catch (logs and skips the bad row, tenant continues) and correcting that one cron directly in `bajco-dev` to `"0 0 8 * * ?"`.

---

## 6. Execution Scripts

All in `execution/`. No interactive prompts; read `.env` for credentials.

| Script | Purpose |
|---|---|
| `seed_demo.py` | Seeds initial org structure: 1 region, 2 stores, 4 users, checklists, templates, recurring assignments, task instances |
| `enrich_demo.py` | Adds 2 more regions, 4 more stores, 10 more users, richer task instances, extra form templates/submissions |
| `seed_real_checklists.py` | Replaces placeholder template items on Downtown Flagship's Morning Opening / Evening Closing checklists with real F0890 fields |
| `check_db.py` | Quick DB health check |
| `check_columns.py` | Inspect table schemas |

**Fixed 2026-07-15:** these (plus `rebuild_templates_from_f0890.py` and `populate_empty_templates.py`, not listed in the table above) now emit the scored-checklist shape — one atomic, scored `ChecklistTemplateItem` per check (Boolean fields become PassFail-scored items; everything else stays an unscored data-capture item), mirroring the same transform rule used by `execution/migrate_flat_walks_to_checklists.py` and `ImportTemplatesHandler.ImportChecklist`. Freshly-seeded data now exercises scoring/corrective-task generation.

---

## 7. Dev Server Setup

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

Both apps proxy `/api/*` → `http://localhost:5000` (stripping `/api`) and `/hubs/*` (WebSocket) in local dev. Note: this is local dev only — the deployed apps talk to the live Azure API via `public/env.js`, not this proxy (see [ARCHITECTURE.md §5](ARCHITECTURE.md)).

---

## 8. Security Notes

**Never commit:** `.env` (gitignored) — contains `SUPABASE_SERVICE_ROLE_KEY`, `MASTER_DB_CONNECTION_STRING`, `TENANT_DB_CONNECTION_STRING`, `JWT_SECRET`; `credentials.json` / `token.json` (Google OAuth).

In production these are injected via the hosting platform's secret store, not read from a committed file — see [ARCHITECTURE.md §6](ARCHITECTURE.md). If any of the above were ever exposed in a chat/log, rotate the Supabase service role key, reset the DB password, and regenerate the JWT secret before treating the session as closed.

---

## 9. Key Architectural Decisions

Decisions specific to this build that aren't already captured as ADRs:

| Decision | What | Why |
|---|---|---|
| JWT + httpOnly cookie | Access token in memory (Angular Signal), refresh token in httpOnly cookie | Access token never touches localStorage; refresh survives page reload |
| Token rotation on refresh | Refresh token marked `IsUsed=true` on every use | Prevents replay attacks |
| StoreId/RegionIds in RefreshToken row | DB stores them alongside the refresh token | Allows JWT reconstruction without re-calling Supabase on every refresh |
| `provideAppInitializer` | Angular 21 API (not `APP_INITIALIZER`) | Old pattern unreliable in Angular 19+; caused the guard/refresh race in §5 |
| JSONB for template fields | `TaskTemplates.Fields` is a JSONB array | Avoids EAV table explosion; fields are schema-flexible per template type |
| `ChecklistTemplateItems` junction | Checklist → ordered list of TaskTemplates | Same template reusable across multiple checklists |
| Store-prefixed checklist names | e.g. "Westside: Morning Opening" | DB `UNIQUE (TenantId, Name, Scope)` constraint prevents duplicates |
| Vertical slices in .NET | Each feature: Handler + Command/Query + Validator in one folder | Cohesion over layers; avoids "service sprawl" of a horizontal architecture |
| Email JWT claim resolved via `IAuthProvider.GetEmailAsync`, not stored on `RefreshToken` | Login mints it from the input email directly; refresh calls the provider (Supabase `GetUserById` / `UserManager.FindByIdAsync`) | No schema change needed for a display-only field; costs one extra provider call per 15-minute token refresh |

Role/authorization-model decisions now live in [ADR-0001](adr/0001-six-role-multi-region-authorization.md) and [ADR-0002](adr/0002-scope-authorizer-pure-module.md) — don't duplicate them here.
