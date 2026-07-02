# OpsFlow V1 — Tracer Bullet Decomposition

**Version:** 1.1  
**Date:** 2026-06-10 (Architecture Update — Grilling Phase 2)  
**PRD Reference:** `OpsFlow_PRD_V1.md`  
**Methodology:** Vertical Slice Architecture — each TB crosses all required layers end-to-end  
**Execution model:** Parallel waves (Sand Castle) — all TBs within a wave are independently grabbable  
**Total TBs:** 79 across 16 waves (Wave 0 – Wave 15)

---

## How to Read This Document

Each Tracer Bullet (TB) represents **one feature action** crossing all necessary layers:

| Layer tag | Meaning |
|-----------|---------|
| `DB` | EF Core migration or model/seed change |
| `API` | .NET Vertical Slice handler + endpoint |
| `SVC` | Angular service in `libs/data-access/` |
| `UI` | Angular component (feature or shared lib) |
| `INFRA` | Infrastructure, config, or pipeline work |
| `JOB` | .NET background job (Quartz.NET / IHostedService) |

**Rules for agents implementing a TB:**
1. Read the referenced FD section in `OpsFlow_PRD_V1.md` before writing a line of code
2. Do not implement anything listed in Section 8 (V1 Out-of-Scope) of the PRD
3. Every TB must have passing tests before marking done (unit + integration per the testing strategy)
4. Do not start a TB until all its `Depends on` TBs are merged

---

## Dependency Wave Map

```
Wave 0: Workspace & Infrastructure (TB-01–TB-06, TB-72)
  └── Wave 1: Authentication (TB-07–TB-12)
        └── Wave 2: Organisation Structure (TB-13–TB-19)
              └── Wave 3: Task Template System (TB-20–TB-25)
                    ├── Wave 4: Checklist System (TB-26–TB-28)
                    │     └── Wave 5: Recurring Assignments & Ad Hoc Tasks (TB-29–TB-34)
                    │           └── Wave 6: Task Board & Real-Time (TB-35–TB-41)
                    │                 └── Wave 7: Task Completion & Lifecycle (TB-42–TB-47)
                    │                       ├── Wave 8: Notification System (TB-48–TB-50)
                    │                       ├── Wave 9: Manager Walk (TB-51–TB-56)
                    │                       ├── Wave 10: MDOG & Inventory (TB-57–TB-60)
                    │                       ├── Wave 11: Safe, Till & Deposit Log (TB-61–TB-63)
                    │                       └── Wave 13: Dashboards (TB-65–TB-68)
                    ├── Wave 14: Admin Panel Remaining (TB-69–TB-71, TB-74)
                    └── Wave 15: Forms (TB-73, TB-75–TB-79)
                          ├── Wave 15a: Form Templates — FD-18 (TB-73)
                          └── Wave 15b: Form Submissions — FD-19 (TB-75–TB-79)
Wave 12: Red Book — RETIRED (no TBs)
```

---

## Wave 0 — Workspace & Infrastructure

> **Goal:** A running, deployable skeleton. No features, but every layer exists, connects, and is tested by CI.  
> **Dependencies:** None — start here.

---

### TB-01: Nx Monorepo Workspace Scaffold

**FD:** FD-01 (foundation)  
**Layers:** `INFRA`  
**Depends on:** —

Set up the Nx monorepo with Angular 17 strict mode. Create two apps (`field-pwa`, `dashboard`) and three shared library categories (`libs/data-access/`, `libs/ui/`, `libs/util/`). Configure Nx boundary rules so apps cannot import from each other — only from `libs/`.

**Acceptance Criteria:**
- [ ] `nx generate @nx/angular:app field-pwa` and `dashboard` both exist with standalone component config
- [ ] `libs/data-access/core`, `libs/ui/core`, `libs/util/models` exist as initial placeholder libs
- [ ] `nx graph` shows no boundary violations
- [ ] `nx build field-pwa` and `nx build dashboard` both succeed with zero errors
- [ ] Nx `enforce-module-boundaries` ESLint rule configured and passing
- [ ] `tsconfig` strict mode enabled across all projects

---

### TB-02: .NET 9 VSA Solution Scaffold

**FD:** FD-01 (foundation)  
**Layers:** `INFRA`  
**Depends on:** —

Create the .NET 9 solution with Vertical Slice Architecture folder structure. Install MediatR, FluentValidation, EF Core 9, and Quartz.NET. Establish the `Features/{Domain}/{Action}/` convention with a single working hello-world slice (`Features/Health/GetStatus/`) to prove the pattern end-to-end.

**Acceptance Criteria:**
- [ ] Solution structure: `OpsFlow.Api/`, `OpsFlow.Domain/`, `OpsFlow.Infrastructure/`, `OpsFlow.Tests.Unit/`, `OpsFlow.Tests.Integration/`
- [ ] `Features/Health/GetStatus/` slice: command + handler + endpoint — `GET /health` returns `{ status: "ok" }`
- [ ] MediatR pipeline: logging + validation behaviours wired
- [ ] FluentValidation integrated via MediatR pipeline; invalid requests return `400` with error details
- [ ] `dotnet test` passes on both test projects (initially empty)
- [ ] `.NET Aspire` or `launchSettings.json` configured for local dev

---

### TB-03: Database-Per-Tenant EF Core Setup

**FD:** FD-01  
**Layers:** `DB`, `API`  
**Depends on:** TB-02

Implement the multi-tenant database resolution pattern. The `TenantDbContextFactory` reads a `tenantId` from the JWT claim (or a `X-Tenant-Id` dev header) and resolves the correct connection string. A master `OpsFlowMasterDb` database stores `Tenants` with connection strings. An EF Core migration orchestrator CLI command applies migrations to all tenant databases.

**Acceptance Criteria:**
- [ ] `Tenants` table in master DB: `{ id, name, connectionString, createdAt, isActive }`
- [ ] `TenantDbContextFactory` resolves connection string from `tenantId` JWT claim on every request
- [ ] Tenant context is unavailable without a valid `tenantId` — requests without one return `401`
- [ ] `dotnet run --migrate-all` CLI command applies pending EF Core migrations to every active tenant DB
- [ ] A seed script creates one "dev" tenant database locally with all migrations applied
- [ ] Integration test: two tenant DBs are created; a query on tenant A does not return data from tenant B

---

### TB-04: CI/CD Pipeline

**FD:** FD-01 (foundation)  
**Layers:** `INFRA`  
**Depends on:** TB-01, TB-02

GitHub Actions workflow: on every PR, run `dotnet test` and `nx affected --target=test`. On merge to `main`, build and deploy to Azure App Service (.NET) and Azure Static Web Apps (Angular). Environments: `dev` and `prod`.

**Acceptance Criteria:**
- [ ] PR workflow: .NET unit + integration tests pass; Nx affected tests pass
- [ ] PR workflow: `nx build field-pwa` and `nx build dashboard` succeed
- [ ] Merge-to-main workflow: deploys .NET API to Azure App Service `dev` slot
- [ ] Merge-to-main workflow: deploys `field-pwa` and `dashboard` to Azure Static Web Apps
- [ ] Secrets stored in GitHub Actions secrets (never in code)
- [ ] Pipeline status badge in `README.md`

---

### TB-05: Azure Infrastructure Provisioning

**FD:** FD-01 (foundation)  
**Layers:** `INFRA`  
**Depends on:** TB-04

Provision production Azure resources: App Service Plan, App Service (.NET 9 API), Azure SQL Server + master database, Azure Static Web Apps (field-pwa + dashboard), Azure Blob Storage account + container, Azure SignalR Service. Document all resource names and connection strings in `.env.example`.

**Acceptance Criteria:**
- [ ] All resources exist in Azure under a single Resource Group (`opsflow-prod`)
- [ ] App Service runs the `GET /health` endpoint from TB-02 publicly
- [ ] Azure SQL master DB is created and accessible from App Service
- [ ] Blob Storage account has a `task-photos` container with private access
- [ ] Azure SignalR Service is in `Serverless` mode, connected to App Service
- [ ] `.env.example` documents all required environment variables
- [ ] No connection strings committed to source control

---

### TB-06: Angular PWA + FCM Setup

**FD:** FD-15  
**Layers:** `INFRA`, `SVC`  
**Depends on:** TB-01

Configure `field-pwa` as an installable PWA: `@angular/pwa` Service Worker, `manifest.webmanifest`, and Firebase Cloud Messaging. Create `libs/data-access/notifications` with a `NotificationTokenService` that registers the FCM device token after permission is granted. Wire the `POST /notifications/register-token` call (endpoint stubbed — implemented in TB-48).

**Acceptance Criteria:**
- [ ] `field-pwa` passes Chrome Lighthouse PWA audit (installable, service worker registered)
- [ ] FCM permission prompt appears on first load; user can grant or deny
- [ ] On grant: FCM token retrieved and stored in `NotificationTokenService` signal
- [ ] `NotificationTokenService.registerToken()` is called after successful login (TB-11 will wire this)
- [ ] Service Worker precaches the app shell; `field-pwa` loads from cache with no network (verified in DevTools)
- [ ] Firebase project configured; `firebase-messaging-sw.js` present and registered

---

### TB-72: Dev Environment Setup + Adapter Interface Scaffolding

**FD:** FD-01 (foundation)  
**Layers:** `INFRA`, `API`  
**Depends on:** TB-02, TB-03

Set up Supabase as the local dev infrastructure. Create the three adapter interfaces (`IAuthProvider`, `IStorageProvider`, `IRealtimeService`) and their Supabase concrete implementations. Register the correct concrete via DI based on the `INFRASTRUCTURE_PROVIDER` env var (`supabase` | `azure`). Azure concretions are stubbed (no-op) for dev but wired for production.

**Acceptance Criteria:**
- [ ] `IAuthProvider` interface defined: `AuthenticateAsync(email, password)`, `CreateUserAsync(...)`, `ResetPasswordAsync(...)`
- [ ] `IStorageProvider` interface defined: `GetUploadUrlAsync(path)`, `DeleteAsync(path)`
- [ ] `IRealtimeService` interface defined: `BroadcastAsync(group, event, payload)`, `JoinGroupAsync(connectionId, group)`
- [ ] `SupabaseAuthProvider`, `SupabaseStorageProvider`, `SupabaseRealtimeService` concrete classes created in `OpsFlow.Infrastructure/Supabase/`
- [ ] `AzureBlobStorageProvider` and `AzureSignalRService` concrete classes created in `OpsFlow.Infrastructure/Azure/` (fully implemented for production)
- [ ] `AspNetIdentityAuthProvider` wraps ASP.NET Core Identity; used for Azure production path
- [ ] `INFRASTRUCTURE_PROVIDER=supabase` in `.env` → Supabase concretions registered; `azure` → Azure concretions registered
- [ ] EF Core `DbContext` selects `UseNpgsql()` (Supabase dev) or `UseSqlServer()` (Azure prod) based on `DATABASE_PROVIDER` env var
- [ ] Integration test: with `INFRASTRUCTURE_PROVIDER=supabase`, Supabase concrete is resolved from DI; with `azure`, Azure concrete is resolved
- [ ] `.env.example` updated with all new env vars

