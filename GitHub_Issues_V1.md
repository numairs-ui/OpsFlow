# OpsFlow V1 — GitHub Issues (Tracer Bullet Format)

**Generated:** 2026-06-10 (Architecture Update — Grilling Phase 2)  
**Source:** `Tracer_Bullets_V1.md` + `OpsFlow_PRD_V1.md`  
**Total issues:** 79 (71 original + 8 new; TB-64 cancelled)  
**Usage:** Each block below is one GitHub issue. Create via `gh issue create` or paste into GitHub UI.  
**Suggested labels to create first:**

```
wave-0, wave-1, wave-2, wave-3, wave-4, wave-5, wave-6, wave-7,
wave-8, wave-9, wave-10, wave-11, wave-12, wave-13, wave-14, wave-15,
layer:db, layer:api, layer:svc, layer:ui, layer:infra, layer:job,
fd-01 ... fd-19,
status:blocked, status:ready, status:in-progress, status:done, status:cancelled
```

---

## WAVE 0 — Workspace & Infrastructure

---

### TB-01: Nx Monorepo Workspace Scaffold

**Labels:** `wave-0` `layer:infra` `fd-01`  
**Milestone:** Wave 0  
**Depends on:** —  
**PRD ref:** FD-01

#### Description
Set up the Nx monorepo with two Angular 17 apps (`field-pwa`, `dashboard`) and three shared library categories. Configure Nx boundary rules so apps can only consume from `libs/` — never from each other.

#### Layers Touched
- [x] INFRA

#### Acceptance Criteria
- [ ] `field-pwa` and `dashboard` Angular 17 apps created with standalone component config and strict TypeScript
- [ ] `libs/data-access/core`, `libs/ui/core`, `libs/util/models` exist as initial placeholder libs
- [ ] `nx graph` shows zero boundary violations
- [ ] `nx build field-pwa` and `nx build dashboard` both succeed
- [ ] `enforce-module-boundaries` ESLint rule configured and passing
- [ ] `tsconfig` strict mode enabled across all projects

---

### TB-02: .NET 9 VSA Solution Scaffold

**Labels:** `wave-0` `layer:infra` `fd-01`  
**Milestone:** Wave 0  
**Depends on:** —  
**PRD ref:** FD-01

#### Description
Create the .NET 9 solution with Vertical Slice Architecture folder structure (`Features/{Domain}/{Action}/`). Install MediatR, FluentValidation, EF Core 9, Quartz.NET. Prove the pattern with a working `GET /health` slice.

#### Layers Touched
- [x] INFRA
- [x] API

#### Acceptance Criteria
- [ ] Solution structure: `OpsFlow.Api/`, `OpsFlow.Domain/`, `OpsFlow.Infrastructure/`, `OpsFlow.Tests.Unit/`, `OpsFlow.Tests.Integration/`
- [ ] `Features/Health/GetStatus/` slice: command + handler + endpoint → `GET /health` returns `{ status: "ok" }`
- [ ] MediatR pipeline: logging + validation behaviours wired
- [ ] FluentValidation integrated; invalid requests return `400` with structured error details
- [ ] `dotnet test` passes on both test projects
- [ ] `launchSettings.json` configured for local dev

---

### TB-03: Database-Per-Tenant EF Core Setup

**Labels:** `wave-0` `layer:db` `layer:api` `fd-01`  
**Milestone:** Wave 0  
**Depends on:** `TB-02`  
**PRD ref:** FD-01

#### Description
Implement the multi-tenant database resolution pattern. `TenantDbContextFactory` reads `tenantId` from the JWT and resolves the correct connection string. Master DB stores `Tenants`. Migration orchestrator CLI applies EF Core migrations to all tenant databases.

#### Layers Touched
- [x] DB
- [x] API

#### Acceptance Criteria
- [ ] `Tenants` master DB table: `{ id, name, connectionString, createdAt, isActive }`
- [ ] `TenantDbContextFactory` resolves connection string from JWT `tenantId` claim on every request
- [ ] Requests without a valid `tenantId` return `401`
- [ ] `dotnet run --migrate-all` CLI command applies pending migrations to every active tenant DB
- [ ] Seed script creates one "dev" tenant DB locally with all migrations applied
- [ ] Integration test: two tenant DBs created; query on tenant A does not return data from tenant B

---

### TB-04: CI/CD Pipeline

**Labels:** `wave-0` `layer:infra` `fd-01`  
**Milestone:** Wave 0  
**Depends on:** `TB-01` `TB-02`  
**PRD ref:** FD-01

#### Description
GitHub Actions workflow: PR → run all tests. Merge to `main` → deploy .NET API to Azure App Service and Angular apps to Azure Static Web Apps. Environments: `dev` and `prod`.

#### Layers Touched
- [x] INFRA

#### Acceptance Criteria
- [ ] PR workflow: `dotnet test` (unit + integration) and `nx affected --target=test` both run and pass
- [ ] PR workflow: `nx build field-pwa` and `nx build dashboard` succeed
- [ ] Merge-to-main: deploys .NET API to Azure App Service `dev` slot
- [ ] Merge-to-main: deploys `field-pwa` and `dashboard` to Azure Static Web Apps
- [ ] All secrets stored in GitHub Actions secrets — never in code
- [ ] Pipeline status badge added to `README.md`

---

### TB-05: Azure Infrastructure Provisioning

**Labels:** `wave-0` `layer:infra` `fd-01`  
**Milestone:** Wave 0  
**Depends on:** `TB-04`  
**PRD ref:** FD-01

#### Description
Provision all production Azure resources in a single Resource Group: App Service, Azure SQL, Static Web Apps (×2), Blob Storage, Azure SignalR Service.

#### Layers Touched
- [x] INFRA

#### Acceptance Criteria
- [ ] All resources exist in `opsflow-prod` Resource Group
- [ ] App Service runs `GET /health` from TB-02 publicly
- [ ] Azure SQL master DB accessible from App Service
- [ ] Blob Storage `task-photos` container created with private access
- [ ] Azure SignalR Service in `Serverless` mode connected to App Service
- [ ] `.env.example` documents all required environment variables
- [ ] No connection strings committed to source control

---

### TB-06: Angular PWA + FCM Setup

**Labels:** `wave-0` `layer:infra` `layer:svc` `fd-15`  
**Milestone:** Wave 0  
**Depends on:** `TB-01`  
**PRD ref:** FD-15

#### Description
Configure `field-pwa` as an installable PWA with `@angular/pwa` Service Worker. Integrate Firebase Cloud Messaging. Create `libs/data-access/notifications` with a `NotificationTokenService` that registers the FCM device token after user grants permission.

#### Layers Touched
- [x] INFRA
- [x] SVC

#### Acceptance Criteria
- [ ] `field-pwa` passes Chrome Lighthouse PWA audit (installable, service worker registered)
- [ ] FCM permission prompt appears on first load
- [ ] On permission grant: FCM token retrieved and stored in a `signal<string | null>`
- [ ] Service Worker precaches app shell; `field-pwa` loads from cache with no network
- [ ] `firebase-messaging-sw.js` present and registered
- [ ] Firebase project configured; credentials in environment files only

---

## WAVE 1 — Authentication

---

### TB-07: Login API — JWT Minting

**Labels:** `wave-1` `layer:db` `layer:api` `fd-02`  
**Milestone:** Wave 1  
**Depends on:** `TB-03`  
**PRD ref:** FD-02

#### Description
`POST /auth/login` validates credentials against ASP.NET Core Identity in the tenant DB and returns a short-lived JWT (access token) + refresh token. JWT payload must include `sub`, `tenantId`, `role`, `storeId`, `regionId`.

#### Layers Touched
- [x] DB
- [x] API

#### Acceptance Criteria
- [ ] `POST /auth/login` with valid credentials → `200` with `{ accessToken, expiresIn }`; refresh token as `HttpOnly` cookie
- [ ] `POST /auth/login` with invalid credentials → `401`
- [ ] Access token expires in 15 minutes (verified with mocked clock in integration test)
- [ ] `RefreshTokens` table: `{ userId, tokenHash, expiresAt, usedAt (nullable), createdAt }`
- [ ] JWT payload contains all required claims: `sub`, `tenantId`, `role`, `storeId` (nullable), `regionId` (nullable)
- [ ] Unit tests cover: wrong password, deactivated user, unknown email

---

### TB-08: Token Refresh + Logout API

**Labels:** `wave-1` `layer:api` `fd-02`  
**Milestone:** Wave 1  
**Depends on:** `TB-07`  
**PRD ref:** FD-02

#### Description
`POST /auth/refresh` reads the `HttpOnly` refresh token cookie, validates it, and returns a new access token with a rotated refresh token. `POST /auth/logout` invalidates the refresh token.

#### Layers Touched
- [x] API

#### Acceptance Criteria
- [ ] `POST /auth/refresh` with valid cookie → new access token + rotated refresh token
- [ ] `POST /auth/refresh` with expired or missing cookie → `401`
- [ ] Reused refresh token → `401` (rotation invalidates previous)
- [ ] `POST /auth/logout` marks token as used; subsequent refresh returns `401`
- [ ] Integration test covers full: login → refresh → logout → refresh fails

---

### TB-09: Angular AuthService + HTTP Interceptor

**Labels:** `wave-1` `layer:svc` `fd-02`  
**Milestone:** Wave 1  
**Depends on:** `TB-07` `TB-08`  
**PRD ref:** FD-02

#### Description
Create `libs/data-access/auth` with `AuthService`. Access token stored in a Signal (memory only). `AuthInterceptor` attaches Bearer token to every request; handles `401` with a single refresh retry.

#### Layers Touched
- [x] SVC

#### Acceptance Criteria
- [ ] `AuthService.login()` calls `POST /auth/login`, stores token in Signal, navigates to home route
- [ ] `AuthService.logout()` calls `POST /auth/logout`, clears Signal, navigates to `/login`
- [ ] `AuthService.refresh()` calls `POST /auth/refresh`; on failure calls `logout()`
- [ ] `AuthInterceptor` attaches `Authorization: Bearer <token>` to all requests except `/auth/*`
- [ ] `401` response → one refresh retry → on second `401` → logout
- [ ] `currentUser = computed(() => decodeJwt(accessToken()))` exposes role, storeId, regionId
- [ ] Unit tests cover all branches with mocked HttpClient

---

### TB-10: Angular Route Guards

