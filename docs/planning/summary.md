# OpsFlow — Workspace Summary

> **Purpose of this file:** a single-shot context primer you can hand to another agent (or human) so they understand what this workspace is, what we're building, how it's architected, and where things live. Last generated 2026-06-28.

---

## 1. What we're building

**OpsFlow** is a **multi-tenant retail operations platform** — the "OpsFlow Module" of the broader **MealDynamics (MD)** product. It is the operational backbone for restaurant/retail chains:

- **Regions** contain **Stores**.
- Stores run **recurring task checklists** and **submit forms**.
- Work flows **up a role hierarchy** for monitoring, review, and approval.
- Stores also log **cash bank deposits** and **inventory snapshots** for financial/operational compliance.

Think "Zenput-style" field operations + checklist/task management + form approval workflows, rebuilt as a modern multi-tenant SaaS. (The `zenput transcript.md` and SRS/PRD docs in the root capture the original requirements gathering.)

The canonical glossary lives in [CONTEXT.md](CONTEXT.md) and [ubiquitous-language.md](ubiquitous-language.md) — **read CONTEXT.md first** for domain language; it deliberately carries zero implementation detail.

---

## 2. The 6-role authorization model (core concept)

Authorization is the spine of the product. Six roles, scoped from network-wide down to a single store (see [docs/adr/0001-six-role-multi-region-authorization.md](docs/adr/0001-six-role-multi-region-authorization.md)):

| Role | Scope | Can do |
|------|-------|--------|
| `super_admin` | Whole tenant (network-wide) | Everything: org structure, all regions/stores/users, tenant settings, System-scope templates |
| `admin` | An assigned **set** of regions | Manage stores/users/templates within those regions. Cannot create super_admins or other admins |
| `supervisor` | A **single** region | Regional/Store templates, monitor stores, review/approve forms in that region |
| `store_manager` | A **single** store | Recurring tasks, deposits, roster, forms |
| `store_employee` | A **single** store | Claim/start/complete/defer tasks, submit forms |
| `store_kiosk` | A single store, **shared station** | Read today's board + claim-by-name only (no personal login; walk-up staff type their name) |

**Scope** also applies to templates/checklists/form templates: **System** (whole tenant) / **Regional** (one region) / **Store** (one store).

> ⚠️ This is a **v2 role model** — migrated from an older model. The old `admin` login is now `super_admin` after a live cutover on the `bajco-dev` tenant. Region scope is a **set**, emitted as repeated `regionId` JWT claims and persisted as `RegionIdsCsv`.

**Authorization implementation** (see [docs/adr/0002-scope-authorizer-pure-module.md](docs/adr/0002-scope-authorizer-pure-module.md)): a **pure module** in [backend/OpsFlow.Domain/Authorization/](backend/OpsFlow.Domain/Authorization/) — a `Caller` value object `{ Role, StoreId, RegionIds }` becomes a `ScopeSpec` exposing EF-translatable `IQueryable` extensions (key-selector filters like `t => t.StoreId`) for list visibility plus assertion methods for read/write access. **No interface, no DI, no DB access** — handlers pass already-loaded data into the assertions.

---

## 3. Backend — `backend/` (.NET 9 / C#)

A **vertical-slice** ASP.NET Core API. Solution: [backend/OpsFlow.sln](backend/OpsFlow.sln). Six projects:

| Project | Role |
|---------|------|
| `OpsFlow.Api` | HTTP API, vertical-slice features, MediatR handlers, endpoints, jobs, hubs |
| `OpsFlow.Domain` | Entities, authorization module, interfaces (no infra deps) |
| `OpsFlow.Infrastructure` | EF Core DbContexts, migrations, Supabase + Azure provider implementations |
| `OpsFlow.Migrations` | Migration runner |
| `OpsFlow.Tests.Unit` | Unit tests |
| `OpsFlow.Tests.Integration` | Integration tests |

### Tech stack
- **.NET 9**, ASP.NET Core (minimal API style)
- **MediatR 12** — every feature is a Command/Query + Handler
- **FluentValidation 11** — wired via a `ValidationBehaviour` pipeline (also `LoggingBehaviour`)
- **EF Core 9** — multi-tenant (see below)
- **JWT Bearer auth** — issuer `OpsFlow`, audience `OpsFlow.API`
- **Quartz.NET** — scheduled jobs (recurring assignments → generate task instances)
- **SignalR** — realtime updates (with an Azure SignalR provider option)
- **OpenAPI + Scalar UI** — API docs/explorer
- **Providers are pluggable**: `INFRASTRUCTURE_PROVIDER` / `DATABASE_PROVIDER` env vars switch between **Supabase** (auth, storage, realtime, Postgres) and **Azure** (ASP.NET Identity auth, Blob storage, Azure SignalR).