---

## Wave 1 — Authentication

> **Goal:** Users can log in. JWTs are issued, stored safely, and attached to every request. Route guards protect all future routes.  
> **Dependencies:** Wave 0 complete (TB-01, TB-02, TB-03).

---

### TB-07: Login API — JWT Minting

**FD:** FD-02  
**Layers:** `DB`, `API`  
**Depends on:** TB-03

`POST /auth/login` accepts email + password, validates against ASP.NET Core Identity in the tenant DB, and returns `{ accessToken, refreshToken, expiresIn }`. Access tokens expire in 15 minutes. JWT payload: `sub`, `tenantId`, `role`, `storeId` (nullable), `regionId` (nullable).

**Acceptance Criteria:**
- [ ] `POST /auth/login` with valid credentials returns `200` with JWT payload containing all required claims
- [ ] `POST /auth/login` with invalid credentials returns `401`
- [ ] Access token expires in 15 minutes (validated in integration test with a mocked clock)
- [ ] Refresh token stored in `RefreshTokens` table: `{ userId, token (hashed), expiresAt, createdAt }`
- [ ] Refresh token response is set as an `HttpOnly` cookie (not in the JSON body)
- [ ] Unit test covers all login failure branches (wrong password, deactivated user, unknown email)

---

### TB-08: Token Refresh + Logout API

**FD:** FD-02  
**Layers:** `API`  
**Depends on:** TB-07

`POST /auth/refresh` reads the refresh token from the `HttpOnly` cookie, validates it against the DB, and issues a new access token + rotated refresh token. `POST /auth/logout` invalidates the refresh token in the DB.

**Acceptance Criteria:**
- [ ] `POST /auth/refresh` with valid cookie returns a new access token and rotated refresh token
- [ ] `POST /auth/refresh` with expired or missing cookie returns `401`
- [ ] `POST /auth/refresh` with an already-used refresh token returns `401` (rotation invalidates previous token)
- [ ] `POST /auth/logout` marks refresh token as used; subsequent refresh with same token returns `401`
- [ ] Integration test covers the full login → refresh → logout flow

---

### TB-09: Angular AuthService + HTTP Interceptor

**FD:** FD-02  
**Layers:** `SVC`  
**Depends on:** TB-07, TB-08

Create `libs/data-access/auth` with `AuthService`. Stores access token in memory (a Signal: `accessToken = signal<string | null>(null)`). An `AuthInterceptor` attaches the Bearer token to every outgoing HTTP request. On `401` response, the interceptor calls `AuthService.refresh()` once and retries; on second `401`, calls `AuthService.logout()`.

**Acceptance Criteria:**
- [ ] `AuthService.login()` calls `POST /auth/login`, stores access token in Signal, navigates to home route
- [ ] `AuthService.logout()` calls `POST /auth/logout`, clears Signal, navigates to `/login`
- [ ] `AuthService.refresh()` calls `POST /auth/refresh`; on success updates Signal; on failure logs out
- [ ] `AuthInterceptor` attaches `Authorization: Bearer <token>` to every request except `/auth/*`
- [ ] `AuthInterceptor` handles `401` with exactly one refresh retry
- [ ] `currentUser = computed(() => decodeJwt(accessToken()))` exposes role, storeId, regionId as a Signal
- [ ] Unit tests cover all branches with mocked HttpClient

---

### TB-10: Angular Route Guards

**FD:** FD-02  
**Layers:** `SVC`  
**Depends on:** TB-09

Create `AuthGuard` and `RoleGuard` in `libs/util/guards`. `AuthGuard` redirects unauthenticated users to `/login`. `RoleGuard` accepts a `requiredRole` parameter and redirects insufficient-role users to `/unauthorized`.

**Acceptance Criteria:**
- [ ] `AuthGuard`: unauthenticated → redirects to `/login`; authenticated → allows navigation
- [ ] `RoleGuard`: role insufficient → redirects to `/unauthorized`; role sufficient → allows navigation
- [ ] Guards use `AuthService.currentUser` Signal — no HTTP calls
- [ ] `/unauthorized` route exists in both apps with a simple message and back link
- [ ] Unit tests cover all guard branches with mocked `AuthService`

---

### TB-11: Field PWA Login Page

**FD:** FD-02  
**Layers:** `UI`  
**Depends on:** TB-09, TB-10

Login page component for `field-pwa`: email + password form, submit calls `AuthService.login()`, loading and error states handled via Signals. Mobile-optimised layout (44×44px touch targets, large type). After login, registers FCM token (TB-06).

**Acceptance Criteria:**
- [ ] Login form validates email format and non-empty password before submission
- [ ] Loading spinner shown during login request; form disabled
- [ ] Error message shown on `401` ("Invalid email or password")
- [ ] Successful login navigates to `/tasks` (Task Board — stub route for now)
- [ ] FCM token registration called after successful login
- [ ] Passes WCAG 2.1 AA contrast check
- [ ] Renders correctly on iPhone SE (375px) and standard Android (390px)

---

### TB-12: Dashboard Login Page

**FD:** FD-02  
**Layers:** `UI`  
**Depends on:** TB-09, TB-10

Login page for the `dashboard` app. Can share the login form component from `libs/ui/auth` if extracted. Desktop-optimised layout. After login, navigates to role-appropriate home: `/admin` (admin), `/supervisor` (supervisor), `/manager` (store_manager).

**Acceptance Criteria:**
- [ ] Same functional behaviour as TB-11
- [ ] Role-based post-login redirect: admin → `/admin`, supervisor → `/supervisor`, store_manager → `/manager`
- [ ] Desktop layout (centred card, 400px max-width)
- [ ] Shared login form component extracted to `libs/ui/auth` if not done in TB-11

---

## Wave 2 — Organisation Structure

> **Goal:** Admins can configure the tenant's organisational hierarchy. The data that all future features depend on (stores, regions, users) exists and is manageable.  
> **Dependencies:** TB-07, TB-09, TB-10 (auth fully functional).

---

### TB-13: Admin Panel Shell + Navigation

**FD:** FD-17  
**Layers:** `UI`  
**Depends on:** TB-10, TB-12

Scaffold the admin section of the `dashboard` app. A persistent sidebar navigation with sections: Users, Stores, Regions, System Templates, Store Settings, Tenant Settings. All routes are guarded by `RoleGuard({ role: 'admin' })`. Includes a breadcrumb component.

**Acceptance Criteria:**
- [ ] `/admin` route group guarded — non-admin role redirects to `/unauthorized`
- [ ] Sidebar renders all navigation sections; active section highlighted
- [ ] Breadcrumb updates on route change
- [ ] Layout is responsive down to 1024px viewport width
- [ ] Navigating to `/admin` without auth redirects to `/login` (AuthGuard)

---

### TB-14: Region CRUD

**FD:** FD-03  
**Layers:** `DB`, `API`, `SVC`, `UI`  
**Depends on:** TB-03, TB-13

`Regions` table. Full CRUD: list, create, edit, deactivate. Admin UI in the Admin Panel.

**Acceptance Criteria:**
- [ ] `Regions` table: `{ id, tenantId, name, description, isActive, createdAt }`
- [ ] `GET /regions` returns all regions (active filter optional)
- [ ] `POST /regions` creates a region; name is unique within tenant
- [ ] `PUT /regions/{id}` updates name/description
- [ ] `POST /regions/{id}/deactivate` soft-deletes; deactivated regions are excluded from dropdowns
- [ ] Admin UI: list table with inline deactivate; "New Region" slide-over form
- [ ] Integration tests cover all endpoints including auth/role enforcement

---

### TB-15: Store CRUD

**FD:** FD-03  
**Layers:** `DB`, `API`, `SVC`, `UI`  
**Depends on:** TB-14

`Stores` table. Full CRUD. Each store belongs to a Region. Admin UI in the Admin Panel.

**Acceptance Criteria:**
- [ ] `Stores` table: `{ id, tenantId, regionId, name, address, isActive, createdAt }`
- [ ] `GET /stores` returns stores (filterable by `regionId`, `isActive`)
- [ ] `POST /stores` requires a valid `regionId`
- [ ] `PUT /stores/{id}` updates name, address, regionId
- [ ] `POST /stores/{id}/deactivate` deactivates store and all its active RecurringAssignments
- [ ] `GET /regions/{id}/stores` returns stores for a region
- [ ] Admin UI: list table grouped by region; "New Store" form with Region dropdown
- [ ] Integration tests cover all endpoints

---

### TB-16: User CRUD + Role Assignment

**FD:** FD-03  
**Layers:** `DB`, `API`, `SVC`, `UI`  
**Depends on:** TB-15

Create user accounts via Admin Panel. Each user gets a role (`store_employee`, `store_manager`, `supervisor`, `admin`). `store_employee` requires a `storeId`. `supervisor` requires a `regionId`. Password is set by Admin; user must change on first login.

**Acceptance Criteria:**
- [ ] `POST /users` creates a user with hashed password via ASP.NET Core Identity
- [ ] Role-specific validation: `store_employee` requires `storeId`; `supervisor` requires `regionId`; `admin` requires neither
- [ ] `GET /users` returns all users (filterable by role, storeId, isActive)
- [ ] `PUT /users/{id}` updates name, email, role, store/region assignment
- [ ] `GET /users/{id}` returns user detail
- [ ] Admin UI: user table with search/filter; "New User" form with role-conditional fields
- [ ] First-login password change flow (flag `MustChangePassword` in Identity)
- [ ] Integration tests cover role-specific validation

---

### TB-17: UserStoreAssignments — Multi-Store Manager

**FD:** FD-03  
**Layers:** `DB`, `API`, `SVC`, `UI`  
**Depends on:** TB-16

Store Managers can be assigned to multiple stores via a join table. Admin UI shows an assignment panel per Store Manager user.

**Acceptance Criteria:**
- [ ] `UserStoreAssignments` table: `{ userId, storeId, assignedAt, assignedByAdminId }`
- [ ] `POST /users/{id}/store-assignments` adds a store assignment (Store Manager role required on user)
- [ ] `DELETE /users/{id}/store-assignments/{storeId}` removes an assignment
- [ ] `GET /users/{id}/store-assignments` returns all stores assigned to a user
- [ ] JWT `storeId` claim for Store Managers contains their primary store (first assigned); all assignments used for auth checks server-side
- [ ] Admin UI: multi-select store assignment panel on Store Manager user detail page
- [ ] Integration test: Store Manager can access data for all assigned stores; blocked from non-assigned stores

---

### TB-18: User Deactivation

**FD:** FD-03  
**Layers:** `API`, `UI`  
**Depends on:** TB-16

Admins can deactivate a user. Deactivated users cannot log in. Historical task completions and records attributed to them are retained.

**Acceptance Criteria:**
- [ ] `POST /users/{id}/deactivate` sets `IsActive = false` on the user
- [ ] Deactivated user's `POST /auth/login` returns `401` with message "Account deactivated"
- [ ] Refresh tokens for deactivated users are invalidated immediately
- [ ] `POST /users/{id}/reactivate` re-enables the account
- [ ] Admin UI: deactivate/reactivate toggle with confirmation dialog
- [ ] Historical task completions by deactivated user remain intact and queryable