**Labels:** `wave-1` `layer:svc` `fd-02`  
**Milestone:** Wave 1  
**Depends on:** `TB-09`  
**PRD ref:** FD-02

#### Description
Create `AuthGuard` and `RoleGuard` in `libs/util/guards`. Guards use `AuthService.currentUser` Signal — no HTTP calls.

#### Layers Touched
- [x] SVC

#### Acceptance Criteria
- [ ] `AuthGuard`: unauthenticated → `/login`; authenticated → pass
- [ ] `RoleGuard`: insufficient role → `/unauthorized`; sufficient → pass
- [ ] `/unauthorized` route exists in both apps with message + back link
- [ ] Unit tests cover all guard branches with mocked `AuthService`

---

### TB-11: Field PWA Login Page

**Labels:** `wave-1` `layer:ui` `fd-02`  
**Milestone:** Wave 1  
**Depends on:** `TB-09` `TB-10`  
**PRD ref:** FD-02

#### Description
Mobile-optimised login page for `field-pwa`. Calls `AuthService.login()`. Registers FCM token after successful login. Navigates to `/tasks` on success.

#### Layers Touched
- [x] UI

#### Acceptance Criteria
- [ ] Email + password form with client-side validation (email format, non-empty password)
- [ ] Loading spinner during request; form disabled
- [ ] `401` → "Invalid email or password" error message
- [ ] Successful login → navigate to `/tasks`; FCM token registration called
- [ ] 44×44px minimum touch targets; passes WCAG 2.1 AA contrast
- [ ] Renders correctly at 375px and 390px viewport widths

---

### TB-12: Dashboard Login Page

**Labels:** `wave-1` `layer:ui` `fd-02`  
**Milestone:** Wave 1  
**Depends on:** `TB-09` `TB-10`  
**PRD ref:** FD-02

#### Description
Desktop login page for `dashboard`. Role-based post-login redirect: `admin` → `/admin`, `supervisor` → `/supervisor`, `store_manager` → `/manager`. Shared login form component extracted to `libs/ui/auth`.

#### Layers Touched
- [x] UI

#### Acceptance Criteria
- [ ] Same functional behaviour as TB-11
- [ ] Role-based redirect after login
- [ ] Desktop layout: centred card, 400px max-width
- [ ] Login form component in `libs/ui/auth` (shared with TB-11 if not already done)

---

## WAVE 2 — Organisation Structure

---

### TB-13: Admin Panel Shell + Navigation

**Labels:** `wave-2` `layer:ui` `fd-17`  
**Milestone:** Wave 2  
**Depends on:** `TB-10` `TB-12`  
**PRD ref:** FD-17

#### Description
Scaffold the admin section of `dashboard`. Persistent sidebar with: Users, Stores, Regions, System Templates, Store Settings, Tenant Settings. All routes guarded by `RoleGuard({ role: 'admin' })`.

#### Layers Touched
- [x] UI

#### Acceptance Criteria
- [ ] `/admin` route group guarded — non-admin redirects to `/unauthorized`
- [ ] Sidebar renders all navigation sections with active state highlighting
- [ ] Breadcrumb component updates on route change
- [ ] Responsive down to 1024px viewport width
- [ ] Unauthenticated access to `/admin` redirects to `/login`

---

### TB-14: Region CRUD

**Labels:** `wave-2` `layer:db` `layer:api` `layer:svc` `layer:ui` `fd-03`  
**Milestone:** Wave 2  
**Depends on:** `TB-03` `TB-13`  
**PRD ref:** FD-03

#### Description
`Regions` table. Full CRUD (list, create, edit, deactivate). Admin UI inside Admin Panel.

#### Layers Touched
- [x] DB
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `Regions` table: `{ id, tenantId, name, description, isActive, createdAt }`
- [ ] `GET /regions` — active filter optional
- [ ] `POST /regions` — name unique within tenant; `store_manager`/`supervisor` → `403`
- [ ] `PUT /regions/{id}` — updates name/description
- [ ] `POST /regions/{id}/deactivate` — soft-delete; deactivated regions excluded from dropdowns
- [ ] Admin UI: list table with inline deactivate; "New Region" slide-over form
- [ ] Integration tests cover all endpoints + role enforcement

---

### TB-15: Store CRUD

**Labels:** `wave-2` `layer:db` `layer:api` `layer:svc` `layer:ui` `fd-03`  
**Milestone:** Wave 2  
**Depends on:** `TB-14`  
**PRD ref:** FD-03

#### Description
`Stores` table. Each store belongs to a Region. Full CRUD. Deactivating a store deactivates all its active Recurring Assignments.

#### Layers Touched
- [x] DB
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `Stores` table: `{ id, tenantId, regionId, name, address, isActive, createdAt }`
- [ ] `GET /stores` — filterable by `regionId`, `isActive`
- [ ] `POST /stores` — requires valid `regionId`
- [ ] `PUT /stores/{id}` — updates name, address, regionId
- [ ] `POST /stores/{id}/deactivate` — deactivates store + all active RecurringAssignments for that store
- [ ] `GET /regions/{id}/stores`
- [ ] Admin UI: list table grouped by region; "New Store" form with Region dropdown
- [ ] Integration tests cover all endpoints

---

### TB-16: User CRUD + Role Assignment

**Labels:** `wave-2` `layer:db` `layer:api` `layer:svc` `layer:ui` `fd-03`  
**Milestone:** Wave 2  
**Depends on:** `TB-15`  
**PRD ref:** FD-03

#### Description
Create/edit/list user accounts via Admin Panel. Role-specific validation: `store_employee` requires `storeId`, `supervisor` requires `regionId`. First-login password change enforced.

#### Layers Touched
- [x] DB
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `POST /users` — creates user via ASP.NET Core Identity; role-specific field validation enforced
- [ ] `GET /users` — filterable by role, storeId, isActive; paginated
- [ ] `PUT /users/{id}` — updates name, email, role, store/region assignment
- [ ] `GET /users/{id}` — returns user detail
- [ ] `MustChangePassword` flag set on creation; first-login change flow implemented
- [ ] Admin UI: user table with search/filter; "New User" form with role-conditional fields
- [ ] Integration tests cover role-specific validation edge cases

---

### TB-17: UserStoreAssignments — Multi-Store Manager

**Labels:** `wave-2` `layer:db` `layer:api` `layer:svc` `layer:ui` `fd-03`  
**Milestone:** Wave 2  
**Depends on:** `TB-16`  
**PRD ref:** FD-03

#### Description
Store Managers can be assigned to multiple stores via join table. Admin UI shows an assignment panel on the Store Manager user detail page.

#### Layers Touched
- [x] DB
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `UserStoreAssignments` table: `{ userId, storeId, assignedAt, assignedByAdminId }`
- [ ] `POST /users/{id}/store-assignments` — adds store; requires user to have `store_manager` role
- [ ] `DELETE /users/{id}/store-assignments/{storeId}` — removes assignment
- [ ] `GET /users/{id}/store-assignments` — returns all assigned stores
- [ ] Admin UI: multi-select store assignment panel on Store Manager user detail
- [ ] Integration test: Store Manager can access data for all assigned stores; blocked from non-assigned stores

---

### TB-18: User Deactivation

**Labels:** `wave-2` `layer:api` `layer:ui` `fd-03`  
**Milestone:** Wave 2  
**Depends on:** `TB-16`  
**PRD ref:** FD-03

#### Description
Admins deactivate/reactivate user accounts. Deactivated users cannot log in; their historical records are retained.

#### Layers Touched
- [x] API
- [x] UI

#### Acceptance Criteria
- [ ] `POST /users/{id}/deactivate` — sets `IsActive = false`
- [ ] Deactivated user login → `401` "Account deactivated"
- [ ] Refresh tokens for deactivated users invalidated immediately
- [ ] `POST /users/{id}/reactivate` — re-enables account
- [ ] Admin UI: deactivate/reactivate toggle with confirmation dialog
- [ ] Historical task completions by deactivated user remain intact

---

### TB-19: Store Employee Roster View

**Labels:** `wave-2` `layer:api` `layer:svc` `layer:ui` `fd-03`  
**Milestone:** Wave 2  
**Depends on:** `TB-16` `TB-17`  
**PRD ref:** FD-03

#### Description
Store Managers can view the active employee roster for their assigned store(s). Read-only dashboard view.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `GET /stores/{id}/employees` — returns active `store_employee` users for the store
- [ ] Store Manager querying a store they are not assigned to → `403`
- [ ] Supervisor can view rosters for any store in their region
- [ ] Dashboard UI: roster list with name, email, assignment date; accessible from store detail

---

## WAVE 3 — Task Template System

---

### TB-20: Dynamic Field Builder Component

**Labels:** `wave-3` `layer:ui` `fd-04`  
**Milestone:** Wave 3  
**Depends on:** `TB-01`  
**PRD ref:** FD-04

#### Description
`libs/ui/field-builder` — reusable Angular component for authoring the dynamic field array of a Task Template. Supports all 5 field types: `Numeric`, `Boolean`, `Text`, `Photo`, `Checklist`. Fields can be added, reordered (CDK DragDrop), and removed.

#### Layers Touched
- [x] UI

#### Acceptance Criteria
- [ ] All 5 field types selectable; each renders a type-specific config form
- [ ] `Numeric`: label, required, rangeMin (optional), rangeMax (optional), correctiveActionText (optional)
- [ ] `Boolean`: label, required, correctiveActionText (fires on "No", optional)
- [ ] `Text`: label, required flag
- [ ] `Photo`: label, required flag
- [ ] `Checklist`: label; ordered sub-items each with label + required flag; sub-items addable/reorderable/removable
- [ ] CDK DragDrop field reordering works on both desktop and touch
- [ ] Emits `FieldsChange: TemplateField[]` output — no direct API calls
- [ ] No `any` types; strict TypeScript throughout

---

### TB-21: Create Task Template

**Labels:** `wave-3` `layer:db` `layer:api` `layer:svc` `layer:ui` `fd-04`  
**Milestone:** Wave 3  
**Depends on:** `TB-15` `TB-20`  
**PRD ref:** FD-04

#### Description
`POST /templates` creates a Task Template. Desktop authoring UI uses TB-20 Field Builder. Scope/role rules enforced server-side.

