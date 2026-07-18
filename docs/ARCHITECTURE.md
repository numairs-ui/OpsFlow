# OpsFlow — Product & Architecture Overview

> **Status:** Current as of 2026-07-18. Supersedes the architecture sections of `docs/PROJECT_STATE.md`. This is the canonical reference — please correct it in place as the system evolves rather than starting a new doc.
>
> **2026-07-14 release:** the post-PRD-audit release (task nucleus + 6 targeted fixes, minus multi-store recurring which was deferred) is **live in production**. See `docs/product/OpsFlow_PRD_V2.md` (current as-built PRD, supersedes V1) and `docs/product/OpsFlow_Release_Notes_2026-07.md`. §7 below reflects the post-release state.
>
> **2026-07-17: role-scoped mini-dashboards for Tasks/Checklists/Recurring, plus a dozen small fixes (PRs #93–#104), are also live.** See §7 for the full list. This includes a fix for a real production data-integrity bug — one recurring assignment's malformed cron expression was silently blocking scheduled-checklist generation tenant-wide for about a month (§7, §8).
>
> **What's actually deployed right now, verified (2026-07-17):** the Azure Container App's active revision is `opsflow-app--0000014` (deployed 2026-07-17T15:50 UTC, image digest `sha256:471ac36f...`), which corresponds to `main` at commit `d0fe61c` (PR #104 merge). The dashboard's Vercel deploy was last pushed the same day and matches the same commit. If you're ever unsure whether prod matches `main` or some other branch, verify like this rather than guessing from branch-divergence size alone — a large diff between `main` and an open PR's branch usually just means real, not-yet-merged work, not a deploy-source mismatch.
>
> **2026-07-18: mobile design sprint (commit `9dfca3f`, branch `feat/track-b-post-prd-audit`, dashboard-only, no backend changes) is live.** Fixed 7 mobile UX bugs found during a live pass (Checklists/Recurring runtime tables overflowing + missing create buttons, Overview dashboard not modular, Store/User/Region detail drawers clipping text, Templates tabs wrapping to 2 lines, template cards not fully clickable, More-sheet "+ Create" rendering as an unstyled white block, missing nav icons) and completed the cream re-skin of Settings/Roster/Deposit — see §5's "Known drift" note below for what's now clean. **Also found and fixed: the entire supervisor and manager shell chrome (sidebar, tabbar, page background) was hardcoded to an unrelated slate-blue Tailwind palette (`#f8f9fa`/`#1e293b`/`#3b82f6`), byte-identical in both files — almost certainly copy-pasted from the same non-cream starter template and never touched since. Both now use the cream/ink/amber tokens matching admin-shell.** See the design system skill for the shell-chrome convention now documented so this doesn't recur a third time (e.g. if a new role shell is added).
>
> **Known gap this update closed:** a meaningful amount of work described as "shipped"/"live" in prior session notes (several bug fixes plus the mini-dashboards feature) had been deployed directly from a local working tree and was never actually committed to git — production and `main` had quietly diverged. It's now been committed and merged as PR #104. If a future session finds deployed behavior that doesn't match any commit, don't assume the docs are wrong before checking whether this has recurred — deploy from a clean, committed checkout going forward.

---

## 1. What OpsFlow Is

OpsFlow is a **multi-tenant operational-compliance platform for multi-store food & beverage operators**. Current client/tenant: **Bajco**, a Domino's franchisee (tenant id `bajco-dev`), positioned as the "OpsFlow Module" of the broader MealDynamics product line.

It replaces paper checklists, WhatsApp threads, and spreadsheets with a structured system so regional supervisors can catch systemic failures — temperature breaches, till variances, missed deposits — before they become customer-facing incidents. Core capabilities:

- Assign checklist tasks to stores on a recurring schedule
- Let field employees complete tasks with structured form fields (boolean, numeric, text, sub-checklists, photo)
- Give managers/supervisors a real-time completion dashboard
- Trigger corrective-action prompts when submitted values fall out of range
- Score checklist items (Pass/Fail or 1–5) into a weighted composite score, auto-generating claimable corrective tasks on failure — this is the "Manager Walk" capability, delivered through Checklists/Tasks rather than a separate Walk domain (see 2026-07-14 note below)
- Track free-form form submissions (incident reports, audits) through an approval workflow
- Support cash/inventory compliance: deposit logs, till counts, dough/cheese prep planning (MDOG)

---

## 2. Domain Model

### 2.1 Roles & scope

Six roles ([ADR-0001](adr/0001-six-role-multi-region-authorization.md)), replacing an earlier 4-role draft:

| Role | Scope | Notes |
|---|---|---|
| `super_admin` | Network-wide | All tenant data |
| `admin` | A **set** of regions | Delegated regional administration short of full network power |
| `supervisor` | A single region | |
| `store_manager` | One or more stores | Can be assigned multiple stores via a join table |
| `store_employee` | A single store | |
| `store_kiosk` | A single store, shared device | Always-logged-in station; walk-up staff claim tasks by typing their name (`CompletedByVolunteerName`) — no personal login |

**Region scope is a set, not a scalar** — an `admin` can span several regions, so region membership is carried as repeated `regionId` JWT claims and persisted as `RegionIdsCsv`. This has bitten the codebase before: a naive check assuming one `StoreId`/`RegionId` per caller (`WhereScopedVisible` in `ScopeQueryExtensions.cs`) once hid templates/checklists from every region-scoped admin. Any new authorization code must be written against the set, not a single value. The same System / Regional / Store scope tiering applies to templates, checklists, and form templates.

Cutover from the old 4-role model ran against live data on 2026-06-28 and has held since with no rollback.

### 2.2 Core entities

```
Tenant → Region → Store → UserProfile / StoreSettings

TaskTemplate            reusable, scope-bound blueprint (fields, validation ranges, corrective-action text) — no schedule
Checklist                groups TaskTemplates via ordered ChecklistTemplateItems junction
ChecklistTemplateItem    junction row; ALSO carries scoring config (ScoringType, Weight, PhotoRequired,
                         FailCorrectiveActionText, FailScoreThreshold) since 2026-07-14 — see below
RecurringAssignment      binds a Checklist to one store + cron schedule (single-store only — see 2026-07-14 note)
TaskInstance             a dated instance of a checklist/template, or standalone (nullable ChecklistId since
                         2026-07-14: may reference a single TaskTemplate via AdHocTaskTemplateId, or be
                         notes-only). SourceTaskInstanceId self-FK links an auto-spawned corrective task
                         back to the session that failed it.
TaskCompletion           submitted field values for a TaskInstance; may trigger embedded corrective-action
                         text; since 2026-07-14 also carries CompositeScorePercent + ItemScoresJson for
                         scored checklist sessions
MissedDepositFlag        new 2026-07-14 — persisted daily flag when a store misses its deposit deadline;
                         feeds the existing region/system dashboards (dashboard-only, no push)
FormTemplate             fields + ordered ApprovalSteps[]
FormSubmission           state machine: Draft → Submitted → PendingApproval → Approved/Rejected/Returned,
                         per PropagationType (Sequential / Parallel / NotificationOnly)
DepositLog               immutable cash-deposit record
InventorySnapshot        feeds MDOG (dough/cheese prep planning)
```

**2026-07-14: "Manager Walk" was never built as a separate domain and won't be** — no `WalkTemplate`/`WalkSession` entities exist. The scored-audit capability (composite score, auto-generated corrective tasks) is delivered entirely through `Checklist` + `ChecklistTemplateItem` scoring + `TaskCompletion`, i.e. a checklist-backed `TaskInstance` session *is* the walk. Pure scoring logic lives in `OpsFlow.Domain/Checklists/ChecklistScoring.cs` (modeled on `Forms/ApprovalWorkflow.cs` — no DB access, unit-tested).

**2026-07-14: multi-store recurring broadcast was built, then deferred and reverted** — it would have required dropping `RecurringAssignment.StoreId` on a live database. `RecurringAssignment` is single-store, unchanged from before this release. A future release will add multi-store broadcast without a column drop.

`TaskTemplate.Fields` is JSONB (not EAV) — an array of `{id, label, type, required, rangeMin, rangeMax, correctiveActionText, subItems}`; field types are `Boolean`, `Numeric`, `Text`, `Checklist` (nested sub-items), `Photo` (fully wired end-to-end since 2026-07-14: signed-URL upload, direct-to-storage PUT, no longer a placeholder).

### 2.3 Feature domains

Multi-tenancy/provisioning · Auth & Authorization · Store/Region/User management · Task Templates · Checklists (with scoring) · Recurring Assignments (single-store) · Task Board (Field PWA, incl. standalone tasks) · Store Kiosk · Task Completion & Verification · Corrective Actions (incl. auto-generated follow-up tasks) · MDOG & Inventory · Safe/Till/Deposit Log (incl. missed-deposit dashboard flag) · Notifications (SignalR only — no FCM/push is implemented) · Role-scoped Dashboards (real for every role since 2026-07-14; Tasks/Checklists/Recurring gained their own mini-dashboards on 2026-07-17, matching what Forms already had) · Admin Panel (incl. unified Create entry point) · Form Templates · Form Submission/Approval engine.

---

## 3. System Architecture

```
                    ┌─────────────────────┐        ┌─────────────────────┐
                    │   Dashboard (SPA)   │        │  Field PWA (SPA)    │
                    │  Angular 21 / Nx    │        │  Angular 21 / Nx    │
                    │  admin/manager/     │        │  worker/kiosk       │
                    │  supervisor console │        │  task board         │
                    └──────────┬──────────┘        └──────────┬──────────┘
                               │  HTTPS + JWT (in-memory) / httpOnly refresh cookie
                               │  SignalR (wss) for realtime task board
                               ▼
                    ┌─────────────────────────────────────────┐
                    │        OpsFlow.Api — .NET 9             │
                    │  Minimal APIs · MediatR (CQRS) ·         │
                    │  FluentValidation · Vertical Slices      │
                    │  Quartz.NET background jobs · SignalR    │
                    └──────────┬────────────────────┬──────────┘
                               │                    │
                     ┌─────────▼────────┐   ┌───────▼────────┐
                     │  MasterDbContext  │   │ TenantDbContext │
                     │  (tenant registry)│   │ (per-tenant data)│
                     └─────────┬─────────┘   └───────┬─────────┘
                               └──────────┬───────────┘
                                          ▼
                          PostgreSQL on Supabase (bajco-dev)
                                          │
                         Supabase Auth (JWT minting via backend)
```

Backend and both frontends deploy independently; the API is stateless per-request (JWT auth, refresh handled via DB-backed rotation), so it scales horizontally behind Azure Container Apps.

---

## 4. Backend (`backend/`)

**Stack:** .NET 9, MediatR 12.4, FluentValidation, EF Core 9 (Npgsql provider in practice; SQL Server provider also compiled in), Quartz.AspNetCore 3.14 for jobs, SignalR for realtime, Scalar for OpenAPI docs, Supabase C# client + JWT bearer auth, Azure Blob Storage / Azure SignalR adapters for storage and scale-out realtime. The `FirebaseAdmin` NuGet package is referenced (for planned FCM push) but has **no adapter/service actually using it anywhere in the codebase** — push notifications were never built; see §7.

**Solution layout:**

| Project | Responsibility |
|---|---|
| `OpsFlow.Api` | Web host — minimal-API feature folders, MediatR pipeline behaviours, Quartz jobs, SignalR hubs, JWT/scope security wiring |
| `OpsFlow.Domain` | Pure domain — entities, forms, interfaces, and the `Authorization` scope module. No EF/ASP.NET references |
| `OpsFlow.Infrastructure` | EF `MasterDbContext` (tenant registry) + `TenantDbContext` (per-tenant), provider config, Azure/Supabase adapters, migrations |
| `OpsFlow.Migrations` | Console app that runs EF migrations against Master/Tenant DBs |
| `OpsFlow.Tests.Unit` / `OpsFlow.Tests.Integration` | Unit (Authorization, Forms, Tasks, Health) and integration (Auth, Checklists, FormSubmissions, Scenarios) test suites |

**Vertical Slice Architecture** is real, not aspirational: each operation under `OpsFlow.Api/Features/<Area>/<Operation>/` has its own `Command`/`Query`, `Handler`, and (where needed) `Validator`, wired to a minimal-API endpoint group that sends through MediatR. 17 feature areas exist today: Auth, Checklists, Dashboard, DepositLog, FormSubmissions, FormTemplates, Health, Inventory, Me, RecurringAssignments, Regions, StoreSettings, Stores, Tasks, Templates, TenantSettings, Users.

**Authorization** ([ADR-0002](adr/0002-scope-authorizer-pure-module.md)) lives entirely in `OpsFlow.Domain/Authorization/`: `ScopeSpec` is a pure, DI-free class built from a `Caller {Role, StoreId, RegionIds}`, exposing `IsGlobal`/`IsRegionScoped`/`IsStoreScoped` and assertion methods (`AssertCanViewRegion`, `AssertCanManageStore`, `AssertCanWriteScope`, etc.). `ScopeQueryExtensions` supplies EF-translatable `IQueryable` filters via key-selectors so handlers stay thin. Handlers call this module directly rather than re-deriving rules — deliberately no `IScopeAuthorizer` interface, since there's exactly one implementation.

**Background jobs** (Quartz.NET, `OpsFlow.Api/Jobs/`): `GenerateTaskInstancesJob` (recurring assignments → dated task instances), `ActivateDeferredTasksJob`, `OverduePromotionJob`, `DepositEscalationJob` (new 2026-07-14 — daily, flags stores past their local deposit deadline into `MissedDepositFlag`), and `TenantIteratingJob` (base helper fanning a job out across all tenants). **`GenerateTaskInstancesJob`'s per-assignment loop is now wrapped in its own try/catch (fixed 2026-07-17)** — `TenantIteratingJob` only catches at the whole-tenant level, so a single `RecurringAssignment` with a malformed `CronExpression` used to throw and silently abort generation for every *other* assignment in that tenant on every tick. Found via the new Recurring dashboard (§7): one assignment had a 5-field Unix-style cron (invalid for Quartz's 6/7-field format), which had been blocking real stores' scheduled checklists for about a month. The bad cron was corrected directly in `bajco-dev`; the loop guard prevents recurrence for any future bad row.

**Multi-tenancy:** every tenant table carries `TenantId`; a `MasterDbContext` holds the tenant registry, a `TenantDbContext` (resolved per-request) holds the actual data.

**Auth flow:**
```
POST /auth/login → Supabase.SignIn → read user_metadata (role, storeId, regionIds)
  → mint JWT (15 min; claims: sub, tenantId, role, storeId, one regionId claim per region)
  → persist RefreshToken (30d, carries StoreId/RegionIds for reconstruction)
  → set httpOnly cookie "refresh_token" = "{tenantId}:{rawToken}"

POST /auth/refresh → read cookie → validate + rotate RefreshToken (mark old IsUsed=true)
  → mint new JWT from stored StoreId/RegionIds → set new cookie
```
`Secure` cookie flag is conditional on environment (`!IsDevelopment()`), since browsers drop `Secure` cookies over plain `http://localhost`.

**Refresh-token reuse grace period (added 2026-07-18):** `RefreshToken` gained `UsedAt`/`ReplacedByTokenId` (migration `AddRefreshTokenReuseGrace`). If a rotated-away token is presented again within **15 seconds** of its own rotation, `RefreshHandler` treats it as a benign race — two near-simultaneous page loads (e.g. the post-login hard reload's own app-initializer call racing a second reload) both presenting the same pre-rotation cookie — rather than a stolen/replayed token, and walks `ReplacedByTokenId` to mint from the current valid token instead of hard-failing. Reuse outside the 15s window still fails exactly as before; this narrows a specific race, it doesn't weaken replay protection. This closed a real bug: mobile page reloads (common — backgrounded tabs reload constantly) had a ~90-100% chance of logging the user out even mid-session. See `docs/PROJECT_STATE.md`'s 2026-07-18 entry for the full diagnosis.

---

## 5. Frontend (`frontend/`)

**Stack:** Nx 22 monorepo (Nx Cloud removed 2026-07-14 — its workspace was never claimed within Nx Cloud's 3-day window and was hard-failing CI builds; it only provided a remote cache layered on top of Nx's always-present local cache, so removal cost nothing), **Angular 21** (standalone components only, no NgModules), **Angular Signals** for all reactive state (no NgRx or other state library), strict TypeScript. Vitest for unit tests (per-lib `vitest.config`, orchestrated by a root `vitest.workspace.ts`), Playwright for e2e (one `-e2e` project per app).

**Apps:**
- `apps/dashboard` (port 4200) — the admin/back-office console. Routes role-gated per area (`admin`, `manager`, `supervisor` shells).
- `apps/field-pwa` (port 4201) — worker-facing PWA: task board, task detail, kiosk mode for `store_kiosk` devices, quick-template, form submissions.

**Libs** (feature-organized, not layer-organized):
- `data-access/{auth,org,tasks,templates,core}` — one lib per bounded context (services, models, guards, interceptors).
- `ui/{core,cron-picker,field-builder,template-builder}` — presentational and authoring-flow components (checklist/template builder, cron schedule picker, dynamic form field builder).
- `util/{guards,http,models}` — cross-cutting helpers (`auth.guard.ts`, `role.guard.ts`, HTTP helpers, shared TS models).

Both apps proxy `/api/*` → the backend (stripping the prefix) and `/hubs/*` for SignalR in local dev.

**Design system:** bespoke (not Ant Design/ng-zorro — that path was tried and fully reverted). Documented in `.claude/skills/opsflow-design-system/SKILL.md`; implemented as CSS custom properties + utility classes duplicated across each app's `styles.scss` (`apps/dashboard`, `apps/field-pwa`). Aesthetic: warm "operations desk" — cream/paper background, near-black ink, one indigo accent for action, amber/rust/green reserved for fixed status meanings (amber = paused, rust = error/overdue, green = success). Type: **Inter** for both sans and serif roles, **JetBrains Mono** as the structural/label voice (eyebrows, table headers, pills). Pill-shaped buttons, 14px base radius.

> ⚠️ **Known drift:** the two apps' stylesheets are hand-duplicated with a history of drifting out of sync. The documented fix — extract a shared SCSS partial into `frontend/libs/ui` — has not been done yet.
>
> **Update (2026-07-18):** the admin-screen drift list above is stale — reconciled during a mobile design/re-skin sprint. Stores, Users, Checklists, Templates, Regions, and Form Submissions all render on the current cream design system now (verified by reading each component's SCSS directly, not just visually). What was actually still drifted, found and fixed this pass: Settings, Roster, and Deposit (component-local hardcoded hex colors, e.g. `#e5e7eb`/`#6b7280`/`#fef2f2`, swapped for the token set below) — **and, more significantly, the entire supervisor and manager shell chrome** (sidebar/tabbar/page background), which had never been on the design system at all — see the note in the file header above. If a future pass finds another screen that looks "off," check for hardcoded hex first; it's been the root cause both times.

**Runtime config:** each app reads `public/env.js` → `window.__OPSFLOW_ENV__.apiOrigin`. A fresh `nx build` resets this to the committed default (`''`) — it must be manually repatched to the live API URL before every deploy, or the deployed app silently can't reach the backend.

---

## 6. Deployment & Infrastructure

| Component | Where it actually runs | Notes |
|---|---|---|
| Backend API | **Azure Container Apps** (`opsflow-app`, resource group `opsflow_dev`), image in ACR `opsflowacr.azurecr.io/opsflow-backend` | Deployed manually by digest (`az acr build` → `az containerapp update --image ...@sha256:...`); Azure won't cut a new revision on an unchanged `:latest` tag. Live revision as of 2026-07-17: `opsflow-app--0000014`. |
| Dashboard | Vercel project `opsflow-dashboard` (`opsflow-dashboard-gamma.vercel.app`) | **Manual deploy only** — see note below |
| Field PWA | Vercel project `opsflow-field-pwa` (`opsflow-field-pwa.vercel.app`) | **Manual deploy only** — see note below |
| Database | PostgreSQL on **Supabase** (`bajco-dev` tenant) | Also the auth provider (Supabase Auth) |

**Update (2026-07-14):** the stale `render.yaml` and the `deploy-api → Render` job in `.github/workflows/deploy.yml` have been removed — Render was the original hosting target, superseded by Azure Container Apps, and the leftover config was confusing anyone using repo files to answer "where does this deploy." Backend deploy to Azure is a manual step (see the table above) with no CI job.

**⚠️ Correction (2026-07-14) — the two Vercel deploy jobs in `deploy.yml` have never actually worked.** `VERCEL_TOKEN`/`VERCEL_ORG_ID`/`VERCEL_PROJECT_ID_FIELD_PWA`/`VERCEL_PROJECT_ID_DASHBOARD` were never configured as repo secrets (`gh secret list` returns empty) — confirmed by inspecting multiple historical CI runs, all failing at the "Deploy to Vercel" step with `Input required and not supplied: vercel-token`. **Every real frontend deploy, past and present, has been manual** via the Vercel CLI:
```bash
npx nx build <app> --configuration=production
# patch dist/apps/<app>/browser/env.js: apiOrigin: '' -> the live Azure FQDN
cd dist/apps/<app>/browser
npx vercel link --project <opsflow-dashboard|opsflow-field-pwa> --yes
npx vercel --prod --yes
```
Build from a clean checkout of `origin/main` (e.g. a `git worktree`), not a working directory that may have uncommitted changes — both apps consume shared `frontend/libs/*`, so uncommitted lib changes elsewhere in the repo can leak into a production build. Setting up the missing secrets so CI can actually deploy is an open follow-up (`docs/product/OpsFlow_PRD_V2.md` §9) — not urgent, since the manual path works, but `merge → live` does not currently hold.

**⚠️ Deploy-directory gotcha (found 2026-07-18):** always `cd` into the actual build output directory (`dist/apps/<app>/browser`) *before* running `vercel link`/`vercel --prod` — never from the `frontend/` workspace root. Two failure modes observed the same session:
1. Linking/deploying from `frontend/` (not the build output) causes Vercel to run its own remote build against the whole workspace instead of serving the prebuilt static files, producing an unrelated/broken deployment (404s from Vercel's platform-level error page).
2. `nx build` wipes and regenerates `dist/apps/<app>/browser` on every run, **including any `.vercel/` link inside it.** Running `vercel --prod` in that directory with no valid link present causes the Vercel CLI to auto-create/attach a project named after the directory itself (`browser`) rather than failing loudly — silently deploying the build to a *different, unrelated Vercel project* that happens to already exist under that name. This actually happened once and was caught only by comparing bundle hashes after deploy; recovered via `vercel rollback <last-good-deployment-url>`.
**Always re-run `vercel link --project <opsflow-dashboard|opsflow-field-pwa> --yes` immediately after every fresh `nx build`, from inside the build output directory, before `vercel --prod`** — don't assume a link from earlier in the session still exists. After deploying, verify by comparing the live `main-*.js` bundle hash (`curl`) against the local one, not just an HTTP 200.

**CI (`.github/workflows/pr.yml`, on PR to `main`):**
- `dotnet` job — restores/builds `backend/OpsFlow.sln`, runs `OpsFlow.Tests.Unit` + `OpsFlow.Tests.Integration`, uploads `.trx` results.
- `angular` job — Node 20, `npm ci`, `nx affected --target=lint`, `nx affected --target=test --ci`, production builds of both apps.

**Fixed (2026-07-16):** the Angular job's long-standing shallow-checkout flake (`nx affected --base=origin/main` failing with "ambiguous argument 'origin/main'" on every single PR, content-independent — confirmed on a docs-only PR) is resolved via `fetch-depth: 0` on the checkout step. The .NET suite still has an intermittent Quartz/LoggerFactory host-startup race under `WebApplicationFactory` — rerun locally (`dotnet test` from `backend/`) before treating a red `.NET Tests` check as a real regression.

**⚠️ That fix unmasked a real, pre-existing gap — don't assume `Angular Tests` is green now.** `Test (affected)` never used to actually execute (the checkout flake always failed first); now that it runs, it fails on `dashboard`/`field-pwa` (`'ci' is not found in schema` — their test executor doesn't support the `--ci` flag `pr.yml` passes) and on `data-access-auth`/`data-access-org`/`data-access-tasks`/`util-guards` (empty placeholder `.spec.ts` files with zero tests, `No test suite found`). Confirmed pre-existing — reproduced locally against a commit that predates this fix — not a regression from anything in this release, but genuinely unfixed. Production builds (`nx build`) are unaffected; this is specifically the `test` target.

**Secrets:** backend secrets (`SUPABASE_SERVICE_ROLE_KEY`, `MASTER_DB_CONNECTION_STRING`, `JWT_SECRET`, etc.) are not committed — injected via the hosting platform's secret store. `appsettings.json` only holds non-secret defaults. Frontend has no `NG_APP_*`/environment files checked in beyond the `env.js` runtime-config pattern above.

### Backups — current posture (reviewed 2026-07-18, DB gap closed same day)

What's actually backed up today, per component:

| Component | Backup mechanism | Gap |
|---|---|---|
| **Code** (backend + both frontends) | Full history on GitHub (`origin`, `numairs-ui/OpsFlow`) — this *is* the backup; a local machine loss loses nothing already pushed | The recurring real risk here isn't "no backup," it's **uncommitted work never reaching GitHub** — this has happened twice already (§10, §11 below) after deploying straight from a working tree. The fix is discipline (commit+push after every deploy), not more infra. |
| **Backend container image** | Azure Container Registry (`opsflowacr`) keeps every tagged/digested image ever pushed | Rebuildable from git + Dockerfile at any time regardless; the registry history is a convenience, not the safety net |
| **Frontend deploys** | Vercel keeps full deployment history per project indefinitely, and `vercel rollback <url>` reverts production to any prior deployment in seconds (confirmed working 2026-07-18, used to recover from a deploy mistake) | None — this is a solid, already-working safety net |
| **Database** (Postgres on Supabase, `bajco-dev`) | **Confirmed via the Supabase Management API (2026-07-18): Free tier, `pitr_enabled: false`, zero backups on record.** No native backup existed. **Closed the same day**: `.github/workflows/backup-db.yml` (PRs #108/#109) runs a daily `pg_dump` (03:17 UTC, plus on-demand via `workflow_dispatch`) to a new, independent Azure Storage account (`opsflowdbbackups`, resource group `opsflow_dev`, container `db-backups`), custom format (`-Fc`), 30-day retention pruning. First run verified end-to-end 2026-07-18 (a real ~560KB dump landed in the container). | None currently — this is now a real, working, independent safety net. **Still worth doing separately**: consider upgrading the Supabase plan for native PITR (this pg_dump approach gives daily-granularity recovery, not point-in-time) — that's a billing decision, not an engineering blocker, and this backup stays valuable as a second independent layer even after upgrading. |

**Restoring from a backup:** download the desired `.dump` blob from the `db-backups` container, then `pg_restore --no-owner --no-privileges -d <target-connection-string> <file>.dump` (custom format supports selective/parallel restore — see `pg_restore --help`).

**Credentials note:** the backup workflow's DB/storage credentials live only as GitHub Actions repo secrets (`BACKUP_DB_HOST`/`_PORT`/`_NAME`/`_USER`/`_PASSWORD`, `AZURE_BACKUP_STORAGE_CONNECTION_STRING`) — same non-committed-secret discipline as the app's own secrets above.

---

## 7. Current Build Status

**Backend:** most feature domains implemented — auth, org CRUD, templates, checklists, recurring assignments, full task lifecycle, deposit log, inventory, dashboards. Form submissions exist end-to-end at the data/API layer; the approval UI is incomplete.

**Frontend:** design system rollout is partial (see §5 drift note). Multi-region/6-role scope migration is settled and live, not in progress.

**Shipped 2026-07-14 (post-PRD-audit release, live in prod — see `docs/product/OpsFlow_PRD_V2.md` for full detail):**
- Standalone tasks (no checklist required — single-template or notes-only)
- Checklist item scoring (Pass/Fail or 1–5, weighted) + a full admin checklist editor (was read-only)
- Scored checklist sessions → composite score + auto-generated claimable corrective tasks on failure
- Unified "+ Create" entry point in the dashboard
- Template-import pipeline rework (real scored checklists on import; fixed a fake-success counting bug)
- Kiosk session no longer drops on token expiry
- Admin-triggered password reset (one-time temp password, no email infra)
- Missed-deposit dashboard flag (deadline-aware daily job; dashboard-only, no push)
- **Photo upload is fully wired** (was a placeholder) — signed-URL, direct-to-storage upload
- Real dashboards for admin (region-scoped, not just super_admin), store employee, and kiosk roles
- **Deferred out of this release:** multi-store recurring broadcast (would have required a live column drop) — `RecurringAssignment` stays single-store; see the 2026-07-14 note in §2.2

**Shipped 2026-07-18 (commits `e150f20`/`89037e7`/`9dfca3f`, live in prod):**
- **Fixed a real, previously-undiagnosed production auth bug:** refresh-token rotation race causing frequent random logouts on mobile — see the §4 Auth flow note above for the fix, `docs/PROJECT_STATE.md` for the full root-cause diagnosis.
- 5 real mobile CSS/layout bugs (stat-tile clipping, Overview grid not collapsing, Templates/Form-Templates filter squeeze, stale heading, raw-GUID display) plus a mobile design sprint: Checklists/Recurring runtime overflow + create buttons, Overview modularized for mobile, detail-drawer left-clip fix, Templates tab/card-click/button-text fixes, More-sheet fixes, and a cream-design re-skin of Settings/Roster/Deposit **and the supervisor/manager shell chrome** (previously on an unrelated hardcoded palette). See §5's "Known drift" update and the design system skill.

**Shipped 2026-07-16/17 (PRs #93–#104, live in prod):**
- **Role-scoped mini-dashboards for Tasks, Checklists, and Recurring** (PR #104) — same treatment the Forms page already had: a stats strip on Tasks, and new dedicated Checklist Performance / Recurring Health pages for Checklists/Recurring, all scoped server-side by role (org/region/store) with no schema changes. See §2.3, §4.
- **Fixed a real production bug** (PR #104): a malformed recurring-assignment cron expression was silently blocking scheduled task generation tenant-wide for ~4 weeks; see the `GenerateTaskInstancesJob` note in §4.
- Auth error handling fixed — rejected logins (bad password, unconfirmed account) no longer surface as a generic 500 (PR #104)
- Supervisor task-create/detail routing fixed; assign-to dropdown no longer shows "unassigned" for tasks that do have an assignee (PR #104)
- Checklists' template picker and Form Templates listing gained real search + pagination past their first page/100 rows (PR #104)
- Template detail: full-row-click + in-page edit; "Task Templates" tab renamed to "Tasks" (PR #104)
- Field-builder: fixed a bug where adding a field mid-edit implicitly submitted the form and kicked the user back to read-only view before they could fill it in (PR #104)
- A round of smaller fixes from an onboarding walkthrough (PRs #96–#103): admin Tasks list gained an "Upcoming" tab; super_admin can create/edit System and Regional scope templates; `FormTemplate` approval steps serialize as camelCase JSON; admin-created Supabase users are auto-confirmed; field-pwa no longer hides escalated (`CorrectiveActionRaised`) tasks; deposit-amount input no longer crashes on every keystroke; super_admin sees the org-wide Form Submissions view; broken row-divider lines and decorative sidebar dots cleaned up.

**Open gaps:**
- Overdue-task push notifications (FCM was never built — SignalR only, confirmed pre-existing, not part of the above release)
- Per-store manager task board view
- Seeding real (non-placeholder) checklist content into stores beyond the flagship location — a migration script (`execution/migrate_flat_walks_to_checklists.py`) exists and is self-tested but has not been run against any database
- Form submission approval UI
- Design-system rollout — §5's drift list is now clean as of 2026-07-18 (see that section's update note); no other known drifted screens at this time
- Multi-store recurring broadcast (deferred, see above) — needs a non-destructive schema design
- Vercel CI secrets (`VERCEL_TOKEN` etc.) were never configured — CI cannot currently deploy either frontend; all deploys are manual (see §6)
- ~~Supabase database backup/PITR status for `bajco-dev` has not been confirmed~~ — confirmed (Free tier, no native backups) and closed via a daily `pg_dump` GitHub Actions workflow, see §6 Backups. Native PITR itself is still a possible future upgrade (billing decision, not blocking).
- "Morning" recurring-assignment instance generation is still stuck (evening assignments generate fine) despite the cron-expression + per-assignment try/catch fixes from 2026-07-17 — flagged 2026-07-18, not yet root-caused; hypothesis is that `SaveChangesAsync` running once per tenant-tick (not per-assignment) means one bad row can still lose other assignments' generated instances for that tick even though it no longer crashes the whole job

---

## 8. Key Architectural Decisions

| Decision | Rationale | Reference |
|---|---|---|
| Six roles, set-valued region scope for `admin`, dedicated `store_kiosk` role | Rejected a single scalar-region model (too restrictive for multi-region admins) and treating kiosk as "just a view" (need claim-by-name with no personal login) | [ADR-0001](adr/0001-six-role-multi-region-authorization.md) |
| Authorization scope as one pure, DI-free module (`ScopeSpec`) rather than marker interfaces or per-entity checks | Marker-interface/per-entity approaches were leaky or non-EF-translatable; no `IScopeAuthorizer` interface since there's one real implementation | [ADR-0002](adr/0002-scope-authorizer-pure-module.md) |
| Vertical Slice Architecture (backend) | Cohesion over layers; avoids "service sprawl" of a horizontal (Controller/Service/Repository) split | `docs/PROJECT_STATE.md`, Key Architectural Decisions section |
| JWT in memory (Angular Signal) + refresh token in httpOnly cookie | Access token never touches localStorage (XSS-resistant); refresh survives page reload | — |
| Refresh-token rotation, `IsUsed` flag | Prevents replay of a stolen refresh token | — |
| `TaskTemplate.Fields` as JSONB, not EAV tables | Fields are schema-flexible per template type without table explosion | — |
| `ChecklistTemplateItems` junction (not FK on template) | Same template reusable across multiple checklists | — |

---

## 9. Where Things Live (quick reference)

- Backend feature code: `backend/OpsFlow.Api/Features/<Area>/<Operation>/`
- Authorization module: `backend/OpsFlow.Domain/Authorization/`
- Background jobs: `backend/OpsFlow.Api/Jobs/`
- EF migrations: `backend/OpsFlow.Infrastructure/Migrations/{Master,Tenant}/`
- Frontend apps: `frontend/apps/{dashboard,field-pwa}`
- Frontend shared libs: `frontend/libs/{data-access,ui,util}/*`
- Design system spec: `.claude/skills/opsflow-design-system/SKILL.md`
- ADRs: `docs/adr/`
- Product requirements: `docs/product/OpsFlow_PRD_V2.md` (current, as-built — supersedes V1); `docs/product/OpsFlow_PRD_V1.md` (original design spec, historical); `docs/product/OpsFlow_Release_Notes_2026-07.md` (plain-language what's-new for the 2026-07-14 release)
- Domain glossary: `docs/planning/ubiquitous-language.md`
- Build plan (waves/tracer bullets): `docs/planning/Tracer_Bullets_V1.md`
- Skills (project + Matt Pocock pack, curated subset): `.claude/skills/` — one dir per skill, flat, no subcategories. `mp-*` prefix = sourced from the Matt Pocock Skills pack (`Matt Pacock Skills/` at repo root has the full pack, including categories not yet curated in — deprecated/, in-progress/, personal/).
- Git safety hook (blocks `reset --hard`, `clean -f`/`-fd`, `checkout .`/`restore .`, `branch -D`, force-push — not routine push): `.claude/hooks/block-dangerous-git.sh`, wired in `.claude/settings.json`.

---

## 10. Repo Hygiene (2026-07-16)

A parallel session ran an unstashed, forceful branch reset from the repo root and wiped another session's uncommitted work (an `email`-claim auth refactor — recovered from the losing session's own handoff notes, see PR history on `feat/track-b-post-prd-audit`). That incident, plus a pile of long-standing stray root-level content nobody had cleaned up, prompted a full pass:

- **Branches:** every branch except `main` and `feat/track-b-post-prd-audit` (the currently open PR at the time) was fully merged and has been deleted, both remote and local. If you see any other branch in the future, either it's genuinely new work in progress or it's drift that should be cleaned up the same way — confirm with `git branch -r --merged origin/main` plus `gh pr list --state all` (watch for squash-merges: a branch can be fully merged in content while `--merged` still calls it unmerged, because the squash commit isn't the branch's own commit).

**2026-07-19: `feat/track-b-post-prd-audit` renamed to `dev`.** That branch had, since 2026-07-14, become the de facto long-lived working branch for essentially all ongoing development (PRs #90 through #110) — its name only ever described the *first* thing built on it (Track B of a PRD-gap audit), not its actual role. Once PR #110 merged with nothing left outstanding, it was renamed to `dev` to describe what it actually is. **`dev` is that same branch, same history — just the current name for "the active long-lived working branch."** Every reference to `feat/track-b-post-prd-audit` elsewhere in this doc (and in `PROJECT_STATE.md`/release notes) is an accurate historical record of what was true when it was written — left as-is rather than rewritten, but don't go looking for a branch by that name; it's `dev` now. The one-branch-forever convention itself (rather than a fresh branch per feature) is unusual but has held up fine so far — no need to "fix" it, just know it's deliberate at this point, not an oversight.
- **Stray root content** (`Design Process/`, `bencium-controlled-ux-designer/`, `frontend-design/`, `awesome-fixes.md`, root `PROJECT_STATE.md`, a broken `awesome-ux-skills` submodule gitlink with no `.gitmodules` entry) has been cleaned up — moved into `.claude/skills/`/`docs/` where it had real content, deleted where it didn't. See the `chore: repo hygiene` commit for the full list.
- **Guardrail hook installed** (see §9 above) specifically to prevent a repeat of the incident that prompted this.

If you're a future session and something here looks stale again, it means someone (agent or human) made an uncommitted or unreconciled change since — don't assume the previous state was wrong without checking `git log`/`git status` first.

---

## 11. Repo Hygiene (2026-07-17)

A second instance of the §10 problem turned up: several sessions' worth of already-deployed work (the onboarding-walkthrough fixes, template-cleanup UX, and this session's new mini-dashboards feature) had been built and pushed to production directly from a local working tree, but never actually committed. `git status` on `feat/track-b-post-prd-audit` showed ~44 modified/untracked files with no corresponding commits — production and `main` had quietly diverged for at least a day.

- All of it was reviewed, grouped into 6 logically-scoped commits, pushed, and merged as [PR #104](https://github.com/numairs-ui/OpsFlow/pull/104) — see that PR's commit list for the individual pieces.
- **Lesson:** `az acr build`/`vercel --prod` deploy whatever is on disk, committed or not. After any manual deploy, commit and push before ending the session — don't let "verified live" substitute for "committed." If a future session finds `git status` dirty with features that match what's supposedly already live, that's this exact situation recurring, not a false alarm.