### Multi-tenancy
- **Master DB** (`MasterDbContext`) — tenant registry.
- **Per-tenant DB** (`TenantDbContext`) — all operational data, resolved per request.
- Migrations are split: `Migrations/Master/` and `Migrations/Tenant/`. Tenant migration history (newest → `AddMultiRegionScope`, 2026-06-27) tracks the schema's evolution: org structure → task templates → checklists → recurring assignments/tasks → task lifecycle → inventory/store settings → deposit log → forms domain → multi-region scope.

### Domain entities — [backend/OpsFlow.Domain/Entities/](backend/OpsFlow.Domain/Entities/)
`Tenant`, `Region`, `Store`, `StoreSettings`, `UserProfile`, `UserStoreAssignment`, `RefreshToken`, `TaskTemplate`, `Checklist`, `ChecklistTemplateItem`, `RecurringAssignment`, `TaskInstance`, `TaskCompletion`, `FormTemplate`, `FormSubmission`, `FormSubmissionApprovalStep`, `DepositLog`, `InventorySnapshot`.

### API features (vertical slices) — [backend/OpsFlow.Api/Features/](backend/OpsFlow.Api/Features/)
Each subfolder is one slice (Command/Query + Handler + endpoint):
- **Auth** — Login, Logout, Refresh (JWT + refresh tokens)
- **Regions / Stores / StoreSettings** — org structure CRUD
- **Users** — CRUD, activate/deactivate, store assignments, activity
- **Templates** — task template CRUD + import; **Checklists** — CRUD + item ordering
- **RecurringAssignments** — schedule CRUD, pause, delete (cron-driven)
- **Tasks** — full lifecycle: Create, Assign, Claim, Start, Complete, Verify, Defer, Cancel, GetToday/GetTasks/GetTask
- **FormTemplates** — CRUD + deactivate; **FormSubmissions** — create/draft/submit + approval workflow (Approve, Reject, Return), GetPendingReview, GetMySubmissions
- **DepositLog** — record + query daily cash deposits
- **Inventory** — latest + history snapshots
- **Dashboard** — System / Region / Store roll-ups
- **Me** — current user's completions; **TenantSettings**; **Health**

---

## 4. Frontend — `frontend/` (Nx monorepo, Angular 21)

**Nx 22.7** monorepo, **Angular** (standalone components, Signals, strict typing). Build via **Vite/AnalogJS**, test via **Vitest**, e2e via **Playwright**.

> **Angular version note:** the *eventual deployment target is Angular 17* (per AGENTS.md). The workspace currently has Angular 21 installed — this is fine for the present goal of getting a **working prototype** running on **Supabase + Vercel**; the version will be reconciled down to 17 before the production deployment.

### Two apps — `frontend/apps/`
1. **`dashboard`** — the back-office web app for admin/supervisor/manager roles. Route areas:
   - `admin/` — overview, regions, stores, store-settings, users, templates, checklists, recurring-assignments, form-templates, template-import, tenant-settings, admin-shell
   - `supervisor/` — overview, supervisor-shell
   - `manager/` — overview, deposit, roster, manager-shell
   - `shared/form-submissions`, `login`, `unauthorized`
2. **`field-pwa`** — the field/store PWA for employees & kiosks: `dashboard`, `tasks`, `task-detail`, `submissions`, `quick-template`, `kiosk`, `login`, `unauthorized`.

(Plus `dashboard-e2e` and `field-pwa-e2e` Playwright projects.)

### Shared libraries — `frontend/libs/`
- `data-access/{auth,core,org,tasks,templates}` — API clients & state
- `ui/{core,cron-picker,field-builder,template-builder}` — reusable UI (note the form **field-builder** and **template-builder** — the dynamic form/checklist designers)
- `util/{guards,models}` — route guards & shared TS models

> Frontend has its own [frontend/CLAUDE.md](frontend/CLAUDE.md): **always drive tasks through `nx`** (`nx build`/`run`/`affected`), use the `nx-workspace` / `nx-generate` skills, and prefix with the package manager.