#### Layers Touched
- [x] DB
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `TaskTemplates` table: `{ id, tenantId, name, description, category, scope, regionId (nullable), storeId (nullable), fields (JSON), isActive, createdByUserId, createdAt }`
- [ ] Scope/role enforcement: System → `admin` only; Regional → `supervisor`+; Store → `store_manager`+
- [ ] Invalid `fields` JSON structure → `400`
- [ ] Desktop UI: "New Template" form — name, category, scope selector (role-filtered), Field Builder
- [ ] On save: navigate to template list
- [ ] Integration tests: scope-role combinations (System by store_manager → 403; Regional by store_manager → 403)

---

### TB-22: List + Filter Task Templates

**Labels:** `wave-3` `layer:api` `layer:svc` `layer:ui` `fd-04`  
**Milestone:** Wave 3  
**Depends on:** `TB-21`  
**PRD ref:** FD-04

#### Description
`GET /templates` returns templates visible to the current user based on their role and scope. Filterable and searchable.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] Visibility: `store_employee` sees System + Region + own Store; `supervisor` sees System + own Region; `admin` sees all
- [ ] Query params: `scope`, `category`, `isActive`, `search` (name contains)
- [ ] Paginated: 20 per page
- [ ] Desktop UI: searchable filterable table with scope badge, category, active status indicator

---

### TB-23: Edit Task Template

**Labels:** `wave-3` `layer:api` `layer:svc` `layer:ui` `fd-04`  
**Milestone:** Wave 3  
**Depends on:** `TB-21` `TB-22`  
**PRD ref:** FD-04

#### Description
`PUT /templates/{id}` updates a template. System templates: admin-only edit. Templates with active Recurring Assignments: `fields` changes blocked.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] Role-scope enforcement: same rules as creation
- [ ] Templates with active RecurringAssignments: `fields` change → `409` with clear error
- [ ] System templates: only `admin` can edit
- [ ] Desktop UI: edit form pre-populated; Field Builder shows existing fields
- [ ] Success → optimistic UI update + toast

---

### TB-24: Deactivate Task Template

**Labels:** `wave-3` `layer:api` `layer:ui` `fd-04`  
**Milestone:** Wave 3  
**Depends on:** `TB-23`  
**PRD ref:** FD-04

#### Description
Soft-delete and restore Task Templates. Blocked if active Recurring Assignments reference the template.

#### Layers Touched
- [x] API
- [x] UI

#### Acceptance Criteria
- [ ] `POST /templates/{id}/deactivate` → `409` if active RecurringAssignments exist
- [ ] `POST /templates/{id}/activate` — re-enables
- [ ] Deactivated templates excluded from pickers; visible in list with "Show inactive" toggle
- [ ] Admin UI: toggle with confirmation dialog

---

### TB-25: Field PWA Quick Template Creation

**Labels:** `wave-3` `layer:svc` `layer:ui` `fd-04`  
**Milestone:** Wave 3  
**Depends on:** `TB-21`  
**PRD ref:** FD-04

#### Description
Simplified "quick template" form on `field-pwa` for Store Managers. Store-scope only. Max 5 fields. Numeric, Boolean, Text field types only (no Checklist or Photo on PWA form).

#### Layers Touched
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] Only accessible to `store_manager`+ on `field-pwa`
- [ ] Scope auto-set to `Store`; `storeId` auto-populated from JWT
- [ ] Field types limited to Numeric, Boolean, Text; max 5 fields enforced with UI indicator
- [ ] Uses same `POST /templates` endpoint as desktop
- [ ] On save: navigates to ad hoc task creation with new template pre-selected (links to TB-34)

---

## WAVE 4 — Checklist System

---

### TB-26: Create Checklist + Manage Items

**Labels:** `wave-4` `layer:db` `layer:api` `layer:svc` `layer:ui` `fd-05`  
**Milestone:** Wave 4  
**Depends on:** `TB-21`  
**PRD ref:** FD-05

#### Description
`POST /checklists` creates a Checklist with an ordered list of Task Template references. Desktop UI includes a template picker and drag-to-reorder item list.

#### Layers Touched
- [x] DB
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `Checklists` table: `{ id, tenantId, name, description, scope, regionId (nullable), storeId (nullable), isActive, createdByUserId }`
- [ ] `ChecklistTemplateItems` table: `{ checklistId, templateId, order }` (composite PK)
- [ ] Same scope/role rules as Task Templates
- [ ] `PUT /checklists/{id}/items` — full replace of ordered item list
- [ ] Templates in checklist must be visible to creator (scope validation)
- [ ] Desktop UI: checklist builder with template picker + CDK DragDrop reorder
- [ ] Integration tests: scope-role validation

---

### TB-27: List Checklists

**Labels:** `wave-4` `layer:api` `layer:svc` `layer:ui` `fd-05`  
**Milestone:** Wave 4  
**Depends on:** `TB-26`  
**PRD ref:** FD-05

#### Description
`GET /checklists` with same visibility rules as templates. Each result includes item count and first 3 template names.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] Same visibility scoping as `GET /templates`
- [ ] Response includes `itemCount` and preview of first 3 template names
- [ ] Filterable by scope, isActive; searchable by name
- [ ] Desktop UI: list table with scope badge, item count

---

### TB-28: Deactivate Checklist

**Labels:** `wave-4` `layer:api` `layer:ui` `fd-05`  
**Milestone:** Wave 4  
**Depends on:** `TB-27`  
**PRD ref:** FD-05

#### Description
Soft-delete and restore Checklists. Blocked if active Recurring Assignments reference the checklist.

#### Layers Touched
- [x] API
- [x] UI

#### Acceptance Criteria
- [ ] `POST /checklists/{id}/deactivate` → `409` if active RecurringAssignments exist
- [ ] `POST /checklists/{id}/activate` — re-enables
- [ ] Admin UI: toggle with confirmation dialog

---

## WAVE 5 — Recurring Assignments & Ad Hoc Tasks

---

### TB-29: Cron Picker Component

**Labels:** `wave-5` `layer:ui` `fd-06`  
**Milestone:** Wave 5  
**Depends on:** `TB-01`  
**PRD ref:** FD-06

#### Description
`libs/ui/cron-picker` — recurrence picker generating valid 5-part cron strings without exposing raw syntax. Options: Daily, Weekly, Monthly, Custom (with preview), One-Time (date picker).

#### Layers Touched
- [x] UI

#### Acceptance Criteria
- [ ] Emits `{ cronExpression: string, isOneTime: boolean, oneTimeDate?: Date }`
- [ ] Daily: time picker → `0 {hour} * * *`; Weekly: day + time → `0 {hour} * * {day}`; Monthly: date + time → `0 {hour} {date} * *`
- [ ] Custom: raw cron input + real-time human-readable preview
- [ ] One-Time: date + time picker; emits `isOneTime: true`
- [ ] Invalid cron expressions shown as inline error
- [ ] Purely presentational — no API calls

---

### TB-30: Create Recurring Assignment (Single Store)

**Labels:** `wave-5` `layer:db` `layer:api` `layer:svc` `layer:ui` `fd-06`  
**Milestone:** Wave 5  
**Depends on:** `TB-21` `TB-26` `TB-29`  
**PRD ref:** FD-06

#### Description
`POST /recurring-assignments` binds a template or checklist to a single assignment target (user or store) with a cron schedule.

#### Layers Touched
- [x] DB
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `RecurringAssignments` table: `{ id, tenantId, templateId (nullable), checklistId (nullable), cronExpression, isOneTime, assignedToUserId (nullable), assignedToStoreId (nullable), targetStoreIds (JSON nullable), isActive, createdByUserId, lastFiredAt (nullable) }`
- [ ] Exactly one of `templateId` / `checklistId` required; exactly one of `assignedToUserId` / `assignedToStoreId` required
- [ ] Store Manager scoped to own store(s) only — `403` otherwise
- [ ] Desktop UI: template/checklist picker, assignment type toggle, user/store selector, Cron Picker
- [ ] PWA UI: simplified form for Store Manager
- [ ] Integration test: Store Manager assigning to another store → `403`

---

### TB-31: Multi-Store Recurring Assignment (Supervisor)

**Labels:** `wave-5` `layer:api` `layer:ui` `fd-06`  
**Milestone:** Wave 5  
**Depends on:** `TB-30`  
**PRD ref:** FD-06

#### Description
Extends Recurring Assignments with `targetStoreIds` array. Supervisor broadcasts one template to multiple stores simultaneously. All target stores must be within the Supervisor's region.

#### Layers Touched
- [x] API
- [x] UI