---

### TB-19: Store Employee Roster View

**FD:** FD-03  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-16, TB-17

Store Managers can see which employees are assigned to their store(s). A read-only roster on the `dashboard` app.

**Acceptance Criteria:**
- [ ] `GET /stores/{id}/employees` returns active `store_employee` users for the store
- [ ] Store Manager can only query stores they are assigned to — `403` otherwise
- [ ] Dashboard UI: roster list showing name, email, assignment date
- [ ] Supervisor can view rosters for any store in their region

---

## Wave 3 — Task Template System

> **Goal:** Templates can be authored, listed, and managed. The Dynamic Field Builder is complete and reusable. All subsequent task features build on top of this.  
> **Dependencies:** TB-14, TB-15, TB-16 (org structure fully set up).

---

### TB-20: Dynamic Field Builder Component

**FD:** FD-04  
**Layers:** `UI`  
**Depends on:** TB-01

Create `libs/ui/field-builder` — a reusable Angular component for authoring the dynamic field array of a Task Template. Supports all five field types: `Numeric`, `Boolean`, `Text`, `Photo`, `Checklist`. Each field type has its own config panel (range min/max, corrective action text, checklist sub-items). Fields can be added, reordered (drag-and-drop), and removed.

**Acceptance Criteria:**
- [ ] All five field types selectable; each renders a type-specific config form
- [ ] `Numeric`: label, required, rangeMin (optional), rangeMax (optional), correctiveActionText (optional)
- [ ] `Boolean`: label, required, correctiveActionText (optional, fires on "No")
- [ ] `Text`: label, required
- [ ] `Photo`: label, required
- [ ] `Checklist`: label; ordered sub-items each with label + required flag; sub-items can be added/reordered/removed
- [ ] Field order drag-and-drop (CDK DragDrop)
- [ ] Component emits `FieldsChange` output event with updated `TemplateField[]` array
- [ ] Used as a pure UI component — no direct API calls; parent form manages submission

---

### TB-21: Create Task Template

**FD:** FD-04  
**Layers:** `DB`, `API`, `SVC`, `UI`  
**Depends on:** TB-15, TB-20

`POST /templates` creates a Task Template. Desktop authoring UI uses the Dynamic Field Builder (TB-20). Template scope (`System | Regional | Store`) determines which IDs are required.

**Acceptance Criteria:**
- [ ] `TaskTemplates` table: `{ id, tenantId, name, description, category, scope, regionId (nullable), storeId (nullable), fields (JSON), isActive, createdByUserId, createdAt }`
- [ ] `POST /templates` validates: System scope requires `admin` role; Regional requires `supervisor`+; Store requires `store_manager`+
- [ ] `fields` JSON stored as a validated structure — invalid field definitions rejected with `400`
- [ ] Desktop UI: "New Template" form with name, category, scope selector (role-filtered), and Field Builder
- [ ] Scope selector shows only allowed scopes for the current user's role
- [ ] On save: navigates to template detail/list
- [ ] Integration test: scope-role combinations (System by store_manager → 403; Store by admin → 200)

---

### TB-22: List + Filter Task Templates

**FD:** FD-04  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-21

`GET /templates` returns templates visible to the current user (System templates always included; Regional templates for user's region; Store templates for user's store). Filterable by scope, category, and isActive.

**Acceptance Criteria:**
- [ ] `GET /templates` returns templates scoped to the user's visibility without extra params
- [ ] Query params: `scope`, `category`, `isActive`, `search` (name contains)
- [ ] Store Manager sees: System + their Region's + their Store's templates
- [ ] Supervisor sees: System + their Region's templates (+ any store's templates in their region)
- [ ] Admin sees all templates
- [ ] Desktop UI: searchable, filterable table with scope badge, category, active status
- [ ] Pagination: 20 per page

---

### TB-23: Edit Task Template

**FD:** FD-04  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-21, TB-22

`PUT /templates/{id}` updates a template. System templates cannot be edited by Store Managers or Supervisors. A template with active Recurring Assignments cannot have its `fields` array changed (only name/description/category editable).

**Acceptance Criteria:**
- [ ] `PUT /templates/{id}`: role-scope check enforced (same rules as creation)
- [ ] System templates: `admin` only can edit
- [ ] Templates with active Recurring Assignments: `fields` changes blocked with `409` + clear error message
- [ ] Desktop UI: edit form pre-populated from existing template; Field Builder shows existing fields
- [ ] On save: optimistic UI update + success toast

---

### TB-24: Deactivate Task Template

**FD:** FD-04  
**Layers:** `API`, `UI`  
**Depends on:** TB-23

`POST /templates/{id}/deactivate` soft-deletes a template. Blocked if active Recurring Assignments reference it. `POST /templates/{id}/activate` re-enables.

**Acceptance Criteria:**
- [ ] `POST /templates/{id}/deactivate` returns `409` with error if active RecurringAssignments exist
- [ ] Deactivated templates excluded from template pickers in Recurring Assignment and ad hoc task forms
- [ ] Deactivated templates still visible in list with a filter toggle ("Show inactive")
- [ ] Admin UI: deactivate/activate toggle with confirmation dialog

---

### TB-25: Field PWA Quick Template Creation

**FD:** FD-04  
**Layers:** `UI`, `SVC`  
**Depends on:** TB-21

A simplified "quick template" form on `field-pwa` for Store Managers creating an ad hoc Store-scope template on the floor. Supports title, category, and up to 5 basic fields (Numeric, Boolean, Text only — no Checklist or Photo on this form). Uses the same `POST /templates` endpoint.

**Acceptance Criteria:**
- [ ] Only available to `store_manager` and above on `field-pwa`
- [ ] Form: name, category (dropdown), scope auto-set to `Store` and `storeId` auto-populated from JWT
- [ ] Field types limited to Numeric, Boolean, Text
- [ ] Max 5 fields enforced with clear UI indication
- [ ] On save: navigates to ad hoc task creation (TB-34) with new template pre-selected

---

## Wave 4 — Checklist System

> **Goal:** Templates can be grouped into named Checklists. Checklists can be assigned and scheduled like standalone templates.  
> **Dependencies:** TB-21 (templates exist and can be referenced).

---

### TB-26: Create Checklist + Manage Items

**FD:** FD-05  
**Layers:** `DB`, `API`, `SVC`, `UI`  
**Depends on:** TB-21

`POST /checklists` creates a Checklist. An ordered list of Task Template references defines the checklist's items. Items can be added, reordered, and removed.

**Acceptance Criteria:**
- [ ] `Checklists` table: `{ id, tenantId, name, description, scope, regionId (nullable), storeId (nullable), isActive, createdByUserId }`
- [ ] `ChecklistTemplateItems` table: `{ checklistId, templateId, order }` (composite PK)
- [ ] `POST /checklists` creates checklist; same scope/role rules as TaskTemplates
- [ ] `PUT /checklists/{id}/items` replaces the ordered item list (full replace, not patch)
- [ ] Templates in a checklist must be visible to the creator (scope validation)
- [ ] Desktop UI: checklist builder with a template picker + drag-to-reorder item list
- [ ] Integration test: scope-role validation

---

### TB-27: List Checklists

**FD:** FD-05  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-26

`GET /checklists` with same visibility rules as templates. Desktop UI list view.

**Acceptance Criteria:**
- [ ] `GET /checklists` returns checklists visible to the current user (same scoping as templates)
- [ ] Each result includes `itemCount` and a preview of first 3 template names
- [ ] Desktop UI: list table with scope badge, item count, active status
- [ ] Filterable by scope, isActive; searchable by name

---

### TB-28: Deactivate Checklist

**FD:** FD-05  
**Layers:** `API`, `UI`  
**Depends on:** TB-27

`POST /checklists/{id}/deactivate` soft-deletes. Blocked if active Recurring Assignments reference it.

**Acceptance Criteria:**
- [ ] `POST /checklists/{id}/deactivate` returns `409` if active RecurringAssignments exist
- [ ] `POST /checklists/{id}/activate` re-enables
- [ ] Admin UI: toggle with confirmation dialog

---

## Wave 5 — Recurring Assignments & Ad Hoc Tasks

> **Goal:** Tasks can be scheduled to recur and instantly created ad hoc. The background job generates Task Instances from Recurring Assignments. The full creation surface exists on both apps.  
> **Dependencies:** TB-21 (templates), TB-26 (checklists), TB-15 (stores), TB-33 not yet — Quartz job is in this wave.

---

### TB-29: Cron Picker Component

**FD:** FD-06  
**Layers:** `UI`  
**Depends on:** TB-01

`libs/ui/cron-picker` — a recurrence picker that generates a valid 5-part cron string without exposing raw cron syntax to the user. Options: Daily, Weekly (day selector), Monthly (date selector), Custom (expert mode with cron preview). Also supports `isOneTime` (specific date picker, no recurrence).

**Acceptance Criteria:**
- [ ] Emits `{ cronExpression: string, isOneTime: boolean, oneTimeDate?: Date }` on change
- [ ] Daily: `0 6 * * *` (default 06:00, time picker adjustable)
- [ ] Weekly: `0 6 * * 1` (day-of-week selector)
- [ ] Monthly: `0 6 1 * *` (day-of-month selector)
- [ ] Custom: raw cron input with real-time human-readable preview ("Every Monday at 6:00 AM")
- [ ] One-Time: date + time picker; emits `isOneTime: true`
- [ ] Invalid cron strings are caught and shown as an inline error
- [ ] Component is purely presentational — no API calls

---

### TB-30: Create Recurring Assignment (Single Store)

**FD:** FD-06  
**Layers:** `DB`, `API`, `SVC`, `UI`  
**Depends on:** TB-21, TB-26, TB-29

`POST /recurring-assignments` creates a Recurring Assignment binding a template or checklist to a single assignment target (user or store) with a cron schedule.

**Acceptance Criteria:**
- [ ] `RecurringAssignments` table: `{ id, tenantId, templateId (nullable), checklistId (nullable), cronExpression, isOneTime, assignedToUserId (nullable), assignedToStoreId (nullable), targetStoreIds (JSON nullable), isActive, createdByUserId, lastFiredAt (nullable) }`
- [ ] Validation: exactly one of `templateId` / `checklistId` required; exactly one of `assignedToUserId` / `assignedToStoreId` required (for single-store; `targetStoreIds` handled in TB-31)
- [ ] Store Manager can only assign within their own store(s) — `403` otherwise
- [ ] Desktop UI: form with template/checklist picker, assignment type (individual/store), user/store selector, Cron Picker (TB-29)
- [ ] PWA UI: simplified form for Store Manager creating a store-assignment recurring task
- [ ] Integration test: Store Manager assigning to another store's user → `403`

---

### TB-31: Multi-Store Recurring Assignment (Supervisor)

**FD:** FD-06  
**Layers:** `API`, `UI`  
**Depends on:** TB-30