> **Legacy app (archived):** the original React Native / Expo prototype was moved to [archive/opsflow-app-react-native/](archive/opsflow-app-react-native/) on 2026-06-28. It is kept for historical reference only — **do not build on it**. The Angular monorepo above is the only active frontend.

---

## 5. The "3-Layer Agentic Architecture" (how this workspace itself is meant to be operated)

This is a **meta-layer** about how AI agents work *in* the repo (defined in [AGENTS.md](AGENTS.md), mirrored to `CLAUDE.md`/`GEMINI.md`):

1. **Layer 1 — Directives** (`directives/`): Markdown SOPs = deterministic intent.
2. **Layer 2 — Orchestration**: the AI agent routes intent → tools, recovers from failures.
3. **Layer 3 — Execution** (`execution/`): pure, testable Python scripts (JSON stdout, no interactive prompts).
- **Self-Annealing Loop**: when a tool fails, fix the script *and* update the directive so the workspace gets smarter each run.
- `.tmp/` is scratch; permanent deliverables live in cloud/GitHub.
- Commit attribution footer required: `Co-Authored-By: Antigravity <antigravity@google.com>`.

Current directives: `backend_setup.md`, `bajco_template_onboarding.md`, `f0890_field_map.md` (the F0890 is a sample data-collection form being mapped — see `F0890 DP sample (1).pdf`).

---

## 6. Process, issue tracking & docs

- **Issues/PRDs**: GitHub issues on `numairs-ui/OpsFlow` via the `gh` CLI (external PRs are not a triage surface). Triage labels: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See [docs/agents/](docs/agents/).
- **Architecture decisions**: [docs/adr/](docs/adr/) (0001 = roles, 0002 = scope authorizer).
- **Dev methodology** (per AGENTS.md): vertical slices / "tracer bullets", deep modules over shallow ones, TDD red-green-refactor, standalone Angular components + Signals + strict typing.
- **Deployment**:
  - **Current goal — working prototype on Supabase + Vercel.** The frontend (Angular) targets **Vercel**; data/auth/storage/realtime run on **Supabase** (the backend's `INFRASTRUCTURE_PROVIDER=supabase` path).
  - [render.yaml](render.yaml) exists for deploying the .NET API to **Render** as a Docker web service off `main` (health check `/health`, Supabase provider, `task-photos` bucket, master DB connection string, JWT secret). Useful as the API host while the frontend is on Vercel.

### Key reference docs in the repo root
- `OpsFlow_PRD_V1.md` — product requirements
- `Restaurant_Checklist_and_Task_Management_System_Complete_SRS.md` — full SRS
- `MealDynamics_Opsflow_Strategy_Document.md` — strategy
- `MealDynamics_OpsFlow_Style_Guide.md` — design/style guide
- `Tracer_Bullets_V1.md` — the build plan (vertical slices)
- `UI_Completion_Roadmap.md` — frontend roadmap
- `GitHub_Issues_V1.md` — issue backlog snapshot
- `design/` — design assets/mockups (MVP design briefs, information architecture, V1 mobile screenshots)
- `frontend-design/`, `Design Process/` — vendored skill packages, not project design docs

---

## 7. Development status (from git history)

Work is organized in **"Waves"**. Recent commits:
- **Waves 0–15**: backend API, Angular dashboard + field-pwa, Forms domain
- **Wave 16**: task allocation, manager/supervisor home views, field-builder UX
- **Wave 17**: actioned 23 audit findings (`awesome-fixes.md`)
- Most recent schema change: **multi-region scope** migration (2026-06-27) — the v2 six-role model rollout.

There are uncommitted modifications across many backend handlers and frontend files (the role-model v2 / multi-region scope work is mid-flight at the time of writing).

---

## 8. Quick orientation for a new agent

1. Read [CONTEXT.md](CONTEXT.md) for domain language, then this file.
2. Backend feature work → find the matching slice in `backend/OpsFlow.Api/Features/<Area>/<Action>/`; it's a MediatR Command/Query + Handler. Authorization → `OpsFlow.Domain/Authorization` (pure, pass data in).
3. Frontend work → use `nx` from `frontend/`; UI lives in `apps/dashboard` (back-office) or `apps/field-pwa` (field/kiosk); shared code in `libs/`.
4. Schema change → add an EF migration under `OpsFlow.Infrastructure/Migrations/Tenant/` (or `/Master/`).
5. Respect the 6-role + 3-scope model everywhere — it's the product's core invariant.