#### Acceptance Criteria
- [ ] `POST /recurring-assignments` with `targetStoreIds`: all stores validated within Supervisor's region
- [ ] Non-supervisors using `targetStoreIds` → `403`
- [ ] Desktop UI: "Multiple Stores" toggle reveals multi-select store picker (filtered to Supervisor's region)
- [ ] `GET /recurring-assignments/{id}` shows `targetStoreIds` with broadcast indicator
- [ ] Integration test: store outside Supervisor's region in `targetStoreIds` → `422`

---

### TB-32: Manage Recurring Assignments

**Labels:** `wave-5` `layer:api` `layer:svc` `layer:ui` `fd-06`  
**Milestone:** Wave 5  
**Depends on:** `TB-30`  
**PRD ref:** FD-06

#### Description
List, pause, resume, and delete Recurring Assignments. Deletion preserves all already-generated Task Instances.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `GET /recurring-assignments` — scoped by user's store/region visibility
- [ ] `POST /recurring-assignments/{id}/pause` → `isActive = false`
- [ ] `POST /recurring-assignments/{id}/resume` → `isActive = true`
- [ ] `DELETE /recurring-assignments/{id}` — removes assignment; existing Tasks unaffected
- [ ] Desktop UI: list table with next-fire-time (human-readable), pause/resume toggle, delete button

---

### TB-33: Quartz.NET Background Job — Task Instance Generation

**Labels:** `wave-5` `layer:job` `layer:db` `fd-06`  
**Milestone:** Wave 5  
**Depends on:** `TB-30` `TB-31`  
**PRD ref:** FD-06

#### Description
Quartz.NET job runs every minute, evaluates active Recurring Assignments against their cron expressions, and generates Task Instances. `IsOneTime` assignments deactivated after first fire.

#### Layers Touched
- [x] JOB
- [x] DB

#### Acceptance Criteria
- [ ] `RecurringAssignmentJob` runs every minute via Quartz.NET scheduler
- [ ] Evaluates each active assignment's cron expression against current time; generates Task Instances if due
- [ ] Task Instance: `{ id, tenantId, templateId (nullable), checklistInstanceId (nullable), storeId, assignedToUserId (nullable), assignedToStoreId (nullable), status: 'Open', dueAt, createdAt }`
- [ ] Checklist assignments: one `ChecklistInstance` + one Task Instance per `ChecklistTemplateItem`
- [ ] `IsOneTime` assignments: deactivated immediately after first fire; `lastFiredAt` updated
- [ ] Idempotent: re-running same minute does not create duplicates (checked via `lastFiredAt`)
- [ ] Integration test: mock Quartz trigger; assert correct Task Instance count for multi-store broadcast

---

### TB-34: Ad Hoc Task Creation

**Labels:** `wave-5` `layer:api` `layer:svc` `layer:ui` `fd-06`  
**Milestone:** Wave 5  
**Depends on:** `TB-21` `TB-29`  
**PRD ref:** FD-06

#### Description
`POST /tasks` creates a Task Instance directly (no Recurring Assignment). Template optional — if omitted, inline `fields` JSON accepted. Assignment (user or store) is mandatory. `dueAt` required.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `POST /tasks` with `templateId`: copies field definitions from template
- [ ] `POST /tasks` without `templateId`: accepts inline `fields` JSON (same schema)
- [ ] One of `assignedToUserId` / `assignedToStoreId` required → `400` if missing
- [ ] `dueAt` required → `400` if missing
- [ ] Desktop UI: "New Task" drawer — template picker (optional), assignment selector, due date/time picker, inline Field Builder if no template
- [ ] PWA UI: simplified "New Task" — title, assignment, due time, one field
- [ ] SignalR broadcast to store group on creation
- [ ] Integration test: no assignment → `400`; Store Manager assigning to different store user → `403`

---

## WAVE 6 — Task Board & Real-Time

---

### TB-35: Task Board Shell — Field PWA

**Labels:** `wave-6` `layer:api` `layer:svc` `layer:ui` `fd-07`  
**Milestone:** Wave 6  
**Depends on:** `TB-33` `TB-34` `TB-11`  
**PRD ref:** FD-07

#### Description
`GET /stores/{id}/tasks/today` returns today's Task Instances. Field PWA renders them grouped by Checklist with completion percentage, and standalone tasks below. Status colours and overdue highlighting.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `GET /stores/{id}/tasks/today` returns `{ checklists: [...], standaloneTasks: [...] }`
- [ ] Each task includes: id, title, status, dueAt, assignedToUserId, assignedToStoreId, checklistId
- [ ] Checklist accordion groups with "X/Y complete" counter
- [ ] Status indicators: Open (grey), In Progress (blue), Overdue (red + elapsed time), Completed (green), Verified (green + check)
- [ ] Overdue tasks sorted to top within group
- [ ] Store Manager: toggle between "Store View" (all tasks) and "My View"
- [ ] `GET /stores/{id}/tasks/today` → `403` for users not assigned to that store

---

### TB-36: SignalR Real-Time Task Board

**Labels:** `wave-6` `layer:api` `layer:svc` `layer:ui` `fd-07`  
**Milestone:** Wave 6  
**Depends on:** `TB-35` `TB-05`  
**PRD ref:** FD-07

#### Description
Task Board subscribes to SignalR `store-{storeId}` group. Events update Angular Signals without page refresh. Azure SignalR Service (not in-process) used in production.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `TaskBoardHub`: clients join `store-{storeId}` on connect (auth required)
- [ ] `.NET TaskHub.BroadcastTaskUpdate()` called on every task state change
- [ ] Angular `TaskBoardService`: HubConnection opened on board init; closed on destroy
- [ ] `tasks = signal<Task[]>([])` updated via SignalR events — no manual refresh
- [ ] New task from background job visible on board within 500ms
- [ ] Connection loss: reconnect indicator shown; board re-fetches on reconnect
- [ ] Azure SignalR Service (not in-process) verified in integration test

---

### TB-37: Task Detail View + Dynamic Field Form

**Labels:** `wave-6` `layer:api` `layer:svc` `layer:ui` `fd-07` `fd-09`  
**Milestone:** Wave 6  
**Depends on:** `TB-35`  
**PRD ref:** FD-07, FD-09

#### Description
`GET /tasks/{id}` returns task + field definitions + existing completion (if any). Field PWA renders a dynamic form with type-appropriate controls per field.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `GET /tasks/{id}` returns: metadata + `fields` array + `completion` (null if not yet done)
- [ ] Numeric → number input with range hint; Boolean → toggle; Text → textarea; Photo → camera button with preview; Checklist → ordered checkboxes with required markers
- [ ] Required fields show validation error on attempted submission
- [ ] Opening a task sets status to `In Progress` server-side via `PATCH /tasks/{id}/status`
- [ ] Completed tasks: field values shown read-only with timestamp and completer name

---

### TB-38: Photo Upload Flow

**Labels:** `wave-6` `layer:api` `layer:svc` `layer:ui` `fd-09`  
**Milestone:** Wave 6  
**Depends on:** `TB-05` `TB-37`  
**PRD ref:** FD-09

#### Description
`GET /tasks/{id}/photo-upload-url` generates a pre-signed Azure Blob SAS URL (15-minute expiry). Angular uploads directly to Blob Storage; blob URL stored as the field value.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `GET /tasks/{id}/photo-upload-url` returns `{ uploadUrl, blobUrl }`; SAS expires in 15 minutes
- [ ] Angular `StorageService` in `libs/data-access/storage`: request SAS → `PUT` to uploadUrl → return blobUrl
- [ ] Upload progress indicator shown in Photo field component
- [ ] Failed uploads show a retry button
- [ ] Blob naming: `{tenantId}/{storeId}/{taskId}/{fieldId}/{timestamp}.jpg`
- [ ] `blobUrl` (no SAS params) stored in `TaskCompletionFieldValues`

---

### TB-39: Claim a Store-Level Task

**Labels:** `wave-6` `layer:api` `layer:svc` `layer:ui` `fd-07` `fd-08`  
**Milestone:** Wave 6  
**Depends on:** `TB-35`  
**PRD ref:** FD-07, FD-08

#### Description
`POST /tasks/{id}/claim` assigns an unowned store-level task to the authenticated user or stores a volunteer name. Prevents double-claiming.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `POST /tasks/{id}/claim` body: `{ volunteerName?: string }`
- [ ] Auth user + no volunteerName → claims as current user (`assignedToUserId` set)
- [ ] Kiosk flow → sets `CompletedByVolunteerName`
- [ ] Can only claim `assignedToStoreId` tasks (not personally assigned) → `409` otherwise
- [ ] Already claimed task → `409` "Task already claimed"
- [ ] SignalR broadcast on claim; Task Board updates claimant name instantly

---

### TB-40: Partial Offline — Service Worker + Submission Queue

**Labels:** `wave-6` `layer:infra` `layer:svc` `fd-07`  
**Milestone:** Wave 6  
**Depends on:** `TB-06` `TB-37`  
**PRD ref:** FD-07

#### Description
Service Worker precaches Task Board + Task Detail routes. Task completion submissions made offline queued in `localStorage`; replayed in FIFO order on reconnect.

#### Layers Touched
- [x] INFRA
- [x] SVC

#### Acceptance Criteria
- [ ] `ngsw-config.json` precaches `/tasks`, `/tasks/*`, app shell
- [ ] Task Board loads from cache with no network (verified in Chrome DevTools offline mode)
- [ ] `OfflineQueueService` in `libs/data-access/offline`: queues `POST /tasks/{id}/complete` when offline
- [ ] On reconnect: queued submissions replayed in FIFO order
- [ ] Duplicate prevention: if task already completed server-side, queued submission discarded (idempotent endpoint)
- [ ] Offline indicator amber banner shown when `navigator.onLine === false`
- [ ] Unit tests: queue add, replay, discard-on-duplicate

---

### TB-41: Store Kiosk View

**Labels:** `wave-6` `layer:api` `layer:svc` `layer:ui` `fd-08`  
**Milestone:** Wave 6  
**Depends on:** `TB-35` `TB-39`  
**PRD ref:** FD-08

#### Description
Dedicated `/kiosk` route on `field-pwa` for shared store device. Full store task board visible. Financial tasks hidden. Volunteer name entry for claiming. Session never expires.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `/kiosk` uses store-account JWT (365-day refresh token TTL for kiosk accounts)
- [ ] Financial tasks (category `Safe`) hidden from kiosk task list
- [ ] Financial field values omitted from API response in kiosk mode
- [ ] All task statuses visible for non-financial tasks
- [ ] Claim flow: tap task → modal with name entry text input + optional "Log in" link
- [ ] Volunteer claim → `CompletedByVolunteerName`; employee login → `CompletedByUserId`
- [ ] Live updates via SignalR (same `store-{storeId}` group)

---

## WAVE 7 — Task Completion & Lifecycle

---

### TB-42: Complete a Task

**Labels:** `wave-7` `layer:db` `layer:api` `layer:svc` `layer:ui` `fd-09`  
**Milestone:** Wave 7  
**Depends on:** `TB-37` `TB-38`  
**PRD ref:** FD-09

#### Description
`POST /tasks/{id}/complete` submits field values, validates required fields, runs range evaluation, records the completion, and returns triggered corrective actions.

#### Layers Touched
- [x] DB
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `TaskCompletions` table: `{ id, taskId, completedByUserId (nullable), completedByVolunteerName (nullable), fieldValues (JSON), correctiveActionsTriggered (JSON), completedAt }`
- [ ] Missing required fields → `400` with field-level errors
- [ ] Range evaluation: Numeric out of range → adds to `correctiveActionsTriggered`
- [ ] Boolean false + correctiveActionText → adds to `correctiveActionsTriggered`
- [ ] Unchecked required Checklist sub-items → blocks with `400`
- [ ] Response includes `{ completion, triggeredCorrectiveActions: [{ fieldName, text }] }`
- [ ] Task status → `Completed`; SignalR broadcast to store group
- [ ] Idempotent: second submission returns existing completion with `200`
- [ ] Integration tests: valid completion, range breach, missing required field, idempotency

---

### TB-43: Corrective Action Inline Display

**Labels:** `wave-7` `layer:ui` `fd-10`  
**Milestone:** Wave 7  
**Depends on:** `TB-42`  
**PRD ref:** FD-10

#### Description
After task submission, if `triggeredCorrectiveActions` is non-empty, Task Detail shows a distinct "Action Required" panel. Task Board marks the task with an amber badge.

#### Layers Touched
- [x] UI

#### Acceptance Criteria
- [ ] "Action Required" panel shows field name + corrective text for each triggered action
- [ ] Panel distinct from normal completion confirmation (amber colour scheme)
- [ ] Task Board: amber badge indicator on tasks with triggered corrective actions
- [ ] Panel dismissible (local flag — no API call)
- [ ] Manager viewing completed task with triggered actions sees same panel in Task Detail

---

### TB-44: Verify a Task

**Labels:** `wave-7` `layer:api` `layer:svc` `layer:ui` `fd-09`  
**Milestone:** Wave 7  
**Depends on:** `TB-42`  
**PRD ref:** FD-09

#### Description
`POST /tasks/{id}/verify` available to `store_manager`+. Transitions `Completed` → `Verified`. Records verifier and timestamp.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] Requires `store_manager`+ role → `403` for `store_employee`
- [ ] Requires `status = Completed` → `409` otherwise
- [ ] `verifiedByUserId` and `verifiedAt` recorded
- [ ] SignalR broadcast to store group
- [ ] Dashboard UI: "Verify" button on completed tasks in Store Manager view