Extends Recurring Assignments to support `targetStoreIds` — a Supervisor broadcasts one template to multiple stores simultaneously. Validates that all target stores are within the Supervisor's region.

**Acceptance Criteria:**
- [ ] `POST /recurring-assignments` with `targetStoreIds` array: validates all stores are in Supervisor's region
- [ ] Non-supervisors cannot use `targetStoreIds` — `403`
- [ ] Desktop UI: multi-select store picker (filtered to Supervisor's region) replaces single-store dropdown when "Multiple Stores" is toggled
- [ ] `GET /recurring-assignments/{id}` shows `targetStoreIds` and indicates it is a broadcast assignment
- [ ] Integration test: Supervisor with storeId outside their region in `targetStoreIds` → `422`

---

### TB-32: Manage Recurring Assignments

**FD:** FD-06  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-30

List, view, pause, resume, and delete Recurring Assignments. Deletion does not remove already-generated Task Instances.

**Acceptance Criteria:**
- [ ] `GET /recurring-assignments` returns assignments visible to the user (scoped by store/region)
- [ ] `POST /recurring-assignments/{id}/pause` sets `isActive = false`
- [ ] `POST /recurring-assignments/{id}/resume` sets `isActive = true`
- [ ] `DELETE /recurring-assignments/{id}` removes the assignment; generated Tasks unaffected
- [ ] Desktop UI: list table with next-fire-time display, pause/resume toggle, delete button
- [ ] Next fire time calculated from cron expression and displayed in human-readable format

---

### TB-33: Quartz.NET Background Job — Task Instance Generation

**FD:** FD-06  
**Layers:** `JOB`, `DB`  
**Depends on:** TB-30, TB-31

A Quartz.NET job runs every minute, evaluates active Recurring Assignments, and generates Task Instances when a cron fires. For multi-store broadcasts, generates one Task Instance per store. `IsOneTime` assignments are deactivated after first fire.

**Acceptance Criteria:**
- [ ] `RecurringAssignmentJob` runs every minute via Quartz.NET scheduler
- [ ] For each active RecurringAssignment: evaluates cron using `CronExpression.IsTimeNow()` (or equivalent); generates Task Instance(s) if due
- [ ] Task Instance created: `{ id, tenantId, templateId, checklistInstanceId (if checklist), storeId, assignedToUserId (nullable), assignedToStoreId (nullable), status: 'Open', dueAt, createdAt }`
- [ ] `IsOneTime` assignments: `isActive` set to `false` after generating first instance
- [ ] `lastFiredAt` updated on every fire
- [ ] If checklist: one `ChecklistInstance` created, one Task Instance per `ChecklistTemplateItem`
- [ ] Job is idempotent: re-running for the same minute does not create duplicate instances (checked via `lastFiredAt`)
- [ ] Integration test: mock Quartz trigger; assert correct number of Task Instances created for a multi-store broadcast

---

### TB-34: Ad Hoc Task Creation

**FD:** FD-06  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-21, TB-29

`POST /tasks` creates a Task Instance directly (no Recurring Assignment). Template is optional — if omitted, the manager provides all field definitions inline (stored directly on the Task, not a template). Mandatory: assignment (user or store).

**Acceptance Criteria:**
- [ ] `POST /tasks` with `templateId`: copies field definitions from template into the task instance
- [ ] `POST /tasks` without `templateId`: accepts inline `fields` JSON (same schema as template fields)
- [ ] Validation: one of `assignedToUserId` / `assignedToStoreId` is required
- [ ] `dueAt` is required for ad hoc tasks
- [ ] Desktop UI: "New Task" drawer — template picker (optional), assignment selector, due date/time picker; if no template, Field Builder inline
- [ ] PWA UI: simplified "New Task" button — title, assignment (store or self), due time, one field only
- [ ] SignalR broadcast to store group on creation
- [ ] Integration test: creating without assignment → `400`; creating with another store's user (Store Manager) → `403`

---

## Wave 6 — Task Board & Real-Time

> **Goal:** The Field PWA is live. Store Employees can see their tasks, the board updates in real-time, and the Store Kiosk is operational.  
> **Dependencies:** TB-33 (task instances exist), TB-34 (ad hoc tasks), TB-06 (PWA + FCM), TB-10 (guards).

---

### TB-35: Task Board Shell — Field PWA

**FD:** FD-07  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-33, TB-34, TB-11

`GET /stores/{id}/tasks/today` returns all Task Instances for the store for the current date. The Field PWA renders them grouped by Checklist (with completion percentage) and standalone tasks below. Task status colours and overdue highlighting.

**Acceptance Criteria:**
- [ ] `GET /stores/{id}/tasks/today` returns tasks grouped in the response: `{ checklists: [...], standaloneTasks: [...] }`
- [ ] Each task includes: id, title, status, dueAt, assignedToUserId, assignedToStoreId, checklistId, checklistName
- [ ] PWA layout: Checklist accordion groups with "X/Y complete" counter; standalone tasks flat list below
- [ ] Visual status indicators: Open (grey), In Progress (blue), Overdue (red + elapsed time), Completed (green), Verified (green + check)
- [ ] Overdue tasks sorted to top within their group
- [ ] Store Manager's view shows all store tasks; Store Employee sees own + store-level tasks
- [ ] Store Manager route accessible on `field-pwa` with a toggle to "Store View" vs "My View"
- [ ] `GET /stores/{id}/tasks/today` returns `403` for users not assigned to that store

---

### TB-36: SignalR Real-Time Task Board

**FD:** FD-07  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-35, TB-05

The Task Board subscribes to the SignalR `store-{storeId}` group. Events update the board in real-time via Angular Signals without page refresh. The .NET API dispatches events to the hub on task state changes.

**Acceptance Criteria:**
- [ ] `TaskBoardHub` on .NET API: clients join `store-{storeId}` group on connect (auth required)
- [ ] `.NET TaskHub.BroadcastTaskUpdate(storeId, taskUpdate)` called whenever a task changes status
- [ ] Angular `TaskBoardService` opens HubConnection on board init; closes on destroy
- [ ] `tasks = signal<Task[]>([])` updated via `effect()` when SignalR events arrive — no manual refresh needed
- [ ] New task appearing on board (from Recurring Assignment job firing) — client sees it within 500ms
- [ ] Connection loss: reconnect indicator shown; board re-fetches on reconnect
- [ ] Azure SignalR Service (not in-process) used in production — confirmed by integration test hitting real hub

---

### TB-37: Task Detail View + Dynamic Field Form

**FD:** FD-07, FD-09  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-35

`GET /tasks/{id}` returns the task including its field definitions and any existing completion. Field PWA renders a dynamic form: each field type renders the appropriate input control. Required fields block submission.

**Acceptance Criteria:**
- [ ] `GET /tasks/{id}` returns: task metadata + `fields` array (from template or inline) + `completion` (null if not yet completed)
- [ ] Dynamic form renders each field type correctly:
  - `Numeric`: number input with range hint displayed
  - `Boolean`: toggle/checkbox
  - `Text`: textarea
  - `Photo`: camera button (opens device camera); captured image previewed
  - `Checklist`: ordered list of checkboxes, required items marked
- [ ] Required fields show validation error on attempted submission without value
- [ ] "In Progress" status set when user opens task detail and has not yet completed it (server-side via `PATCH /tasks/{id}/status`)
- [ ] Completed tasks show the completion values read-only with completion timestamp and name

---

### TB-38: Photo Upload Flow

**FD:** FD-09  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-05, TB-37

`GET /tasks/{id}/photo-upload-url` generates an Azure Blob Storage pre-signed SAS URL (15-minute expiry). The Angular client uploads directly to Azure Blob; the blob URL is stored as the field value on completion submission.

**Acceptance Criteria:**
- [ ] `GET /tasks/{id}/photo-upload-url` returns `{ uploadUrl, blobUrl }` — SAS URL expires in 15 minutes
- [ ] Client uploads directly to Azure Blob via `PUT` to the `uploadUrl`
- [ ] `blobUrl` (the persistent Azure URL without SAS params) is what's stored in `TaskCompletionFieldValues`
- [ ] `libs/data-access/storage` service encapsulates the upload flow: request SAS → upload → return blobUrl
- [ ] Upload progress indicator shown in the Photo field component
- [ ] Failed uploads show a retry button
- [ ] Blob naming convention: `{tenantId}/{storeId}/{taskId}/{fieldId}/{timestamp}.jpg`
- [ ] Integration test: valid task ID returns a SAS URL; URL actually accessible (or mocked for test)

---

### TB-39: Claim a Store-Level Task

**FD:** FD-07, FD-08  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-35

`POST /tasks/{id}/claim` assigns an unowned store-level task to the current user (if authenticated) or stores a volunteer name (if kiosk/unauthenticated flow). Prevents double-claiming.

**Acceptance Criteria:**
- [ ] `POST /tasks/{id}/claim` body: `{ volunteerName?: string }` — if authenticated and no volunteerName, claims as the current user
- [ ] Can only claim tasks with `assignedToStoreId` set (not personally-assigned tasks) — `409` otherwise
- [ ] Cannot claim an already-claimed task — `409` with "Task already claimed"
- [ ] On claim: task `assignedToUserId` set (or `CompletedByVolunteerName` set); status stays Open; SignalR broadcast
- [ ] Task Board: "Claim" button replaced with claimant name after claiming
- [ ] Kiosk flow (TB-41): volunteer name text input on claim; no auth required for this action

---

### TB-40: Partial Offline — Service Worker + Submission Queue

**FD:** FD-07  
**Layers:** `INFRA`, `SVC`  
**Depends on:** TB-06, TB-37

Angular Service Worker precaches the Task Board and Task Detail routes. Task completion submissions made while offline are queued in `localStorage` and replayed in order when connectivity restores.

**Acceptance Criteria:**
- [ ] `ngsw-config.json` precaches: `/tasks`, `/tasks/*`, `field-pwa` app shell
- [ ] Task Board loads from cache with no network (verified in Chrome DevTools offline mode)
- [ ] `OfflineQueueService` in `libs/data-access/offline`: queues `POST /tasks/{id}/complete` requests when offline
- [ ] On network restore: queued submissions replayed in FIFO order; each result processed; board refreshed
- [ ] Offline indicator banner (amber) shown when `navigator.onLine === false`
- [ ] Duplicate submission prevention: if a task was already completed server-side while offline, the queued submission is discarded (idempotent endpoint handles this)
- [ ] Unit tests cover queue add, replay, and discard-on-duplicate logic

---

### TB-41: Store Kiosk View

**FD:** FD-08  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-35, TB-39

A dedicated Kiosk route on `field-pwa` (`/kiosk`) accessible via the store-level account. Shows the full store task board (all tasks, all statuses). Financial task fields (Till amounts, deposit amounts) are hidden. Any person can claim and complete a task via volunteer name entry.

**Acceptance Criteria:**
- [ ] `/kiosk` route uses the kiosk session JWT (issued for the store account, no individual user)
- [ ] `GET /stores/{id}/tasks/today` kiosk mode: financial field values omitted from response
- [ ] All task statuses visible: open, in progress, overdue, completed, verified
- [ ] Claim flow: tap task → modal with "Enter your name" text input + optional "Log in with your account" link
- [ ] Volunteer claim sets `CompletedByVolunteerName`; employee login sets `CompletedByUserId`
- [ ] Session does not expire (refresh token with 365-day TTL for kiosk accounts)
- [ ] Financial tasks (category `Safe`) hidden from kiosk task list entirely (not just values hidden)
- [ ] Live updates via SignalR (same `store-{storeId}` group)

---

## Wave 7 — Task Completion & Lifecycle

> **Goal:** The full task lifecycle is operational. Tasks can be completed, verified, cancelled, deferred, and promoted to overdue.  
> **Dependencies:** TB-35, TB-37 (Task Board and Detail view exist).

---

### TB-42: Complete a Task

**FD:** FD-09  
**Layers:** `DB`, `API`, `SVC`, `UI`  
**Depends on:** TB-37, TB-38

`POST /tasks/{id}/complete` submits field values, runs range validation, records the completion, and returns any triggered corrective actions.

**Acceptance Criteria:**
- [ ] `TaskCompletions` table: `{ id, taskId, completedByUserId (nullable), completedByVolunteerName (nullable), fieldValues (JSON), correctiveActionsTriggered (JSON), completedAt }`
- [ ] Server validates all required fields are present — `400` with field-level errors if not
- [ ] Range evaluation: for each `Numeric` field, if value outside `[rangeMin, rangeMax]` → add to `correctiveActionsTriggered`
- [ ] Boolean false + correctiveActionText defined → add to `correctiveActionsTriggered`
- [ ] Unchecked required Checklist sub-items → block completion with `400`
- [ ] Response: `{ completion: {...}, triggeredCorrectiveActions: [{ fieldName, text }] }`
- [ ] Task status transitions to `Completed`; SignalR broadcast to store group
- [ ] `POST /tasks/{id}/complete` is idempotent: second call returns existing completion with `200`
- [ ] Integration tests cover: valid completion, range breach, required field missing, idempotency

---

### TB-43: Corrective Action Inline Display

**FD:** FD-10  
**Layers:** `UI`  
**Depends on:** TB-42

After task submission, if `triggeredCorrectiveActions` is non-empty, the Task Detail view transitions to a "Corrective Action Required" screen listing each triggered action. The Task Board marks the task with an amber badge.

**Acceptance Criteria:**
- [ ] Task Detail view: on completion response, if `triggeredCorrectiveActions.length > 0`, show a distinct "Action Required" panel
- [ ] Panel lists: field name + corrective action text for each triggered action
- [ ] Task Board: tasks with triggered corrective actions show an amber indicator badge
- [ ] The corrective action panel is dismissible (acknowledged) — sets a local flag, does not call the API
- [ ] Manager viewing a completed task with triggered actions sees the same panel on the Task Detail

---

### TB-44: Verify a Task

**FD:** FD-09  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-42

`POST /tasks/{id}/verify` is available to `store_manager` and above. Transitions task from `Completed` to `Verified`.

**Acceptance Criteria:**
- [ ] `POST /tasks/{id}/verify`: requires `store_manager`+ role; requires task status = `Completed` — `409` otherwise
- [ ] Task status transitions to `Verified`; `verifiedByUserId` and `verifiedAt` recorded
- [ ] SignalR broadcast to store group on verification
- [ ] Dashboard UI: "Verify" button on completed tasks in Store Manager view
- [ ] Integration test: verify by Store Employee → `403`; verify an Open task → `409`

---

### TB-45: Cancel a Task

**FD:** FD-09  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-37

`POST /tasks/{id}/cancel` requires a mandatory reason. Available to `store_manager` and above. Terminal state.

**Acceptance Criteria:**
- [ ] `POST /tasks/{id}/cancel` body: `{ reason: string }` — reason required; blank reason → `400`
- [ ] Available from any non-terminal state (Open, In Progress, Overdue)
- [ ] Task status transitions to `Cancelled`; `cancelledByUserId`, `cancelReason`, `cancelledAt` recorded
- [ ] `Cancelled` tasks remain on the Task Board for the day (greyed out, no actions)
- [ ] `Cancelled` tasks do not count toward completion rate
- [ ] SignalR broadcast; reason visible to Store Manager on task detail
- [ ] UI: "Cancel Task" button on Task Detail (manager-only); reason input modal

---

### TB-46: Defer a Task

**FD:** FD-09  
**Layers:** `API`, `SVC`, `UI`, `JOB`  
**Depends on:** TB-37

`POST /tasks/{id}/defer` requires a mandatory reason and a target date. On the target date, a background job resets the task to `Open`.

**Acceptance Criteria:**
- [ ] `POST /tasks/{id}/defer` body: `{ reason: string, deferredTo: date }` — both required; `deferredTo` must be after today
- [ ] Task status transitions to `Deferred`; `deferredTo`, `deferReason`, `deferredByUserId` recorded
- [ ] `Deferred` tasks do not appear on the Task Board (hidden from `GET /stores/{id}/tasks/today`)
- [ ] Quartz.NET job (runs at 06:00 AM daily): queries `Tasks WHERE status = 'Deferred' AND deferredTo = today`; resets each to `Open`; SignalR broadcast
- [ ] UI: "Defer Task" button on Task Detail (manager-only); reason input + date picker modal
- [ ] Integration test: defer to tomorrow → not visible today; mock clock to tomorrow → appears as Open

---

### TB-47: Overdue Promotion Job

**FD:** FD-09  
**Layers:** `JOB`  
**Depends on:** TB-33

A Quartz.NET job runs every 5 minutes and promotes tasks past their `dueAt` deadline from `Open` or `InProgress` to `Overdue`. A grace period (configurable, default 30 minutes) before `Overdue` transitions to `CorrectiveActionRaised`.

**Acceptance Criteria:**
- [ ] `OverduePromotionJob` runs every 5 minutes
- [ ] Queries: `Tasks WHERE status IN ('Open', 'InProgress') AND dueAt < NOW()`
- [ ] Transitions matched tasks to `Overdue`; SignalR broadcast to store group per task
- [ ] Second pass: `Tasks WHERE status = 'Overdue' AND dueAt < NOW() - graceMinutes`; transitions to `CorrectiveActionRaised`
- [ ] Grace period configurable via `StoreSettings.overdueGraceMinutes` (default 30)
- [ ] `FCM Standard` push to Store Manager when any task transitions to `Overdue` (wired via TB-48)
- [ ] Integration test: task with `dueAt` in the past → status becomes `Overdue` after job runs

---

## Wave 8 — Notification System

> **Goal:** Every operational event fires the correct notification to the correct recipient via the correct channel and priority.  
> **Dependencies:** TB-42 (completions exist), TB-06 (FCM configured), TB-47 (overdue job exists).

---

### TB-48: NotificationService — Central Dispatch

**FD:** FD-15  
**Layers:** `API`, `DB`  
**Depends on:** TB-06, TB-42

The single .NET `NotificationService` class handles all notification dispatch. No feature handler sends notifications directly — they call `NotificationService.Dispatch(event)`. Implements `POST /notifications/register-token` and `DELETE /notifications/register-token`.

**Acceptance Criteria:**
- [ ] `FcmDeviceTokens` table: `{ userId, token, deviceType, lastRefreshedAt, tenantId }`
- [ ] `POST /notifications/register-token` upserts token for the authenticated user
- [ ] `DELETE /notifications/register-token` removes the token (on logout)
- [ ] `NotificationService.Dispatch(NotificationEvent event)` routes to SignalR, FCM, or both based on event type
- [ ] FCM delivery failure (stale token): token is deleted from `FcmDeviceTokens` on `404` response from FCM
- [ ] Unit tests: each event type routes to the correct channel
- [ ] No feature handler (e.g., `CompleteTaskHandler`) calls FCM or SignalR directly — all go through `NotificationService`

---

### TB-49: FCM Standard Push Notifications

**FD:** FD-15  
**Layers:** `API`, `SVC`  
**Depends on:** TB-48

Wire Standard-priority FCM pushes for: task assigned to user, task overdue, Walk Session completed, Red Book entry posted (if spec available).

**Acceptance Criteria:**
- [ ] `TaskAssigned` event → Standard FCM push to `assignedToUserId`'s device tokens
- [ ] `TaskOverdue` event → Standard FCM push to the store's `store_manager` users
- [ ] FCM payload: `{ title, body, data: { taskId, type } }` — clicking opens the relevant task on the PWA
- [ ] Angular Service Worker: `push` event handler routes FCM messages to the correct in-app route via `data.type`
- [ ] Notification displayed by OS even when app is backgrounded (Service Worker handles)
- [ ] Integration test (with FCM emulator or mocked): correct payload dispatched on task assignment

---

### TB-50: FCM Critical Push + Deposit Escalation Job

**FD:** FD-15, FD-13  
**Layers:** `API`, `JOB`  
**Depends on:** TB-48, TB-47

Wire Critical-priority FCM pushes: temperature out-of-range (on task completion with `Numeric` field breach), overdue escalation to `CorrectiveActionRaised`. Implement the 10:00 AM deposit escalation Quartz.NET job.

**Acceptance Criteria:**
- [ ] `TemperatureViolation` event (triggered in `CompleteTaskHandler` when a temperature-categorised numeric field breaches range) → Critical FCM to Store Manager
- [ ] `CorrectiveActionRaised` event (triggered in `OverduePromotionJob`) → Critical FCM to Store Manager + Supervisor
- [ ] FCM `priority: "high"` set on all Critical events
- [ ] `DepositEscalationJob` runs daily at 10:00 AM (Quartz.NET cron: `0 10 * * *`): checks for stores with no `DepositLog` record for today → Critical FCM to the region's Supervisor
- [ ] Integration test: trigger a temperature breach via `CompleteTask` → verify Critical FCM dispatched
- [ ] Integration test: no deposit by 10:00 AM → verify Supervisor receives Critical push

---

## Wave 9 — Manager Walk

> **Goal:** Managers can conduct structured, scored audits using Walk Templates. Sessions produce reports and auto-generate corrective Tasks.  
> **Dependencies:** TB-21 (templates pattern), TB-42 (task completion pattern), TB-48 (notifications), TB-38 (photo upload).

---

### TB-51: Walk Template CRUD

**FD:** FD-11  
**Layers:** `DB`, `API`, `SVC`, `UI`  
**Depends on:** TB-21

`WalkTemplates` and `WalkAuditItems` tables. CRUD for Walk Templates via the Admin/Supervisor section of the `dashboard`. Same three-tier scope as Task Templates.

**Acceptance Criteria:**
- [ ] `WalkTemplates` table: `{ id, tenantId, name, scope, regionId (nullable), storeId (nullable), isActive, createdByUserId }`
- [ ] `WalkAuditItems` table: `{ id, templateId, label, description, scoringType (PassFail|OneToFive), weight, photoRequired, failCorrectiveActionText, order }`
- [ ] `POST /walk-templates` validates scope/role rules identical to Task Templates
- [ ] `GET /walk-templates` uses same visibility scoping as `GET /templates`
- [ ] `PUT /walk-templates/{id}` + `POST /walk-templates/{id}/deactivate`
- [ ] Desktop UI: Walk Template list + builder form with audit item management
- [ ] Integration tests cover scope-role rules

---

### TB-52: Walk Audit Item Builder Component

**FD:** FD-11  
**Layers:** `UI`  
**Depends on:** TB-20, TB-51

`libs/ui/walk-audit-item-builder` — a reusable component for defining Walk Audit Items within a Walk Template form. Mirrors the approach of the Dynamic Field Builder (TB-20).

**Acceptance Criteria:**
- [ ] Supports `PassFail` and `OneToFive` scoring types
- [ ] Each item: label, description (optional), scoring type, weight (default 1.0), photoRequired toggle, failCorrectiveActionText (optional)
- [ ] Items can be added, reordered, and removed
- [ ] Emits `AuditItemsChange` output with updated `WalkAuditItem[]`
- [ ] Weight input validates to a positive number

---

### TB-53: Start Walk Session

**FD:** FD-11  
**Layers:** `DB`, `API`, `SVC`, `UI`  
**Depends on:** TB-51

`POST /walk-sessions` opens a Walk Session. The `dashboard` app renders a session shell with one card per audit item.

**Acceptance Criteria:**
- [ ] `WalkSessions` table: `{ id, tenantId, templateId, storeId, conductedByUserId, startedAt, completedAt (nullable), compositeScore (nullable) }`
- [ ] `WalkSessionItems` table: `{ id, sessionId, auditItemId, status (Pending|Pass|Fail|NA), score (nullable), notes (nullable), photoBlobUrl (nullable) }`
- [ ] `POST /walk-sessions` creates session + one `WalkSessionItem` per template audit item (status: Pending)
- [ ] Store Manager can only start sessions for their assigned stores
- [ ] Dashboard UI: Walk Session page showing template name, store, start time, and a card per audit item (all Pending)
- [ ] In-progress sessions are resumable: `GET /walk-sessions/{id}` returns current state

---

### TB-54: Score Walk Session Items

**FD:** FD-11  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-53, TB-38

`PATCH /walk-sessions/{id}/items/{itemId}` updates an individual audit item's score, notes, and photo. Progress auto-saves — no "save all" button.

**Acceptance Criteria:**
- [ ] `PATCH /walk-sessions/{id}/items/{itemId}` body: `{ status, score (nullable), notes (nullable), photoBlobUrl (nullable) }`
- [ ] `status = Fail` and `photoRequired = true` → `photoBlobUrl` is required in the request — `400` otherwise
- [ ] Auto-save: PATCH called on every field blur (debounced 500ms)
- [ ] Dashboard UI: each audit item card has a Pass/Fail/NA toggle (or 1–5 rating for OneToFive), notes textarea, photo upload button
- [ ] Photo upload uses the same SAS URL pattern (TB-38) — `GET /walk-sessions/{id}/photo-upload-url`
- [ ] Progress indicator: "X / Y items scored" shown in session header
- [ ] Partially completed session is resumable after browser refresh (state loaded from API)

---

### TB-55: Submit Walk Session

**FD:** FD-11  
**Layers:** `API`, `SVC`, `UI`, `JOB`  
**Depends on:** TB-54, TB-34, TB-48

`POST /walk-sessions/{id}/submit` closes the session, calculates the composite score, and auto-generates corrective action Tasks for all `Fail` items with a `failCorrectiveActionText`.

**Acceptance Criteria:**
- [ ] All `WalkSessionItems` must be in a non-Pending status before submit is allowed — `409` with list of pending items otherwise
- [ ] Composite score formula: `sum(item.score * item.weight) / sum(item.weight)` — normalised to 0–100
- [ ] PassFail scoring: Pass = full weight, Fail = 0, NA = excluded from calculation
- [ ] For each `Fail` item with `failCorrectiveActionText`: creates a Task Instance (`POST /tasks` internally) assigned to `assignedToStoreId` with `dueAt = end-of-day`
- [ ] `WalkSession.completedAt` and `compositeScore` written
- [ ] SignalR broadcast to `store-{storeId}`: new corrective Tasks created
- [ ] FCM Standard push to Store Manager: "Walk complete — N corrective tasks generated"
- [ ] Integration test: session with 2 fail items → 2 Task Instances created with correct storeId

---

### TB-56: Walk Session Report View

**FD:** FD-11  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-55

`GET /walk-sessions/{id}` returns the full session report. Dashboard renders a readable report with score, item-by-item results, photos, and links to generated corrective Tasks.

**Acceptance Criteria:**
- [ ] `GET /walk-sessions/{id}` returns full session data including all items, photos, generated task IDs, and composite score
- [ ] `GET /stores/{id}/walk-sessions` returns paginated session history for a store; filterable by date range
- [ ] Supervisor endpoint: `GET /regions/{id}/walk-sessions` — all sessions across the region
- [ ] Dashboard report view: score badge (colour-coded), item list with Pass/Fail/NA indicators, photos (clickable to full size), corrective task links
- [ ] PDF export — out of scope (V2); no export button in V1

---

## Wave 10 — MDOG & Inventory

> **Goal:** The MDOG daily inventory workflow is operational. Previous day's counts pre-populate today's task form. The 56-Degree Rule is enforced.  
> **Dependencies:** TB-42 (task completion pipeline), TB-15 (stores), TB-21 (templates).

---

### TB-57: MDOG System Template Seed

**FD:** FD-12  
**Layers:** `DB`  
**Depends on:** TB-21

EF Core seed data: a System-scope Task Template for the MDOG daily inventory check. Includes `Numeric` fields for each dough size (10", 12", 14", 16") and a temperature check field with the 56-Degree Rule range constraint.

**Acceptance Criteria:**
- [ ] Seed applied in tenant provisioning migration (`dotnet run --migrate-all` includes seed)
- [ ] MDOG template: `{ name: "Daily MDOG Check", scope: "System", category: "Inventory" }`
- [ ] Fields: `Dough_10in` (Numeric), `Dough_12in` (Numeric), `Dough_14in` (Numeric), `Dough_16in` (Numeric), `WalkInTemperature` (Numeric, rangeMax: 56, correctiveActionText: "Product > 56°F — return to refrigeration immediately")
- [ ] Seed is idempotent: running migration twice does not create duplicate templates
- [ ] Integration test: fresh tenant DB has MDOG template after migration

---

### TB-58: Inventory Snapshot Write

**FD:** FD-12  
**Layers:** `DB`, `API`  
**Depends on:** TB-42, TB-57

On completion of an MDOG Task, the `CompleteTaskHandler` writes `InventorySnapshot` records — one per dough-size field — to persist the on-hand counts for historical reference.

**Acceptance Criteria:**
- [ ] `InventorySnapshots` table: `{ id, tenantId, storeId, date, itemKey, onHandCount, submittedByUserId, createdAt }`
- [ ] `CompleteTaskHandler` detects MDOG template (by template ID or category tag) and writes snapshots for all `Numeric` fields
- [ ] `itemKey` matches the field name (e.g., `Dough_10in`)
- [ ] Snapshot written with `date = today` — not datetime, to allow one snapshot per item per day
- [ ] Re-completing an MDOG task (idempotency): snapshot upserted (not duplicated)
- [ ] `GET /stores/{id}/inventory/latest` returns the most recent snapshot per item for the store
- [ ] `GET /stores/{id}/inventory/history?days=7` returns snapshots for the last 7 days

---

### TB-59: MDOG Form Pre-Population

**FD:** FD-12  
**Layers:** `SVC`, `UI`  
**Depends on:** TB-58, TB-37

When a Store Employee opens an MDOG task, the Task Detail form pre-populates numeric fields with the most recent `InventorySnapshot` values for that store.

**Acceptance Criteria:**
- [ ] `GET /tasks/{id}` for an MDOG task: response includes `previousValues: { [fieldName]: number }` from latest snapshots
- [ ] Task Detail form: pre-populates numeric field inputs with `previousValues` (editable — employee can change)
- [ ] Pre-populated values visually distinguished (grey placeholder text or small "yesterday: X" hint)
- [ ] If no previous snapshot exists (first day): fields start empty
- [ ] Unit test: `previousValues` correctly populated from snapshot data

---

### TB-60: 3-Day Need Display + Store Settings

**FD:** FD-12, FD-17  
**Layers:** `DB`, `API`, `SVC`, `UI`  
**Depends on:** TB-59, TB-13

`StoreSettings` stores dough need targets per size. The MDOG Task Detail shows a surplus/deficit indicator for each dough field, comparing on-hand count against the configured Day 2 and Day 3 need targets.

**Acceptance Criteria:**
- [ ] `StoreSettings` table: `{ storeId, tillABase, tillBBase, doughNeedTargets (JSON), timezoneId, overdueGraceMinutes }`
- [ ] `doughNeedTargets` JSON: `{ "Dough_10in": { day2Need: 24, day3Need: 48 }, ... }`
- [ ] `GET /stores/{id}/settings` returns store settings; `PUT /stores/{id}/settings` updates them (Admin only)
- [ ] Task Detail — MDOG task: each dough field shows "Day 2 Need: X | Day 3 Need: Y | Surplus/Deficit: ±Z" computed client-side from entered value vs settings
- [ ] Surplus shown green; deficit shown red
- [ ] Admin Panel → Store Settings page: form to configure dough need targets per size, till base amounts, timezone
- [ ] Integration test: settings updated → next MDOG task detail reflects new targets

---

## Wave 11 — Safe, Till & Deposit Log

> **Goal:** Financial compliance is tracked. Till counts are recorded via task flow. Bank deposit has an immutable audit trail and triggers escalation.  
> **Dependencies:** TB-42 (task completion), TB-50 (Critical FCM), TB-60 (StoreSettings for till base amounts).

---

### TB-61: Till Count System Template Seed

**FD:** FD-13  
**Layers:** `DB`  
**Depends on:** TB-57, TB-60

EF Core seed data: a System-scope "Till Count" Task Template with fields for Till A, Till B, variance note, and manager initials. Range validation uses till base amounts from `StoreSettings`.

**Acceptance Criteria:**
- [ ] Seed: `{ name: "Till Count", scope: "System", category: "Safe" }`
- [ ] Fields: `TillA` (Numeric), `TillB` (Numeric), `VarianceNote` (Text, required if TillA or TillB out of range), `ManagerInitials` (Text, required if variance)
- [ ] Variance logic: `correctiveActionText` on TillA/TillB: "Variance detected — record reason and manager initials"
- [ ] Conditional field requirement (VarianceNote + ManagerInitials only required when TillA/TillB breach range) — enforced server-side in `CompleteTaskHandler` for Safe-category tasks
- [ ] Seed idempotent
- [ ] Integration test: complete till count with out-of-range value without VarianceNote → `400`

---

### TB-62: Record Bank Deposit

**FD:** FD-13  
**Layers:** `DB`, `API`, `SVC`, `UI`  
**Depends on:** TB-60, TB-48

`POST /stores/{id}/deposit-log` creates an immutable deposit record. Available to `store_manager` and above. SignalR broadcast to store group on submission.

**Acceptance Criteria:**
- [ ] `DepositLog` table: `{ id, tenantId, storeId, amount, submittedByManagerId, submittedAt }` — no update or delete endpoints (immutable)
- [ ] `POST /stores/{id}/deposit-log` body: `{ amount: number }` — amount must be positive
- [ ] Only one deposit per store per day allowed — second submission returns `409` with existing record
- [ ] SignalR Standard broadcast to `store-{storeId}` on successful submission
- [ ] Dashboard UI (Store Manager): "Record Deposit" button in the Safe/Deposit section; amount input + submit; confirmation shown with timestamp after submission
- [ ] Submitted deposits shown as a green compliance indicator on the Store Manager dashboard
- [ ] Integration test: submit deposit → record exists; submit again → `409`

---

### TB-63: Deposit Log History View

**FD:** FD-13  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-62

`GET /stores/{id}/deposit-log` returns paginated deposit history. Available to `store_manager` and above.

**Acceptance Criteria:**
- [ ] `GET /stores/{id}/deposit-log` returns deposits paginated (20/page), newest first
- [ ] Query params: `from`, `to` (date range)
- [ ] `GET /stores/{id}/deposit-log/{date}` returns deposit for a specific date (or `404` if none)
- [ ] Admin endpoint: `GET /deposit-log?storeId={id}&from={date}&to={date}` across all stores
- [ ] Dashboard UI: deposit history table (date, amount, submitted by, time); filterable by date range
- [ ] Missing deposit for a past date shown as a red "MISSED" row in the history

---

## Wave 12 — Red Book [RETIRED]

> **Status:** FD-14 retired 2026-06-09. Red Book operational content becomes System-scope templates loaded via Admin JSON import (TB-74). Shift handover becomes a `NotificationOnly` Form Template (TB-73). No TBs in this wave.

---

### TB-64: Red Book — CANCELLED

**FD:** FD-14 (RETIRED)  
**Status:** Cancelled — FD-14 retired. Red Book content is loaded as System-scope templates via TB-74 (Admin JSON Template Import). Shift handover is implemented as a `NotificationOnly` Form Template in FD-18.

**Acceptance Criteria:** N/A — replaced by TB-73 and TB-74.

---

## Wave 13 — Dashboards

> **Goal:** All four dashboard surfaces are complete and data-driven. This is the final assembly wave — it depends on data from Waves 7–11 existing.  
> **Dependencies:** TB-42 (completions), TB-55 (walk sessions), TB-58 (inventory), TB-62 (deposits), TB-47 (overdue).

---

### TB-65: Store Employee Dashboard

**FD:** FD-16a  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-42, TB-35

Field PWA dashboard for Store Employees: personal assigned tasks, claimable store tasks, 7-day personal history, store-wide progress indicator.

**Acceptance Criteria:**
- [ ] "My Tasks" section: assigned tasks for today, grouped by Checklist — links to Task Detail
- [ ] "Open Store Tasks" section: unassigned store-level tasks with Claim button
- [ ] "My History" section: `GET /users/me/completions?days=7` — date, task name, status, completion time
- [ ] "Store Progress" bar: `completedCount / totalCount` for today (excludes Cancelled tasks from denominator)
- [ ] Financial tasks hidden from Store Employee's view entirely (Safe category)
- [ ] All sections update in real-time via existing SignalR connection (TB-36)

---

### TB-66: Store Manager Dashboard

**FD:** FD-16c  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-42, TB-55, TB-62, TB-47

Desktop dashboard for Store Managers. Today's operational snapshot + corrective actions + overdue tasks + walk frequency + deposit status.

**Acceptance Criteria:**
- [ ] `GET /dashboard/store/{id}` returns: `{ completionRate, openCount, overdueCount, activeCorrectiveActionCount, lastWalkAt, depositLoggedToday, unreadRedBookCount }`
- [ ] Dashboard widgets: Completion Rate (donut chart), Open/Overdue counts (stat cards), Active Corrective Actions (list linking to tasks), Walk Frequency ("Last walk: X days ago"), Deposit Status (green/red badge)
- [ ] Multi-store managers: a store selector dropdown; each store has its own data
- [ ] Overdue tasks widget: sortable by elapsed time; click → Task Detail
- [ ] All data is today-scoped; no date picker in V1
- [ ] Data refreshes every 60 seconds via polling (SignalR handles real-time task events; dashboard totals poll)

---

### TB-67: Supervisor Regional Dashboard

**FD:** FD-16d  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-66, TB-50

Desktop dashboard for Supervisors. Regional leaderboard + critical alerts + walk coverage + store drill-down.

**Acceptance Criteria:**
- [ ] `GET /dashboard/region/{id}` returns: `{ stores: [{ storeId, name, completionRate, correctiveActionCount, lastWalkAt, depositLoggedToday, compositeScore }] }`
- [ ] Composite score: `(completionRate * 0.5) + ((1 - corrActionRate) * 0.3) + (walkFrequencyScore * 0.2)` — normalised 0–100
- [ ] Leaderboard table: stores ranked by composite score; colour-coded (green > 80, amber 60–80, red < 60)
- [ ] Critical Alerts panel: stores with missed deposits today; stores with open `CorrectiveActionRaised` tasks
- [ ] Walk Coverage: stores with no Manager Walk in last 7 days — highlighted
- [ ] Clicking a store row → Store Manager Dashboard view for that store (no role switch needed)
- [ ] Integration test: composite score calculation produces expected result

---

### TB-68: Admin System Dashboard

**FD:** FD-16e  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-67, TB-50

Desktop dashboard for Admins. System-wide health — aggregated across all regions.

**Acceptance Criteria:**
- [ ] `GET /dashboard/system` returns: `{ systemCompletionRate, storesWithCriticalEscalations, missedDepositsToday: [storeId...], regionalSummary: [...] }`
- [ ] "System Health" widget: overall completion rate across all stores + tenant today
- [ ] "Missed Deposits Today" panel: mandatory, non-dismissible list of stores with no deposit by 10 AM
- [ ] "Regional Summary" cards: one per region, showing regional completion rate + critical alert count
- [ ] "Active Escalations" panel: Critical FCM events fired in last 24 hours (queried from notification log)
- [ ] Admin sees all data — no region scoping

---

## Wave 14 — Admin Panel (Remaining)

> **Goal:** The Admin Panel is complete with System Template management, Store Settings, and Tenant Settings.  
> **Dependencies:** TB-21 (templates), TB-60 (StoreSettings).

---

### TB-69: System Templates Management UI

**FD:** FD-17  
**Layers:** `UI`  
**Depends on:** TB-21, TB-22, TB-13

Admin-scoped section in the Admin Panel for authoring and managing System-scope Task Templates and Walk Templates. Reuses the Field Builder (TB-20) and Walk Audit Item Builder (TB-52).

**Acceptance Criteria:**
- [ ] Admin Panel sidebar: "System Templates" section (Task Templates + Walk Templates tabs)
- [ ] Task Template list filtered to `scope = System`; "New System Template" creates with `scope` locked to `System`
- [ ] Walk Template list filtered to `scope = System`; same creation pattern
- [ ] Edit + deactivate actions available (same as TB-23, TB-24, TB-51)
- [ ] Non-admin users have no access to this section — `403` on API + route guard on UI

---

### TB-70: Store Settings UI

**FD:** FD-17  
**Layers:** `UI`, `SVC`  
**Depends on:** TB-60, TB-13

Admin Panel section for configuring per-store operational settings: dough need targets, till base amounts, tenant timezone, overdue grace period.

**Acceptance Criteria:**
- [ ] Admin Panel → Store Settings: store selector dropdown; form with all `StoreSettings` fields
- [ ] Dough need targets: input per size (10", 12", 14", 16") for Day 2 and Day 3
- [ ] Till base amounts: Till A base (dropdown: $50 / $75), Till B base (dropdown: $50 / $75)
- [ ] Timezone: dropdown of IANA timezone identifiers
- [ ] Overdue grace minutes: number input (default 30)
- [ ] Save calls `PUT /stores/{id}/settings`; success toast shown

---

### TB-71: Tenant Settings UI

**FD:** FD-17  
**Layers:** `DB`, `API`, `SVC`, `UI`  
**Depends on:** TB-13

Admin Panel section for tenant-level configuration: name, logo, primary contact.

**Acceptance Criteria:**
- [ ] `Tenants` master table extended with: `name`, `logoUrl`, `primaryContactEmail`
- [ ] `GET /tenant/settings` + `PUT /tenant/settings` — admin only
- [ ] Logo upload: uses same Azure Blob SAS URL pattern; stores logo URL in tenant settings
- [ ] Admin Panel → Tenant Settings: form with name, logo upload, contact email
- [ ] Tenant name shown in the dashboard app header/title

---

## Wave 14 — Admin Panel (Remaining) — Additions

### TB-74: Admin JSON Template Import

**FD:** FD-17 (addition)  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-73, TB-21, TB-26, TB-13

`POST /admin/templates/import` accepts a JSON payload of template definitions and bulk-creates them. Admin Panel UI includes a file picker with import preview. Feeds the Bajco Group operational PDF templates after conversion via the `execution/pdf_to_template_json.py` script.

**Acceptance Criteria:**
- [ ] `POST /admin/templates/import` body: `{ templates: [TaskTemplateDto | ChecklistTemplateDto | FormTemplateDto][] }`
- [ ] Each template in the array validated against its type's schema; valid ones created, invalid ones returned with field-level error details
- [ ] Partial import: valid templates created even if some fail; response: `{ created: number, failed: [{ index, errors }] }`
- [ ] Admin Panel sidebar: "Template Import" section; JSON file picker + paste textarea
- [ ] Import preview: shows count by type (Task: N, Checklist: N, Form: N) before confirming
- [ ] Confirmed import calls `POST /admin/templates/import`; success/failure summary shown
- [ ] Integration test: mixed valid/invalid batch → correct partial create + error list returned
- [ ] Internal Layer 3 script `execution/pdf_to_template_json.py`: accepts a PDF path, outputs valid import JSON — documented usage in `directives/bajco_template_onboarding.md`

---

## Wave 15 — Forms

> **Goal:** The Forms domain is fully operational. Form Templates can be authored, and Form Submissions flow through the approval engine end-to-end.  
> **Dependencies:** TB-21 (template pattern), TB-48 (NotificationService), TB-10 (guards), TB-72 (adapter interfaces).

---

### TB-73: Form Template CRUD + Builder UI

**FD:** FD-18  
**Layers:** `DB`, `API`, `SVC`, `UI`  
**Depends on:** TB-21, TB-72

`POST /form-templates` creates a Form Template in the unified `Templates` table (TPH). The Form Template builder extends the shared `FieldBuilderComponent` with a `PropagationTypePicker` and `ApprovalStepsBuilder`. System/Regional/Store scope rules identical to Task Templates.

**Acceptance Criteria:**
- [ ] `Templates` table updated: `templateType` discriminator column (`Task|Checklist|Form`); `typeConfig` NVARCHAR(MAX)/JSONB column added
- [ ] EF Core TPH mapping: `HasDiscriminator<string>("TemplateType")` with `FormTemplate`, `TaskTemplate`, `ChecklistTemplate` entity types
- [ ] `FormTemplate` entity: `typeConfig` deserialized into `FormTemplateConfig { PropagationType, ApprovalSteps[] }` in the domain layer
- [ ] `POST /form-templates` validates: `propagationType` is `Sequential | Parallel | NotificationOnly`; `approvalSteps` non-empty; roles must be valid role values
- [ ] Scope/role enforcement identical to `POST /templates` (TB-21)
- [ ] `GET /form-templates` uses same visibility scoping as `GET /templates`; filterable by `propagationType`, `scope`, `isActive`
- [ ] `PUT /form-templates/{id}` + `POST /form-templates/{id}/deactivate` (blocked if active FormSubmissions exist)
- [ ] `libs/ui/template-builder`: new `PropagationTypePicker` component (Sequential / Parallel / NotificationOnly radio with description)
- [ ] `libs/ui/template-builder`: new `ApprovalStepsBuilder` component — role dropdown + CDK DragDrop reorder (order only relevant for Sequential; shown but disabled for Parallel/NotificationOnly)
- [ ] Form Template builder UI in `dashboard` Templates section; simplified view in `field-pwa`
- [ ] Form Templates appear in `TemplatePicker` dropdown (shared across Create flow and Templates module)
- [ ] Integration tests: Sequential Form Template creation; Parallel; NotificationOnly; scope/role violations

---

### TB-75: Form Submission — Draft + Submit

**FD:** FD-19  
**Layers:** `DB`, `API`, `SVC`, `UI`  
**Depends on:** TB-73, TB-48

Creates the `FormSubmissions` and `FormSubmissionApprovalSteps` tables. `POST /form-submissions` creates a Draft. `POST /form-submissions/{id}/submit` advances to `PendingApproval[step 1]` (or `Recorded` for NotificationOnly). Notifies step 1 reviewers. Unified Create flow entry point wired for Forms.

**Acceptance Criteria:**
- [ ] `FormSubmissions` table: `{ id, tenantId, formTemplateId (nullable FK), storeId, submittedByUserId, status, currentStepOrder (nullable), fieldValues (JSON), createdAt, submittedAt (nullable), resolvedAt (nullable) }`
- [ ] `FormSubmissionApprovalSteps` table: `{ id, submissionId, stepOrder, role, actionByUserId (nullable), action (Pending|Approved|Rejected|Returned), comments (nullable), actionAt (nullable) }`
- [ ] `POST /form-submissions` creates submission in `Draft` state; accepts `formTemplateId` (optional) and `fieldValues` (JSON)
- [ ] `POST /form-submissions/{id}/submit`: required fields validated; status → `Submitted` → `PendingApproval` (step 1) with step rows inserted; FCM Standard + SignalR notification to all users with step 1 role in relevant store/region
- [ ] `NotificationOnly` submit: status → `Recorded` directly; all ApprovalStep role users notified
- [ ] `GET /form-submissions/my-submissions` returns submissions created by authenticated user, newest first; includes status and `formTemplateName`
- [ ] Unified Create flow: home screen "Create" button → type selector (Task | Checklist | Form) → `TemplatePicker` dropdown (optional) → form fields → "Save as Template" toggle OR "Submit" button
- [ ] "Save as Template" + submit: saves template first → creates submission from template
- [ ] Field PWA and dashboard both expose the Create flow
- [ ] Integration tests: Draft → Submit → PendingApproval; NotificationOnly Draft → Submit → Recorded; required field missing → 400

---

### TB-76: Form Submission — Reviewer Actions

**FD:** FD-19  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-75

Implements the three reviewer actions: Approve (advances step or reaches terminal Approved), Reject (terminal with reason), Return (rework loop with comments). Sequential and Parallel resolution logic.

**Acceptance Criteria:**
- [ ] `POST /form-submissions/{id}/approve`: requires authenticated user's role to match current `PendingApproval` step role and be scoped to the submission's store/region; Sequential → advances to step N+1 or terminal `Approved`; Parallel → resolves submission, auto-closes remaining steps
- [ ] `POST /form-submissions/{id}/reject` body: `{ reason: string }`; reason required; transitions to `Rejected` (terminal); `FormSubmissionApprovalSteps` action recorded; notifies submitter via FCM Standard
- [ ] `POST /form-submissions/{id}/return` body: `{ comments: string }`; transitions to `Returned`; reviewer comments stored on `FormSubmissionApprovalSteps` record; notifies submitter with comments via FCM Standard; `currentStepOrder` retained (not reset to 0)
- [ ] Re-submit of a `Returned` submission: `POST /form-submissions/{id}/submit` re-enters Sequential at the previously returning step (not step 1); Parallel re-notifies all step reviewers
- [ ] Parallel first-action-wins: when one reviewer acts, submission resolves; remaining `FormSubmissionApprovalSteps` records for that submission set to auto-closed
- [ ] `GET /form-submissions/pending-review`: returns submissions where current `PendingApproval` step role matches authenticated user's role and store/region scope
- [ ] Dashboard UI: "Pending Review" queue with Approve / Return / Reject action buttons; Return and Reject require a comments/reason input modal
- [ ] Field PWA UI: same queue for `store_manager` role
- [ ] SignalR broadcast to store group on each action
- [ ] Integration tests: full Sequential approve chain; Reject terminal; Return → revise → re-submit enters at returning step; Parallel first action wins

---

### TB-77: Form Submission Piles + History UI

**FD:** FD-19  
**Layers:** `API`, `SVC`, `UI`  
**Depends on:** TB-76

Complete the submitter-facing UI: "My Submissions" list with state piles (Draft, Submitted, Returned, Rejected, Approved, Recorded). Submission detail view showing field values, approval history, and reviewer comments.

**Acceptance Criteria:**
- [ ] `GET /form-submissions/my-submissions` returns all submissions by authenticated user; includes `status`, `formTemplateName`, `submittedAt`, `resolvedAt`
- [ ] `GET /form-submissions/{id}` returns submission detail: field values, `FormSubmissionApprovalSteps` with actions and comments, current status
- [ ] UI: "My Submissions" section in both apps — tabbed or filtered by state: All / Draft / Pending / Returned / Rejected / Approved
- [ ] Returned submissions show reviewer comments prominently; "Revise & Resubmit" button opens pre-populated form
- [ ] Rejected submissions show rejection reason; no further action available
- [ ] Approved/Recorded submissions shown as read-only with full approval trail
- [ ] Draft submissions show "Continue" button to resume filling out
- [ ] Supervisor and Admin: `GET /form-submissions?storeId={id}` and `GET /form-submissions?regionId={id}` for management views

---

### TB-78: Form Submission Notifications

**FD:** FD-19  
**Layers:** `API`, `SVC`  
**Depends on:** TB-76, TB-48

Wires all Form Submission events through `NotificationService`. All form notifications are Standard priority.

**Acceptance Criteria:**
- [ ] `FormSubmitted` event → FCM Standard to all users with current step 1 role in store/region
- [ ] `FormReturned` event → FCM Standard to submitter with reviewer name and comments in notification body
- [ ] `FormRejected` event → FCM Standard to submitter with rejection reason
- [ ] `FormApproved` event (final step or parallel resolved) → FCM Standard to submitter
- [ ] `FormRecorded` event (NotificationOnly) → FCM Standard to all ApprovalStep role users
- [ ] All events also broadcast via SignalR to `store-{storeId}` group
- [ ] FCM payload for all form events: `{ title, body, data: { submissionId, type: "form_*" } }` — clicking opens submission detail in the app
- [ ] No form events use Critical priority in V1
- [ ] Unit tests: each event type dispatches to the correct channel and recipients

---

### TB-79: Unified Create Flow — Home Screen Entry Point

**FD:** FD-18, FD-19 (UI entry point)  
**Layers:** `UI`, `SVC`  
**Depends on:** TB-34, TB-73, TB-75

Adds the universal "Create" button to both `field-pwa` and `dashboard`. A type selector (Task | Checklist | Form) routes to the appropriate form. A shared `TemplatePicker` dropdown pre-fills from any visible template. A "Save as Template" toggle available on all types.

**Acceptance Criteria:**
- [ ] Persistent "Create" button in both apps' navigation (FAB on PWA, button in dashboard header)
- [ ] Type selector: Task | Checklist | Form — selection determines which sub-form renders
- [ ] `TemplatePicker` dropdown (in `libs/ui/template-builder`): filtered to the selected type; shows templates visible to the current user (scope-filtered); selecting a template pre-populates the form
- [ ] "Save as Template" toggle on all three types: if enabled, calls the relevant template POST endpoint before execution; if template save fails, execution is blocked
- [ ] "Execute Only" path: creates Task Instance (TB-34), Checklist Instance (TB-26 pattern), or Form Submission Draft (TB-75) directly without saving a template
- [ ] Type selector, TemplatePicker, and SaveToggle extracted to `libs/ui/template-builder/create-flow/` as standalone components with no API calls
- [ ] Unit tests for all three type paths with mocked sub-forms

---

## Summary Table

| Wave | TBs | Can start when... |
|------|-----|-------------------|
| 0 — Infrastructure | TB-01 to TB-06, TB-72 | Immediately |
| 1 — Authentication | TB-07 to TB-12 | Wave 0 done |
| 2 — Org Structure | TB-13 to TB-19 | Wave 1 done |
| 3 — Task Templates | TB-20 to TB-25 | Wave 2 done |
| 4 — Checklists | TB-26 to TB-28 | TB-21 done |
| 5 — Recurring Assignments | TB-29 to TB-34 | TB-21, TB-26, TB-15 done |
| 6 — Task Board | TB-35 to TB-41 | TB-33, TB-34, TB-06, TB-10 done |
| 7 — Task Completion | TB-42 to TB-47 | TB-35, TB-37 done |
| 8 — Notifications | TB-48 to TB-50 | TB-42, TB-06 done |
| 9 — Manager Walk | TB-51 to TB-56 | TB-21, TB-42, TB-48, TB-38 done |
| 10 — MDOG & Inventory | TB-57 to TB-60 | TB-42 done |
| 11 — Safe & Deposit | TB-61 to TB-63 | TB-42, TB-50, TB-60 done |
| 12 — Red Book | TB-64 (CANCELLED) | FD-14 retired — no implementation |
| 13 — Dashboards | TB-65 to TB-68 | TB-42, TB-55, TB-58, TB-62 done |
| 14 — Admin Panel (rest) | TB-69 to TB-71, TB-74 | TB-21, TB-60, TB-73 done |
| 15a — Form Templates | TB-73 | TB-21, TB-72 done |
| 15b — Form Submissions | TB-75 to TB-79 | TB-73, TB-48 done |

**Total: 79 Tracer Bullets (71 original + 8 new; TB-64 cancelled)**

---

*Every TB is a vertical slice. No horizontal layers. No partial implementations. If a TB can't be demonstrated in a browser or via an API call at the end of the session, it isn't done.*