---

### TB-45: Cancel a Task

**Labels:** `wave-7` `layer:api` `layer:svc` `layer:ui` `fd-09`  
**Milestone:** Wave 7  
**Depends on:** `TB-37`  
**PRD ref:** FD-09

#### Description
`POST /tasks/{id}/cancel` with mandatory reason. Available to `store_manager`+. Terminal state. Task remains visible on board (greyed out).

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] Body: `{ reason: string }` — blank reason → `400`
- [ ] Available from Open, In Progress, Overdue states
- [ ] Records `cancelledByUserId`, `cancelReason`, `cancelledAt`
- [ ] Cancelled tasks shown greyed out on Task Board; excluded from completion rate
- [ ] SignalR broadcast; reason visible to Store Manager on task detail
- [ ] UI: "Cancel Task" button (manager-only); reason input modal with confirmation

---

### TB-46: Defer a Task

**Labels:** `wave-7` `layer:api` `layer:svc` `layer:ui` `layer:job` `fd-09`  
**Milestone:** Wave 7  
**Depends on:** `TB-37`  
**PRD ref:** FD-09

#### Description
`POST /tasks/{id}/defer` with mandatory reason and target date. Background job resets deferred tasks to `Open` on target date at 06:00 AM.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI
- [x] JOB

#### Acceptance Criteria
- [ ] Body: `{ reason: string, deferredTo: date }` — both required; `deferredTo` must be after today
- [ ] Records `deferredTo`, `deferReason`, `deferredByUserId`
- [ ] Deferred tasks hidden from `GET /stores/{id}/tasks/today`
- [ ] Quartz.NET job at 06:00 AM daily: resets `Deferred` tasks where `deferredTo = today` to `Open`; SignalR broadcast
- [ ] UI: "Defer Task" button (manager-only); reason + date picker modal
- [ ] Integration test: deferred to tomorrow → not visible today; mock clock → appears as Open

---

### TB-47: Overdue Promotion Job

**Labels:** `wave-7` `layer:job` `fd-09`  
**Milestone:** Wave 7  
**Depends on:** `TB-33`  
**PRD ref:** FD-09

#### Description
Quartz.NET job runs every 5 minutes promoting tasks past `dueAt` from `Open`/`InProgress` → `Overdue`, then after grace period → `CorrectiveActionRaised`.

#### Layers Touched
- [x] JOB

#### Acceptance Criteria
- [ ] `OverduePromotionJob` runs every 5 minutes
- [ ] Pass 1: `status IN ('Open', 'InProgress') AND dueAt < NOW()` → transition to `Overdue`; SignalR broadcast per task
- [ ] Pass 2: `status = 'Overdue' AND dueAt < NOW() - overdueGraceMinutes` → transition to `CorrectiveActionRaised`
- [ ] Grace period from `StoreSettings.overdueGraceMinutes` (default 30)
- [ ] FCM Standard push to Store Manager on `Overdue` transition (via NotificationService stub — TB-48 wires this fully)
- [ ] Integration test: task with past `dueAt` → `Overdue` after job run

---

## WAVE 8 — Notification System

---

### TB-48: NotificationService — Central Dispatch

**Labels:** `wave-8` `layer:api` `layer:db` `fd-15`  
**Milestone:** Wave 8  
**Depends on:** `TB-06` `TB-42`  
**PRD ref:** FD-15

#### Description
Single .NET `NotificationService` — all notification dispatch routes through here. Implements `POST /notifications/register-token` and `DELETE /notifications/register-token`. No feature handler calls FCM or SignalR directly.

#### Layers Touched
- [x] API
- [x] DB

#### Acceptance Criteria
- [ ] `FcmDeviceTokens` table: `{ userId, token, deviceType, lastRefreshedAt, tenantId }`
- [ ] `POST /notifications/register-token` — upserts token for authenticated user
- [ ] `DELETE /notifications/register-token` — removes token (called on logout)
- [ ] `NotificationService.Dispatch(NotificationEvent)` routes to SignalR, FCM, or both
- [ ] FCM delivery failure (stale token 404 from FCM) → token deleted from `FcmDeviceTokens`
- [ ] Unit tests: each event type routes to the correct channel(s)
- [ ] Zero feature handlers call FCM/SignalR directly — enforced by code review

---

### TB-49: FCM Standard Push Notifications

**Labels:** `wave-8` `layer:api` `layer:svc` `fd-15`  
**Milestone:** Wave 8  
**Depends on:** `TB-48`  
**PRD ref:** FD-15

#### Description
Wire Standard-priority FCM pushes for: task assigned to user, task overdue, Walk Session completed. Angular Service Worker routes push clicks to the correct in-app view.

#### Layers Touched
- [x] API
- [x] SVC

#### Acceptance Criteria
- [ ] `TaskAssigned` → Standard FCM push to `assignedToUserId`'s device tokens
- [ ] `TaskOverdue` → Standard FCM to store's `store_manager` users' tokens
- [ ] FCM payload: `{ title, body, data: { taskId, type } }`
- [ ] Angular Service Worker `push` event handler routes by `data.type` to correct in-app route
- [ ] Notification displayed by OS when app is backgrounded
- [ ] Integration test (with FCM emulator or mock): correct payload dispatched on task assignment

---

### TB-50: FCM Critical Push + Deposit Escalation Job

**Labels:** `wave-8` `layer:api` `layer:job` `fd-15` `fd-13`  
**Milestone:** Wave 8  
**Depends on:** `TB-48` `TB-47`  
**PRD ref:** FD-15, FD-13

#### Description
Wire Critical-priority FCM for: temperature out-of-range, `CorrectiveActionRaised`. Implement `DepositEscalationJob` (10:00 AM daily Critical push to Supervisor for stores with no deposit).

#### Layers Touched
- [x] API
- [x] JOB

#### Acceptance Criteria
- [ ] Temperature violation (Numeric breach on temperature-category field) → Critical FCM to Store Manager
- [ ] `CorrectiveActionRaised` transition → Critical FCM to Store Manager + Supervisor
- [ ] FCM `priority: "high"` set on all Critical dispatches
- [ ] `DepositEscalationJob` runs daily at 10:00 AM (Quartz cron: `0 10 * * *`): stores with no `DepositLog` for today → Critical FCM to region's Supervisor
- [ ] Integration test: temperature breach via `CompleteTask` → Critical FCM dispatched
- [ ] Integration test: no deposit by 10 AM → Supervisor receives Critical push

---

## WAVE 9 — Manager Walk

---

### TB-51: Walk Template CRUD

**Labels:** `wave-9` `layer:db` `layer:api` `layer:svc` `layer:ui` `fd-11`  
**Milestone:** Wave 9  
**Depends on:** `TB-21`  
**PRD ref:** FD-11

#### Description
`WalkTemplates` and `WalkAuditItems` tables. CRUD via Admin/Supervisor section of `dashboard`. Same 3-tier scope as Task Templates.

#### Layers Touched
- [x] DB
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `WalkTemplates` table: `{ id, tenantId, name, scope, regionId (nullable), storeId (nullable), isActive, createdByUserId }`
- [ ] `WalkAuditItems` table: `{ id, templateId, label, description, scoringType (PassFail|OneToFive), weight, photoRequired, failCorrectiveActionText, order }`
- [ ] `POST /walk-templates` — same scope/role rules as Task Templates
- [ ] `GET /walk-templates` — same visibility scoping
- [ ] `PUT /walk-templates/{id}` + `POST /walk-templates/{id}/deactivate`
- [ ] Desktop UI: Walk Template list + builder form with audit item management
- [ ] Integration tests cover scope-role rules

---

### TB-52: Walk Audit Item Builder Component

**Labels:** `wave-9` `layer:ui` `fd-11`  
**Milestone:** Wave 9  
**Depends on:** `TB-20` `TB-51`  
**PRD ref:** FD-11

#### Description
`libs/ui/walk-audit-item-builder` — reusable component for defining Walk Audit Items. Mirrors the Dynamic Field Builder pattern.

#### Layers Touched
- [x] UI

#### Acceptance Criteria
- [ ] Supports `PassFail` and `OneToFive` scoring types
- [ ] Each item: label, description (optional), scoring type, weight (default 1.0, must be positive), photoRequired toggle, failCorrectiveActionText (optional)
- [ ] CDK DragDrop reordering
- [ ] Emits `AuditItemsChange: WalkAuditItem[]` output — no direct API calls

---

### TB-53: Start Walk Session

**Labels:** `wave-9` `layer:db` `layer:api` `layer:svc` `layer:ui` `fd-11`  
**Milestone:** Wave 9  
**Depends on:** `TB-51`  
**PRD ref:** FD-11

#### Description
`POST /walk-sessions` opens a Walk Session against a store using a Walk Template. Dashboard renders session shell with one card per audit item.

#### Layers Touched
- [x] DB
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `WalkSessions` table: `{ id, tenantId, templateId, storeId, conductedByUserId, startedAt, completedAt (nullable), compositeScore (nullable) }`
- [ ] `WalkSessionItems` table: `{ id, sessionId, auditItemId, status (Pending|Pass|Fail|NA), score (nullable), notes (nullable), photoBlobUrl (nullable) }`
- [ ] `POST /walk-sessions` creates session + one `WalkSessionItem` per template audit item (status: Pending)
- [ ] Store Manager restricted to their assigned stores → `403` otherwise
- [ ] Dashboard: session page with template name, store, start time, card-per-item (all Pending)
- [ ] `GET /walk-sessions/{id}` returns current state (resumable after refresh)

---

### TB-54: Score Walk Session Items

**Labels:** `wave-9` `layer:api` `layer:svc` `layer:ui` `fd-11`  
**Milestone:** Wave 9  
**Depends on:** `TB-53` `TB-38`  
**PRD ref:** FD-11

#### Description
`PATCH /walk-sessions/{id}/items/{itemId}` updates individual audit item score, notes, and photo. Auto-saves on field blur.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `PATCH` body: `{ status, score (nullable), notes (nullable), photoBlobUrl (nullable) }`
- [ ] `status = Fail` + `photoRequired = true` → `photoBlobUrl` required → `400` otherwise
- [ ] Auto-save triggered on field blur, debounced 500ms
- [ ] Dashboard: Pass/Fail/NA toggle (or 1–5 rating), notes textarea, photo upload button per item
- [ ] Photo upload uses same SAS URL pattern: `GET /walk-sessions/{id}/photo-upload-url`
- [ ] Session header shows "X / Y items scored" progress

---

### TB-55: Submit Walk Session

**Labels:** `wave-9` `layer:api` `layer:svc` `layer:ui` `layer:job` `fd-11`  
**Milestone:** Wave 9  
**Depends on:** `TB-54` `TB-34` `TB-48`  
**PRD ref:** FD-11

#### Description
`POST /walk-sessions/{id}/submit` closes the session, calculates composite score, auto-generates corrective Tasks for failed items, dispatches notifications.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI
- [x] JOB

#### Acceptance Criteria
- [ ] All items must be non-Pending before submit → `409` with list of pending items if not
- [ ] Composite score: `sum(item.score * item.weight) / sum(item.weight)` normalised to 0–100; NA items excluded
- [ ] PassFail: Pass = full weight, Fail = 0
- [ ] Each `Fail` item with `failCorrectiveActionText` → creates Task Instance assigned to `assignedToStoreId`, `dueAt = end-of-day`
- [ ] SignalR broadcast to `store-{storeId}`: new corrective Tasks
- [ ] FCM Standard to Store Manager: "Walk complete — N corrective tasks generated"
- [ ] Integration test: 2 fail items → 2 Task Instances created with correct storeId

---

### TB-56: Walk Session Report View

**Labels:** `wave-9` `layer:api` `layer:svc` `layer:ui` `fd-11`  
**Milestone:** Wave 9  
**Depends on:** `TB-55`  
**PRD ref:** FD-11

#### Description
`GET /walk-sessions/{id}` returns full session report. Dashboard renders score badge, item results, photos, and links to corrective Tasks.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `GET /walk-sessions/{id}` — full data including items, photos, generated task IDs, composite score
- [ ] `GET /stores/{id}/walk-sessions` — paginated history; filterable by date range
- [ ] `GET /regions/{id}/walk-sessions` — all sessions across region (Supervisor)
- [ ] Dashboard: score badge (green/amber/red), item list with status indicators, photos clickable to full size, corrective task links
- [ ] No PDF export button (V2 scope)

---

## WAVE 10 — MDOG & Inventory

---

### TB-57: MDOG System Template Seed

**Labels:** `wave-10` `layer:db` `fd-12`  
**Milestone:** Wave 10  
**Depends on:** `TB-21`  
**PRD ref:** FD-12

#### Description
EF Core seed data: System-scope MDOG Task Template with dough size fields (10"/12"/14"/16") and a temperature field with the 56-Degree Rule range constraint. Seed is idempotent.

#### Layers Touched
- [x] DB

#### Acceptance Criteria
- [ ] Seed: `{ name: "Daily MDOG Check", scope: "System", category: "Inventory" }`
- [ ] Fields: `Dough_10in`, `Dough_12in`, `Dough_14in`, `Dough_16in` (all Numeric); `WalkInTemperature` (Numeric, `rangeMax: 56`, `correctiveActionText: "Product > 56°F — return to refrigeration immediately"`)
- [ ] Seed runs via migration; idempotent (no duplicate creation on re-run)
- [ ] Integration test: fresh tenant DB has MDOG template after migration

---

### TB-58: Inventory Snapshot Write

**Labels:** `wave-10` `layer:db` `layer:api` `fd-12`  
**Milestone:** Wave 10  
**Depends on:** `TB-42` `TB-57`  
**PRD ref:** FD-12

#### Description
On completion of the MDOG Task, `CompleteTaskHandler` writes `InventorySnapshot` records — one per dough-size field. `GET /stores/{id}/inventory/latest` and `/history` endpoints added.

#### Layers Touched
- [x] DB
- [x] API

#### Acceptance Criteria
- [ ] `InventorySnapshots` table: `{ id, tenantId, storeId, date, itemKey, onHandCount, submittedByUserId, createdAt }`
- [ ] `CompleteTaskHandler` detects MDOG template and writes snapshots for all Numeric fields
- [ ] Snapshot `date = today` (date only, not datetime); upserted on re-completion (not duplicated)
- [ ] `GET /stores/{id}/inventory/latest` — most recent snapshot per item
- [ ] `GET /stores/{id}/inventory/history?days=7` — snapshots for last 7 days

---

### TB-59: MDOG Form Pre-Population

**Labels:** `wave-10` `layer:svc` `layer:ui` `fd-12`  
**Milestone:** Wave 10  
**Depends on:** `TB-58` `TB-37`  
**PRD ref:** FD-12

#### Description
When a Store Employee opens an MDOG task, numeric fields pre-populate with the most recent `InventorySnapshot` values for that store.

#### Layers Touched
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `GET /tasks/{id}` for MDOG task includes `previousValues: { [fieldName]: number }` from latest snapshots
- [ ] Task Detail form: pre-populates Numeric inputs with `previousValues` (editable)
- [ ] Pre-populated values visually distinguished ("yesterday: X" hint text)
- [ ] No previous snapshot → fields start empty
- [ ] Unit test: `previousValues` correctly populated from snapshot data

---

### TB-60: 3-Day Need Display + Store Settings

**Labels:** `wave-10` `layer:db` `layer:api` `layer:svc` `layer:ui` `fd-12` `fd-17`  
**Milestone:** Wave 10  
**Depends on:** `TB-59` `TB-13`  
**PRD ref:** FD-12, FD-17

#### Description
`StoreSettings` stores dough need targets per size and till base amounts. MDOG Task Detail shows surplus/deficit per dough field. Admin Panel Store Settings page for configuration.

#### Layers Touched
- [x] DB
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `StoreSettings` table: `{ storeId, tillABase, tillBBase, doughNeedTargets (JSON), timezoneId, overdueGraceMinutes }`
- [ ] `doughNeedTargets` JSON: `{ "Dough_10in": { day2Need: 24, day3Need: 48 }, ... }`
- [ ] `GET /stores/{id}/settings` + `PUT /stores/{id}/settings` (Admin only)
- [ ] Task Detail — MDOG: each dough field shows "Day 2 Need: X | Day 3 Need: Y | Surplus/Deficit: ±Z"
- [ ] Surplus green; deficit red
- [ ] Admin Panel → Store Settings: dough targets per size, till base (dropdown: $50/$75 per till), timezone, overdue grace minutes
- [ ] Integration test: settings updated → next MDOG task detail reflects new targets

---

## WAVE 11 — Safe, Till & Deposit Log

---

### TB-61: Till Count System Template Seed

**Labels:** `wave-11` `layer:db` `fd-13`  
**Milestone:** Wave 11  
**Depends on:** `TB-57` `TB-60`  
**PRD ref:** FD-13

#### Description
EF Core seed data: System-scope "Till Count" Task Template with Till A, Till B, Variance Note, and Manager Initials fields. Conditional required validation for variance fields enforced server-side.

#### Layers Touched
- [x] DB

#### Acceptance Criteria
- [ ] Seed: `{ name: "Till Count", scope: "System", category: "Safe" }`
- [ ] Fields: `TillA` (Numeric), `TillB` (Numeric), `VarianceNote` (Text), `ManagerInitials` (Text)
- [ ] `correctiveActionText` on TillA/TillB: "Variance detected — record reason and manager initials"
- [ ] Server-side conditional: if TillA or TillB out of range → VarianceNote + ManagerInitials become required
- [ ] `CompleteTaskHandler` enforces conditional required logic for `Safe` category tasks
- [ ] Integration test: out-of-range till without VarianceNote → `400`

---

### TB-62: Record Bank Deposit

**Labels:** `wave-11` `layer:db` `layer:api` `layer:svc` `layer:ui` `fd-13`  
**Milestone:** Wave 11  
**Depends on:** `TB-60` `TB-48`  
**PRD ref:** FD-13

#### Description
`POST /stores/{id}/deposit-log` creates an immutable deposit record. One deposit per store per day. SignalR broadcast on submission.

#### Layers Touched
- [x] DB
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `DepositLog` table: `{ id, tenantId, storeId, amount, submittedByManagerId, submittedAt }` — no update/delete endpoints
- [ ] Body: `{ amount: number }` — must be positive
- [ ] One deposit per store per day → second submission returns `409` with existing record
- [ ] SignalR Standard broadcast to `store-{storeId}` on submission
- [ ] Dashboard: "Record Deposit" button; amount input + submit; green compliance badge after submission
- [ ] Integration test: submit deposit → record exists; submit again → `409`

---

### TB-63: Deposit Log History View

**Labels:** `wave-11` `layer:api` `layer:svc` `layer:ui` `fd-13`  
**Milestone:** Wave 11  
**Depends on:** `TB-62`  
**PRD ref:** FD-13

#### Description
`GET /stores/{id}/deposit-log` returns paginated deposit history. Missing deposits shown as red "MISSED" rows.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `GET /stores/{id}/deposit-log` — paginated 20/page, newest first; filterable by `from`/`to` date
- [ ] `GET /stores/{id}/deposit-log/{date}` — single date (or `404`)
- [ ] Admin endpoint: `GET /deposit-log?storeId={id}&from={date}&to={date}` across all stores
- [ ] Dashboard: deposit history table (date, amount, submitted by, time)
- [ ] Missing deposit for past date shown as red "MISSED" row in history

---

## WAVE 12 — Red Book [PLACEHOLDER]

---

### TB-64: Red Book — CANCELLED

**Labels:** `wave-12` `fd-14` `status:cancelled`  
**Milestone:** Wave 12  
**PRD ref:** FD-14 (RETIRED)

#### Description
🚫 **CANCELLED — FD-14 retired 2026-06-09.**

Red Book operational content (Bajco Group PDFs) is loaded as System-scope templates via TB-74 (Admin JSON Template Import). Shift handover communication is implemented as a `NotificationOnly` Form Template in FD-18 (TB-73).

No implementation required for this issue. Close as Won't Fix / Duplicate of TB-73 + TB-74.

#### Acceptance Criteria
- [x] Superseded by TB-73 (Form Templates) and TB-74 (Admin JSON Template Import)

---

## WAVE 13 — Dashboards

---

### TB-65: Store Employee Dashboard

**Labels:** `wave-13` `layer:api` `layer:svc` `layer:ui` `fd-16`  
**Milestone:** Wave 13  
**Depends on:** `TB-42` `TB-35`  
**PRD ref:** FD-16a

#### Description
Field PWA dashboard for Store Employees: personal assigned tasks, claimable store tasks, 7-day personal history, store-wide today progress bar.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] "My Tasks": assigned tasks grouped by Checklist, linked to Task Detail
- [ ] "Open Store Tasks": unassigned store tasks with Claim button
- [ ] "My History": `GET /users/me/completions?days=7` — date, task name, status, completion time
- [ ] "Store Progress": `completedCount / totalCount` for today as a progress bar (Cancelled excluded from denominator)
- [ ] Financial tasks (category `Safe`) hidden from all sections
- [ ] All sections update via existing SignalR connection (no additional subscription needed)

---

### TB-66: Store Manager Dashboard

**Labels:** `wave-13` `layer:api` `layer:svc` `layer:ui` `fd-16`  
**Milestone:** Wave 13  
**Depends on:** `TB-42` `TB-55` `TB-62` `TB-47`  
**PRD ref:** FD-16c

#### Description
Desktop dashboard for Store Managers: today's operational snapshot, active corrective actions, overdue tasks, walk frequency, deposit status.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `GET /dashboard/store/{id}` returns `{ completionRate, openCount, overdueCount, activeCorrectiveActionCount, lastWalkAt, depositLoggedToday, unreadRedBookCount }`
- [ ] Widgets: Completion Rate (donut), Open/Overdue counts (stat cards), Active Corrective Actions list, Walk Frequency, Deposit Status badge
- [ ] Multi-store managers: store selector dropdown; each store has its own data
- [ ] Overdue widget: sortable by elapsed time; click → Task Detail
- [ ] Today-scoped only; data polling every 60 seconds

---

### TB-67: Supervisor Regional Dashboard

**Labels:** `wave-13` `layer:api` `layer:svc` `layer:ui` `fd-16`  
**Milestone:** Wave 13  
**Depends on:** `TB-66` `TB-50`  
**PRD ref:** FD-16d

#### Description
Desktop dashboard for Supervisors: regional store leaderboard (composite score), critical alerts, walk coverage gaps, drill-down to Store Manager view.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `GET /dashboard/region/{id}` returns stores with `{ completionRate, correctiveActionCount, lastWalkAt, depositLoggedToday, compositeScore }`
- [ ] Composite score: `(completionRate * 0.5) + ((1 - corrActionRate) * 0.3) + (walkFrequencyScore * 0.2)` normalised 0–100
- [ ] Leaderboard: stores ranked by composite score; colour-coded (green > 80, amber 60–80, red < 60)
- [ ] Critical Alerts panel: missed deposits today + open `CorrectiveActionRaised` tasks
- [ ] Walk Coverage: stores with no Manager Walk in last 7 days highlighted
- [ ] Click store row → opens Store Manager Dashboard for that store (no role switch)
- [ ] Integration test: composite score formula produces expected output

---

### TB-68: Admin System Dashboard

**Labels:** `wave-13` `layer:api` `layer:svc` `layer:ui` `fd-16`  
**Milestone:** Wave 13  
**Depends on:** `TB-67` `TB-50`  
**PRD ref:** FD-16e

#### Description
Desktop dashboard for Admins: system-wide task completion health, missed deposits today, per-region summary cards, active escalations in last 24 hours.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `GET /dashboard/system` returns `{ systemCompletionRate, storesWithCriticalEscalations, missedDepositsToday: [...], regionalSummary: [...] }`
- [ ] "System Health" widget: overall completion rate across all stores + tenant today
- [ ] "Missed Deposits Today" panel: mandatory, non-dismissible; lists all stores with no deposit by 10 AM
- [ ] "Regional Summary" cards: one per region, completion rate + critical alert count
- [ ] "Active Escalations": Critical FCM events in last 24 hours (from notification log)
- [ ] Admin sees all data — no region scoping

---

## WAVE 14 — Admin Panel (Remaining)

---

### TB-69: System Templates Management UI

**Labels:** `wave-14` `layer:ui` `fd-17`  
**Milestone:** Wave 14  
**Depends on:** `TB-21` `TB-22` `TB-13` `TB-51`  
**PRD ref:** FD-17

#### Description
Admin Panel section for authoring and managing System-scope Task Templates and Walk Templates. Reuses Field Builder (TB-20) and Walk Audit Item Builder (TB-52).

#### Layers Touched
- [x] UI

#### Acceptance Criteria
- [ ] Admin Panel sidebar "System Templates" section with Task Templates and Walk Templates tabs
- [ ] Task Template list filtered to `scope = System`; "New System Template" creates with scope locked to System
- [ ] Walk Template list filtered to `scope = System`; same pattern
- [ ] Edit + deactivate actions available (reuses existing TB-23, TB-24, TB-51 implementations)
- [ ] Non-admin users: route guard → `403` on API + redirect on UI

---

### TB-70: Store Settings UI

**Labels:** `wave-14` `layer:svc` `layer:ui` `fd-17`  
**Milestone:** Wave 14  
**Depends on:** `TB-60` `TB-13`  
**PRD ref:** FD-17

#### Description
Admin Panel section for configuring per-store operational settings: dough need targets, till base amounts, timezone, overdue grace period.

#### Layers Touched
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] Admin Panel → Store Settings: store selector dropdown; form with all `StoreSettings` fields
- [ ] Dough need targets: input per size for Day 2 and Day 3
- [ ] Till A base + Till B base: dropdown ($50 / $75)
- [ ] Timezone: IANA timezone identifier dropdown
- [ ] Overdue grace minutes: number input (default 30)
- [ ] Save calls `PUT /stores/{id}/settings`; success toast shown

---

### TB-71: Tenant Settings UI

**Labels:** `wave-14` `layer:db` `layer:api` `layer:svc` `layer:ui` `fd-17`  
**Milestone:** Wave 14  
**Depends on:** `TB-13`  
**PRD ref:** FD-17

#### Description
Admin Panel section for tenant-level configuration: name, logo (Azure Blob upload), primary contact email. Tenant name shown in dashboard header.

#### Layers Touched
- [x] DB
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `Tenants` master table extended: `name`, `logoUrl`, `primaryContactEmail`
- [ ] `GET /tenant/settings` + `PUT /tenant/settings` — admin only
- [ ] Logo upload: Azure Blob SAS URL pattern; `logoUrl` stored in tenant settings
- [ ] Admin Panel → Tenant Settings: name field, logo upload, contact email
- [ ] Tenant name displayed in `dashboard` app header/title bar

---

## WAVE 0 — Infrastructure Additions

---

### TB-72: Dev Environment Setup + Adapter Interface Scaffolding

**Labels:** `wave-0` `layer:infra` `layer:api` `fd-01`  
**Milestone:** Wave 0  
**Depends on:** `TB-02` `TB-03`  
**PRD ref:** FD-01

#### Description
Set up Supabase as the local dev infrastructure and create the three adapter interfaces (`IAuthProvider`, `IStorageProvider`, `IRealtimeService`) with Supabase and Azure concrete implementations. DI registration controlled by `INFRASTRUCTURE_PROVIDER` env var.

#### Layers Touched
- [x] INFRA
- [x] API

#### Acceptance Criteria
- [ ] `IAuthProvider`, `IStorageProvider`, `IRealtimeService` interfaces defined in `OpsFlow.Domain/Interfaces/`
- [ ] `SupabaseAuthProvider`, `SupabaseStorageProvider`, `SupabaseRealtimeService` in `OpsFlow.Infrastructure/Supabase/`
- [ ] `AzureBlobStorageProvider`, `AzureSignalRService`, `AspNetIdentityAuthProvider` in `OpsFlow.Infrastructure/Azure/`
- [ ] `INFRASTRUCTURE_PROVIDER=supabase` → Supabase concretions registered; `azure` → Azure concretions
- [ ] EF Core selects `UseNpgsql()` or `UseSqlServer()` based on `DATABASE_PROVIDER` env var
- [ ] Integration test: correct concrete resolved from DI for each provider setting
- [ ] `.env.example` updated with all new env vars

---

## WAVE 14 — Admin Panel Additions

---

### TB-74: Admin JSON Template Import

**Labels:** `wave-14` `layer:api` `layer:svc` `layer:ui` `fd-17`  
**Milestone:** Wave 14  
**Depends on:** `TB-73` `TB-21` `TB-26` `TB-13`  
**PRD ref:** FD-17

#### Description
`POST /admin/templates/import` accepts a JSON payload of template definitions and bulk-creates them. Admin Panel UI provides a file picker with import preview. Feeds Bajco Group operational PDF templates after conversion by the internal `execution/pdf_to_template_json.py` script.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `POST /admin/templates/import` — validates each template definition; partial import allowed
- [ ] Response: `{ created: number, failed: [{ index, errors }] }`
- [ ] Admin Panel: "Template Import" section; JSON file picker + paste textarea
- [ ] Import preview: count by type before confirming
- [ ] Integration test: mixed valid/invalid batch → correct partial create + error list
- [ ] Internal script `execution/pdf_to_template_json.py` documented in `directives/bajco_template_onboarding.md`

---

## WAVE 15 — Forms

---

### TB-73: Form Template CRUD + Builder UI

**Labels:** `wave-15` `layer:db` `layer:api` `layer:svc` `layer:ui` `fd-18`  
**Milestone:** Wave 15  
**Depends on:** `TB-21` `TB-72`  
**PRD ref:** FD-18

#### Description
`POST /form-templates` creates a Form Template using the unified `Templates` table (TPH, `TemplateType = Form`). Extends the shared `FieldBuilderComponent` with a `PropagationTypePicker` and `ApprovalStepsBuilder`. Full CRUD with same scope/role rules as Task Templates.

#### Layers Touched
- [x] DB
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `Templates` table: `templateType` discriminator column + `typeConfig` JSONB column added via migration
- [ ] EF Core TPH mapping with `FormTemplate`, `TaskTemplate`, `ChecklistTemplate` entity types
- [ ] `POST /form-templates` validates: valid `propagationType`; non-empty `approvalSteps`; scope/role rules
- [ ] `GET /form-templates` — same visibility scoping as `GET /templates`; filterable by `propagationType`
- [ ] `PUT /form-templates/{id}` + `POST /form-templates/{id}/deactivate` (blocked if active FormSubmissions exist)
- [ ] `PropagationTypePicker` component in `libs/ui/template-builder`
- [ ] `ApprovalStepsBuilder` component: role dropdown + CDK DragDrop reorder
- [ ] Form Templates appear in `TemplatePicker` dropdown
- [ ] Integration tests: Sequential / Parallel / NotificationOnly creation; scope violations

---

### TB-75: Form Submission — Draft + Submit

**Labels:** `wave-15` `layer:db` `layer:api` `layer:svc` `layer:ui` `fd-19`  
**Milestone:** Wave 15  
**Depends on:** `TB-73` `TB-48`  
**PRD ref:** FD-19

#### Description
Creates `FormSubmissions` and `FormSubmissionApprovalSteps` tables. `POST /form-submissions` creates a Draft. `POST /form-submissions/{id}/submit` advances to `PendingApproval[step 1]` (or `Recorded` for NotificationOnly). Notifies step 1 reviewers. Unified Create flow entry point for Forms.

#### Layers Touched
- [x] DB
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `FormSubmissions` table: `{ id, tenantId, formTemplateId (nullable), storeId, submittedByUserId, status, currentStepOrder, fieldValues (JSON), createdAt, submittedAt, resolvedAt }`
- [ ] `FormSubmissionApprovalSteps` table: `{ id, submissionId, stepOrder, role, actionByUserId, action, comments, actionAt }`
- [ ] `POST /form-submissions` → `Draft` state
- [ ] `POST /form-submissions/{id}/submit` → `PendingApproval[step 1]`; step rows inserted; FCM Standard + SignalR to step 1 role users
- [ ] NotificationOnly submit → `Recorded`; all step role users notified
- [ ] `GET /form-submissions/my-submissions` — newest first, includes status and template name
- [ ] Integration tests: Draft → Submit → PendingApproval; NotificationOnly → Recorded; missing required field → 400

---

### TB-76: Form Submission — Reviewer Actions

**Labels:** `wave-15` `layer:api` `layer:svc` `layer:ui` `fd-19`  
**Milestone:** Wave 15  
**Depends on:** `TB-75`  
**PRD ref:** FD-19

#### Description
Approve (advances step or terminal Approved), Reject (terminal with reason), Return (rework loop with comments). Sequential and Parallel resolution logic. "Pending My Review" queue for reviewers.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `POST /form-submissions/{id}/approve` — role-scoped; Sequential advances step or reaches Approved; Parallel auto-closes remaining steps
- [ ] `POST /form-submissions/{id}/reject` body: `{ reason: string }` — terminal; notifies submitter via FCM Standard
- [ ] `POST /form-submissions/{id}/return` body: `{ comments: string }` — `Returned` state; submitter notified; Sequential re-enters at returning step on re-submit
- [ ] `GET /form-submissions/pending-review` — scoped to authenticated user's role and store/region
- [ ] Dashboard + PWA: "Pending Review" queue with Approve / Return / Reject action buttons; Return/Reject require input modal
- [ ] SignalR broadcast to store group on each action
- [ ] Integration tests: full Sequential chain; Return → re-submit re-enters at returning step; Parallel first-action-wins; Reject terminal

---

### TB-77: Form Submission Piles + History UI

**Labels:** `wave-15` `layer:api` `layer:svc` `layer:ui` `fd-19`  
**Milestone:** Wave 15  
**Depends on:** `TB-76`  
**PRD ref:** FD-19

#### Description
"My Submissions" UI with state-filtered piles (Draft, Returned, Rejected, Approved, Recorded). Submission detail view with field values, approval trail, and reviewer comments. Management views for Supervisor and Admin.

#### Layers Touched
- [x] API
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] `GET /form-submissions/{id}` — full detail: field values, approval steps with actions and comments
- [ ] UI: "My Submissions" section in both apps; tabs/filter by state
- [ ] Returned submissions: reviewer comments prominent; "Revise & Resubmit" opens pre-populated form
- [ ] Rejected submissions: rejection reason shown; read-only terminal
- [ ] Draft submissions: "Continue" button resumes form
- [ ] Supervisor: `GET /form-submissions?regionId={id}` management view
- [ ] Admin: `GET /form-submissions?storeId={id}` across all stores

---

### TB-78: Form Submission Notifications

**Labels:** `wave-15` `layer:api` `layer:svc` `fd-19` `fd-15`  
**Milestone:** Wave 15  
**Depends on:** `TB-76` `TB-48`  
**PRD ref:** FD-19, FD-15

#### Description
Wires all Form Submission events through `NotificationService`. All Standard priority. FCM payload includes `submissionId` for deep-link routing.

#### Layers Touched
- [x] API
- [x] SVC

#### Acceptance Criteria
- [ ] `FormSubmitted` → FCM Standard to step 1 role users in store/region
- [ ] `FormReturned` → FCM Standard to submitter with reviewer name + comments
- [ ] `FormRejected` → FCM Standard to submitter with rejection reason
- [ ] `FormApproved` (final step or Parallel resolved) → FCM Standard to submitter
- [ ] `FormRecorded` (NotificationOnly) → FCM Standard to all ApprovalStep role users
- [ ] All events also broadcast via SignalR to `store-{storeId}` group
- [ ] FCM payload: `{ title, body, data: { submissionId, type: "form_*" } }`
- [ ] No form events use Critical priority in V1
- [ ] Unit tests: each event type dispatches to correct channel and recipients

---

### TB-79: Unified Create Flow — Home Screen Entry Point

**Labels:** `wave-15` `layer:ui` `layer:svc` `fd-18` `fd-19`  
**Milestone:** Wave 15  
**Depends on:** `TB-34` `TB-73` `TB-75`  
**PRD ref:** FD-18, FD-19

#### Description
Universal "Create" button in both apps. Type selector (Task | Checklist | Form) routes to the appropriate form. Shared `TemplatePicker` dropdown pre-fills from visible templates. "Save as Template" toggle on all types.

#### Layers Touched
- [x] SVC
- [x] UI

#### Acceptance Criteria
- [ ] Persistent "Create" button: FAB on `field-pwa`, button in `dashboard` header
- [ ] Type selector: Task | Checklist | Form — determines which sub-form renders
- [ ] `TemplatePicker` in `libs/ui/template-builder/create-flow/`: filtered to selected type and user's visible scope
- [ ] "Save as Template" toggle: saves template before executing; execution blocked if save fails
- [ ] "Execute Only" path: Task Instance (TB-34 flow), Checklist Instance, or Form Submission Draft (TB-75 flow)
- [ ] `TypeSelector`, `TemplatePicker`, `SaveToggle` extracted to `libs/ui/template-builder/create-flow/` as standalone components with no API calls
- [ ] Unit tests for all three type paths with mocked sub-forms

---

## Bulk Creation Script (GitHub CLI)

To create all issues at once, run the following (requires `gh` CLI and labels pre-created):

```bash
#!/bin/bash
# Run from the repo root after creating all labels
# Requires: gh auth login

REPO="your-org/opsflow"

create_issue() {
  local title="$1"
  local labels="$2"
  local milestone="$3"
  local body_file="$4"
  gh issue create \
    --repo "$REPO" \
    --title "$title" \
    --label "$labels" \
    --milestone "$milestone" \
    --body-file "$body_file"
}

# Example for TB-01:
# create_issue "[TB-01] Nx Monorepo Workspace Scaffold" "wave-0,layer:infra,fd-01" "Wave 0" ./issues/tb-01.md
```

> For bulk creation, export each issue body above to `./issues/tb-{nn}.md` and loop through the `create_issue` function. The title format is `[TB-XX] <Title>` for easy filtering.

---

## Label Creation Script

```bash
#!/bin/bash
REPO="your-org/opsflow"

# Wave labels
for i in 0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15; do
  gh label create "wave-$i" --repo "$REPO" --color "0075ca" --description "Wave $i"
done

# Layer labels
gh label create "layer:db"    --repo "$REPO" --color "e4e669" --description "Database migration or model"
gh label create "layer:api"   --repo "$REPO" --color "d93f0b" --description ".NET handler and endpoint"
gh label create "layer:svc"   --repo "$REPO" --color "0e8a16" --description "Angular service (libs/data-access)"
gh label create "layer:ui"    --repo "$REPO" --color "5319e7" --description "Angular component"
gh label create "layer:infra" --repo "$REPO" --color "b60205" --description "Infrastructure or pipeline"
gh label create "layer:job"   --repo "$REPO" --color "f9d0c4" --description "Background job (Quartz.NET)"

# Status labels
gh label create "status:blocked"     --repo "$REPO" --color "b60205" --description "Blocked on dependency"
gh label create "status:ready"       --repo "$REPO" --color "0e8a16" --description "Ready to grab"
gh label create "status:in-progress" --repo "$REPO" --color "0075ca" --description "In progress"
gh label create "status:done"        --repo "$REPO" --color "cfd3d7" --description "Merged and done"

# FD labels
for i in $(seq -w 1 19); do
  gh label create "fd-$i" --repo "$REPO" --color "c5def5" --description "Feature Domain $i"
done

# Cancelled status label
gh label create "status:cancelled" --repo "$REPO" --color "eeeeee" --description "Issue cancelled / won't fix"
```

---

*79 issues. 16 waves. Zero horizontal layers. Each issue is a vertical slice — demonstrable in a browser or via an API call by end of session.*
