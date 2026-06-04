# OpsFlow — Product Requirements Document (V1)

**Version:** 1.0  
**Status:** Draft  
**Date:** 2026-06-02  
**Authors:** numairs-ui + Claude Sonnet 4.6  
**Shared Design Concept reached:** 2026-06-02 via Grilling Phase  
**Reference:** OpsFlow Angular 17 Architecture & Agentic Implementation Guide  
**Prior art:** React Native prototype in `opsflow-app/` — treated as reference only, not migrated

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Ubiquitous Language](#2-ubiquitous-language)
3. [Architecture Decisions (Locked)](#3-architecture-decisions-locked)
4. [Roles & Permissions Matrix](#4-roles--permissions-matrix)
5. [Feature Domains](#5-feature-domains)
   - [FD-01 Multi-Tenancy & Tenant Provisioning](#fd-01-multi-tenancy--tenant-provisioning)
   - [FD-02 Authentication & Authorization](#fd-02-authentication--authorization)
   - [FD-03 Store, Region & User Management](#fd-03-store-region--user-management)
   - [FD-04 Task Template System](#fd-04-task-template-system)
   - [FD-05 Checklist System](#fd-05-checklist-system)
   - [FD-06 Recurring Assignments & Scheduling](#fd-06-recurring-assignments--scheduling)
   - [FD-07 Task Board — Field PWA](#fd-07-task-board--field-pwa)
   - [FD-08 Store Kiosk View](#fd-08-store-kiosk-view)
   - [FD-09 Task Completion & Verification](#fd-09-task-completion--verification)
   - [FD-10 Corrective Actions](#fd-10-corrective-actions)
   - [FD-11 Manager Walk](#fd-11-manager-walk)
   - [FD-12 MDOG & Inventory](#fd-12-mdog--inventory)
   - [FD-13 Safe, Till & Deposit Log](#fd-13-safe-till--deposit-log)
   - [FD-14 Red Book](#fd-14-red-book-placeholder)
   - [FD-15 Notification System](#fd-15-notification-system)
   - [FD-16 Dashboards](#fd-16-dashboards)
   - [FD-17 Admin Panel](#fd-17-admin-panel)
6. [Data Model — Key Entities](#6-data-model--key-entities)
7. [Non-Functional Requirements](#7-non-functional-requirements)
8. [V1 Out-of-Scope (Hard Boundary)](#8-v1-out-of-scope-hard-boundary)
9. [Definition of Done](#9-definition-of-done)

---

## 1. Executive Summary

OpsFlow is a multi-tenant operational compliance platform built for franchise restaurant operators. It replaces paper checklists, WhatsApp group threads, and disconnected spreadsheets with a structured, real-time system for task management, inventory compliance, financial record-keeping, and manager-to-manager communication.

**The problem it solves:** Store-level operational standards (temperature compliance, dough production planning, till counts, parking lot maintenance) are currently tracked inconsistently across locations. When standards slip, there is no automated escalation, no audit trail, and no mechanism for a regional supervisor to identify systemic failures before they become customer-facing incidents.

**Who uses it:**
- **Store Employees** — complete assigned tasks and claim open store tasks on the Field PWA
- **Store Managers** — author tasks, manage their store's board, conduct walks, and hand over via the Red Book
- **Supervisors** — broadcast tasks across multiple stores, review regional performance, and conduct formal audits
- **Admins (Owners)** — oversee the entire operation, manage users and stores, and configure system-level templates

**V1 Scope:** Two Angular 17 surfaces (Field PWA + Desktop Dashboard) backed by a .NET 9 Vertical Slice API on Azure. Full task lifecycle management, structured Manager Walks, MDOG inventory compliance, Safe/Till financial tracking, and a real-time notification system.

---

## 2. Ubiquitous Language

All human and agent participants — including AI coding agents — must use this terminology precisely. Deviation produces Dock Rot.

| Term | Definition |
|------|------------|
| **Task Board** | The primary field interface on the PWA for real-time task execution and completion |
| **Task Template** | A reusable blueprint defining a task's fields, validation ranges, corrective actions, and scope. Has no assignment or schedule. |
| **Checklist** | A named grouping container for related Tasks. A Checklist is complete when all its Tasks are resolved. |
| **Recurring Assignment** | The entity binding a Task Template to one or more assignment targets and a cron schedule. The background job evaluates this to generate Task instances. |
| **Task Instance** | A concrete, date-stamped Task generated from a Template (or created ad hoc). The thing a Store Employee actually completes. |
| **Manager Walk** | A live, session-based audit of store performance standards using a Walk Template. Produces a scored report and auto-generates corrective Tasks. |
| **MDOG** | Master Daily Operational Guide — the source of truth for production needs and inventory prep |
| **Corrective Action** | A pre-authored remediation step embedded in a Task Template. Automatically surfaced when a completion value falls outside the defined range. |
| **Red Book** | The structured asynchronous communication log for manager-to-manager shift handovers |
| **Deposit Log** | The immutable, timestamped financial compliance record for daily bank deposits |
| **Inventory Snapshot** | A persisted daily record of on-hand inventory counts per item per store, used to pre-populate MDOG task forms |
| **Store Kiosk** | The shared store-level device profile (permanently logged in) from which any employee or volunteer can claim and complete store-assigned tasks |
| **Tracer Bullet** | A vertical slice of functionality crossing all layers (API endpoint + Service + UI component) that produces an immediately testable, visible feature |
| **Shared Design Concept** | The full architectural understanding reached through the Grilling Phase — this document is its written record |
| **Dock Rot** | Documentation or code comments that have diverged from the codebase, misleading AI agents |
| **App Slop** | Functionality that technically runs but lacks operational taste, quality, or user-centricity |
| **Deep Module** | A component or service with high functionality behind a simple interface — the mandatory structural standard |
| **Tenant** | A single client organisation (e.g., a franchise group) with its own isolated SQL Server database |

---

## 3. Architecture Decisions (Locked)

These decisions are fixed. They may not be reopened during V1 implementation without a formal PRD amendment.

### Frontend
| Decision | Choice | Rationale |
|----------|--------|-----------|
| Framework | Angular 17 Standalone Components | — |
| Workspace | Nx Monorepo | Enforced library boundaries; maps to Deep Module principle |
| Apps | `field-pwa`, `dashboard` | Two surfaces, two distinct user contexts |
| Shared libs | `libs/data-access/`, `libs/ui/`, `libs/util/` | Apps consume from libs; never from each other |
| State — UI | Angular Signals (`signal()`, `computed()`, `effect()`) | Reactive, zero change-detection overhead |
| State — Async | RxJS Observables → `toSignal()` at component boundary | HTTP + SignalR stay idiomatic; Signals own the UI graph |
| Offline | Partial — Angular Service Worker cache + submission queue | Full offline deferred to V2 |
| Push | `@angular/pwa` + FCM Web Push | Service Worker intercepts; FCM delivers |

### Backend
| Decision | Choice | Rationale |
|----------|--------|-----------|
| Framework | .NET 9 | — |
| API Style | Vertical Slice Architecture | One folder per feature action; maps to Tracer Bullet methodology |
| Folder pattern | `Features/{Domain}/{Action}/` | `CreateTask/`, `CompleteTask/`, `StartWalkSession/` etc. |
| Auth | ASP.NET Core Identity + JWT | Role claims: `store_employee`, `store_manager`, `supervisor`, `admin` |
| Real-time | SignalR, store-scoped groups | `store-{storeId}`, `region-{regionId}` group targeting |
| Background jobs | .NET `IHostedService` + Quartz.NET | Evaluates cron expressions on `RecurringAssignments` |
| ORM | Entity Framework Core 9 | Code-first migrations |

### Infrastructure
| Decision | Choice |
|----------|--------|
| Hosting | Azure App Service (.NET API) |
| Database | Azure SQL (SQL Server) |
| Frontend hosting | Azure Static Web Apps |
| File storage | Azure Blob Storage, pre-signed SAS URLs |
| Push messaging | Firebase Cloud Messaging (FCM) |
| Multi-tenancy | Database-per-tenant; connection string resolved from JWT tenant claim |
| Migrations | Orchestrator runs EF Core migrations across all tenant databases on deploy |

### Testing
| Layer | Tooling |
|-------|---------|
| Unit | xUnit + FluentAssertions (business logic, handlers, validators, MDOG calculations) |
| Integration | `WebApplicationFactory` + Testcontainers (real SQL Server per test run) |
| E2E | Deferred to V2 (Playwright) |
| Angular | Jest unit tests for `libs/data-access` services with mocked HTTP |

---

## 4. Roles & Permissions Matrix

### Role Hierarchy
```
Admin (Owner)
  └── Supervisor (Regional)
        └── Store Manager
              └── Store Employee
```

Multiple Admin accounts are supported per tenant. A tenant may have one or many Admins.

### Store Assignment
| Role | Store Assignment |
|------|----------------|
| Store Employee | Single store (`Users.StoreId` non-nullable FK) |
| Store Manager | One or many stores (`UserStoreAssignments` join table) |
| Supervisor | Scoped to a `Region` (a Region contains many Stores) |
| Admin | System-wide; no store/region assignment required |

### Permissions Matrix

| Capability | Store Employee | Store Manager | Supervisor | Admin |
|------------|:-:|:-:|:-:|:-:|
| View own assigned tasks | ✓ | ✓ | ✓ | ✓ |
| Complete a task | ✓ | ✓ | ✓ | ✓ |
| Claim a store-level task | ✓ | ✓ | ✓ | ✓ |
| Create ad hoc task (own store) | — | ✓ | ✓ | ✓ |
| Create Task Template (Store scope) | — | ✓ | ✓ | ✓ |
| Create Task Template (Regional scope) | — | — | ✓ | ✓ |
| Create Task Template (System scope) | — | — | — | ✓ |
| Create Recurring Assignment (own store) | — | ✓ | ✓ | ✓ |
| Create Recurring Assignment (multi-store) | — | — | ✓ | ✓ |
| Cancel / Defer a task | — | ✓ | ✓ | ✓ |
| Verify a completed task | — | ✓ | ✓ | ✓ |
| Conduct a Manager Walk | — | ✓ | ✓ | ✓ |
| Create Walk Template (Store scope) | — | ✓ | ✓ | ✓ |
| Create Walk Template (Regional/System scope) | — | — | ✓ | ✓ |
| Post a Red Book entry | — | ✓ | ✓ | ✓ |
| View Red Book (own store) | — | ✓ | ✓ | ✓ |
| View Red Book (all stores in region) | — | — | ✓ | ✓ |
| Record Deposit Log entry | — | ✓ | ✓ | ✓ |
| View Deposit Log | — | ✓ | ✓ | ✓ |
| View store dashboard | ✓ | ✓ | ✓ | ✓ |
| View regional dashboard | — | — | ✓ | ✓ |
| View system-wide dashboard | — | — | — | ✓ |
| Manage users (create/edit/deactivate) | — | — | — | ✓ |
| Manage stores & regions | — | — | — | ✓ |
| Manage tenant settings | — | — | — | ✓ |

---

## 5. Feature Domains

Each Feature Domain is a self-contained unit designed to be decomposed into Tracer Bullets at Phase 4. Each domain section includes: summary, user stories, acceptance criteria, key entities, and API surface.

---

### FD-01: Multi-Tenancy & Tenant Provisioning

**Summary:** OpsFlow is a multi-tenant SaaS. Each client organisation (tenant) gets an isolated SQL Server database. The .NET API resolves the active tenant from a claim in the JWT and routes all data access to the correct database. No cross-tenant data access is possible at the application layer.

**User Stories:**
- As a **System Operator**, I need to provision a new tenant (create database, run migrations, seed System templates) so that a new franchise client can onboard.
- As a **logged-in user**, I need every API call to be automatically scoped to my tenant so I never see another client's data.

**Acceptance Criteria:**
- [ ] A tenant provisioning script creates a new Azure SQL database, runs EF Core migrations, and seeds System-level templates
- [ ] The JWT issued by ASP.NET Core Identity contains a `tenantId` claim
- [ ] The .NET API resolves the correct connection string from `tenantId` on every request via a `TenantDbContextFactory`
- [ ] No API endpoint returns data without first resolving tenant context — integration tests verify cross-tenant isolation
- [ ] Tenant provisioning is a CLI/admin operation; there is no self-serve signup flow in V1

**Key Entities:** `Tenants` (master DB), per-tenant databases  
**Out of scope for this domain:** Billing, self-serve onboarding, tenant suspension

---

### FD-02: Authentication & Authorization

**Summary:** ASP.NET Core Identity manages users and roles within each tenant database. On successful login, the API mints a short-lived JWT (access token) and a longer-lived refresh token. Angular stores the JWT in memory (not localStorage) and attaches it as a `Bearer` header. Route guards on both Angular apps enforce role-based access.

**User Stories:**
- As a **Store Employee**, I need to log in with my email and password so I can access my task board.
- As a **Store Manager**, I need my JWT to carry my `store_manager` role claim so dashboard routes are automatically accessible.
- As an **Admin**, I need to reset any user's password and deactivate accounts.
- As a **developer/agent**, I need every protected endpoint to return `401` without a valid JWT and `403` with an insufficient role — testable without a browser.

**Acceptance Criteria:**
- [ ] `POST /auth/login` returns `{ accessToken, refreshToken, expiresIn }`
- [ ] `POST /auth/refresh` issues a new access token given a valid refresh token
- [ ] `POST /auth/logout` invalidates the refresh token
- [ ] Access tokens expire in 15 minutes; refresh tokens expire in 7 days
- [ ] JWT payload contains: `sub` (userId), `tenantId`, `role`, `storeId` (nullable), `regionId` (nullable)
- [ ] Angular `AuthService` stores access token in memory; refresh token in an `HttpOnly` cookie
- [ ] Angular `AuthGuard` redirects unauthenticated users to `/login`
- [ ] Angular `RoleGuard` redirects users with insufficient role to `/unauthorized`
- [ ] All protected API endpoints return `401` with no token; `403` with wrong role — covered by integration tests
- [ ] Password reset flow: Admin triggers reset → user receives email with one-time link

**Key Entities:** `Users`, `Roles`, `RefreshTokens`  
**API Surface:** `POST /auth/login`, `POST /auth/refresh`, `POST /auth/logout`, `POST /auth/reset-password`  
**Out of scope:** SSO, OAuth, MFA, social login

---

### FD-03: Store, Region & User Management

**Summary:** Admins configure the organisational structure of the tenant: Regions, Stores within Regions, and Users assigned to Stores or Regions. Store Managers can be assigned to multiple stores. Store Employees are assigned to exactly one store.

**User Stories:**
- As an **Admin**, I need to create Regions and assign Stores to them so Supervisors have a defined scope.
- As an **Admin**, I need to create user accounts, assign roles, and assign them to stores or regions.
- As an **Admin**, I need to deactivate a user account without deleting historical data.
- As a **Store Manager**, I need to see which Store Employees are assigned to my store.

**Acceptance Criteria:**
- [ ] Admin can create, edit, and deactivate Regions
- [ ] Admin can create, edit, and deactivate Stores; each Store belongs to exactly one Region
- [ ] Admin can create users with role `store_employee`, `store_manager`, `supervisor`, or `admin`
- [ ] `store_employee` accounts require a single `StoreId`; enforced at creation
- [ ] `store_manager` accounts can be assigned to multiple stores via `UserStoreAssignments`
- [ ] `supervisor` accounts are assigned to a `RegionId`
- [ ] Deactivated users cannot log in; their historical task completions remain intact
- [ ] Store Manager can view the employee roster for their assigned store(s)
- [ ] Admin panel displays all users, filterable by role and store

**Key Entities:** `Regions`, `Stores`, `Users`, `UserStoreAssignments`  
**API Surface:** CRUD for `/regions`, `/stores`, `/users`; `POST /users/{id}/deactivate`; `GET /stores/{id}/employees`  
**Out of scope:** Import from CSV, HR system integration

---

### FD-04: Task Template System

**Summary:** Task Templates are the reusable blueprints that define what a task asks for. They carry no assignment or schedule — those concerns belong to Recurring Assignments (FD-06) or ad hoc creation (FD-07). Templates exist at three scopes: System (Admin only), Regional (Supervisor and above), and Store (Store Manager and above). Each template defines a dynamic array of typed fields, range validation rules, and pre-authored corrective actions per field.

**User Stories:**
- As an **Admin**, I need to create System-level templates (e.g., "Bank Deposit", "Temperature Check") that appear as read-only defaults in every store.
- As a **Supervisor**, I need to create Regional templates pushed to all stores in my region.
- As a **Store Manager**, I need to create Store-level templates for operational tasks specific to my location.
- As a **Store Manager**, I need to build a task with multiple dynamic fields (numeric, boolean, photo, text) so that a single task can capture composite data (e.g., four dough size counts in one submission).
- As a **Store Manager**, I need to define a range (min/max) on a numeric field and author a corrective action text that surfaces automatically when the value is out of range.

**Field Types:**

| Type | Description | Supports Range | Supports Corrective Action |
|------|-------------|:-:|:-:|
| `Numeric` | Number input (integer or decimal) | ✓ | ✓ |
| `Boolean` | Yes / No toggle | — | ✓ (on "No") |
| `Text` | Free-text input | — | — |
| `Photo` | Camera capture or file upload | — | — |
| `Checklist` | Ordered list of sub-items, each with `required: boolean` and `label: string` | — | ✓ (on unchecked required item) |

**Acceptance Criteria:**
- [ ] Template author can add, reorder, and remove fields of any supported type
- [ ] Each `Numeric` field supports optional `rangeMin`, `rangeMax`, and `correctiveActionText`
- [ ] Each `Boolean` field supports optional `correctiveActionText` triggered on "No"
- [ ] `Checklist` field type supports an ordered array of sub-items, each with `label` and `required` flag
- [ ] Templates have a `scope` enum: `System | Regional | Store`
- [ ] `Regional` templates require a `regionId`; `Store` templates require a `storeId`
- [ ] System templates are read-only to Store Managers and Supervisors (cannot be edited, only used)
- [ ] Templates can be activated or deactivated without deletion
- [ ] Template list is filterable by scope, category, and active status
- [ ] Deleting a template that has active Recurring Assignments is blocked with a clear error
- [ ] Full authoring UI available on desktop dashboard; simplified "quick template" creation on Field PWA (title + basic fields only)

**Key Entities:** `TaskTemplates`, `TaskTemplateFields` (or stored as JSON column `Fields`)  
**API Surface:** `GET /templates`, `POST /templates`, `PUT /templates/{id}`, `DELETE /templates/{id}`, `POST /templates/{id}/deactivate`  
**Out of scope:** Template versioning, template cloning across tenants

---

### FD-05: Checklist System

**Summary:** A Checklist is a named grouping container for related Tasks. Checklists follow the same three-tier scope as Templates. A Checklist is considered complete when all its constituent Task Instances are in a terminal state (Completed, Cancelled, or Deferred). Tasks can also exist standalone (`ChecklistId` is nullable).

**User Stories:**
- As a **Store Manager**, I need to group "Opening Duties" tasks into a named Checklist so my team can work through them as a structured set.
- As a **Supervisor**, I need to create a Regional Checklist ("Q3 Cleanliness Standards") and assign it to all stores in my region as a recurring assignment.
- As a **Store Employee**, I need to see which Checklist a task belongs to so I understand its context.

**Acceptance Criteria:**
- [ ] Checklists have `name`, `description`, `scope` (`System | Regional | Store`), `regionId` (nullable), `storeId` (nullable)
- [ ] A Checklist contains an ordered list of Task Template references — the order determines display order on the Task Board
- [ ] When a Checklist is instantiated (via Recurring Assignment or ad hoc), one Task Instance is created per Template in the Checklist
- [ ] Checklist completion percentage is derived in real-time from its Task Instance statuses — not stored
- [ ] Standalone Tasks (`ChecklistId = null`) are displayed below grouped Checklists on the Task Board
- [ ] A Task cannot be moved between Checklists after instantiation
- [ ] Checklists can be activated or deactivated

**Key Entities:** `Checklists`, `ChecklistTemplateItems` (ordered join), `Tasks.ChecklistInstanceId` (nullable)  
**API Surface:** CRUD for `/checklists`; `GET /checklists/{id}/progress`  
**Out of scope:** Nested checklists, checklist dependencies (Task B cannot start until Task A is done)

---

### FD-06: Recurring Assignments & Scheduling

**Summary:** A Recurring Assignment binds a Task Template (or Checklist) to one or more assignment targets (individual users, stores, or multiple stores) and a cron schedule. The .NET background job (Quartz.NET) evaluates active Recurring Assignments each minute and generates Task Instances when the cron fires. A Supervisor can broadcast a single Recurring Assignment to multiple stores, generating one Task Instance per store per firing.

**User Stories:**
- As a **Supervisor**, I need to create a Recurring Assignment for the "Temperature Check" template that fires every day at 06:00 AM across all 12 stores in my region — without manually creating tasks per store.
- As a **Store Manager**, I need to set up a recurring "Friday Deep Clean" checklist that assigns to my store every Friday at 08:00 AM.
- As a **Store Manager**, I need to create a one-off task ("Fix broken display case") assigned to a specific employee with a same-day due time.

**Cron Schedule:**
- Format: standard 5-part cron expression (`minute hour day-of-month month day-of-week`)
- UI: a friendly recurrence picker (Daily / Weekly on [day] / Monthly on [date] / Custom) generates the cron string — managers never type raw cron
- `IsOneTime: boolean` flag — if true, the background job deactivates the Recurring Assignment after the first successful instantiation

**Assignment Targets:**
- `AssignedToUserId` (nullable) — specific named user
- `AssignedToStoreId` (nullable) — store-level, unowned task
- `TargetStoreIds: string[]` — for Supervisor multi-store broadcast; generates one Task Instance per store
- Exactly one of `AssignedToUserId` or `AssignedToStoreId` (or `TargetStoreIds`) must be populated — enforced at API validation

**Acceptance Criteria:**
- [ ] Recurring Assignments store: `templateId` or `checklistId` (one required), `cronExpression`, `isOneTime`, `assignedToUserId` (nullable), `assignedToStoreId` (nullable), `targetStoreIds` (nullable array), `isActive`, `createdByUserId`, `lastFiredAt` (nullable)
- [ ] Background job evaluates active Recurring Assignments on a per-minute tick; uses cron expression to determine if now is a fire time
- [ ] On fire: one Task Instance created per target; SignalR broadcasts to affected store groups; FCM Standard push sent to assignee(s)
- [ ] `IsOneTime` assignments are deactivated immediately after first instantiation
- [ ] Store Manager can only create Recurring Assignments for their own store(s)
- [ ] Supervisor can create Recurring Assignments targeting any store in their region; `TargetStoreIds` must be within `supervisor.regionId`
- [ ] Recurring Assignments can be paused, resumed, and deleted
- [ ] Deleting a Recurring Assignment does not delete already-generated Task Instances
- [ ] The cron picker UI generates and validates the cron string before saving; invalid expressions are rejected with a user-readable error
- [ ] Ad hoc one-off task creation is a direct Task Instance creation (no Recurring Assignment row written) available from both desktop and PWA

**Key Entities:** `RecurringAssignments`, `Tasks` (instances)  
**API Surface:** CRUD for `/recurring-assignments`; `POST /tasks` (ad hoc direct creation); `POST /recurring-assignments/{id}/pause`; `POST /recurring-assignments/{id}/resume`  
**Out of scope:** Dependency chaining between Recurring Assignments, timezone per-store scheduling (V1 uses tenant timezone only)

---

### FD-07: Task Board — Field PWA

**Summary:** The primary field-facing surface. Store Employees see their personally assigned tasks and the store's open (unassigned) tasks in real-time. The board updates live via SignalR without page refresh. Tasks are grouped by Checklist, with standalone tasks below. Partial offline support via Angular Service Worker: the board loads from cache when offline; completions queue locally and sync on reconnect.

**User Stories:**
- As a **Store Employee**, I need to see all tasks assigned to me today — grouped by Checklist — so I know what I'm responsible for.
- As a **Store Employee**, I need to see open store-level tasks I can claim so I can volunteer for additional work.
- As a **Store Employee**, I need to complete a task by filling in its dynamic fields and submitting so the system records my compliance.
- As a **Store Employee**, when the store WiFi drops, I need my Task Board to still load and my submissions to queue so I'm not blocked mid-shift.
- As a **Store Manager**, I need to see the same Task Board for my store so I can monitor real-time progress during a shift.

**Task Board Layout:**
```
[ Store Name ]  [ Date ]  [ Shift Progress: 14/18 ]

▼ Opening Duties Checklist                    3/5 complete
   ✓ Parking Lot Sweep                        Completed — Maria
   ○ Temperature Check — Walk-In              Due 08:00 AM  [Complete]
   ○ Temperature Check — Prep Area            Due 08:00 AM  [Complete]
   ⚠ Till A Count                             OVERDUE       [Complete]
   ○ Till B Count                             Due 09:00 AM  [Complete]

▼ Open Store Tasks                            (unassigned, claimable)
   ○ Pepsi Cooler Rotation                    [Claim]
   ○ Lobby Audit                              [Claim]

▼ My Tasks
   ○ Dough Plan — 10/12 inch                  Due 10:00 AM  [Complete]
```

**Acceptance Criteria:**
- [ ] Task Board loads assigned tasks and store-level tasks for the current date
- [ ] Tasks grouped by Checklist (ordered); standalone tasks in a flat list below
- [ ] Checklist shows completion count (e.g., "3/5 complete") derived live
- [ ] Task statuses displayed with visual differentiation: Open, In Progress, Overdue (with elapsed time), Completed, Verified
- [ ] Overdue tasks surface prominently (highlighted, sorted to top of their group)
- [ ] "Claim" button on store-level tasks prompts: logged-in user auto-claimed, or volunteer name text entry
- [ ] Tapping/clicking a task opens the Task Detail view (dynamic field form)
- [ ] SignalR connection opens on board load; new tasks and status changes push in real-time without refresh
- [ ] Angular Service Worker caches the Task Board view for offline load
- [ ] Task completions submitted while offline are queued in `localStorage` and replayed in order on reconnect
- [ ] Offline indicator banner displays when connection is lost
- [ ] Store Manager sees a store-wide Task Board (all tasks, all employees) in addition to their personal view

**Key Entities:** `Tasks`, `TaskCompletions`, `Checklists`  
**API Surface:** `GET /stores/{id}/tasks/today`, `GET /tasks/{id}`, `POST /tasks/{id}/claim`  
**Out of scope:** Historical task board (past dates), task board filtering in V1 (V2)

---

### FD-08: Store Kiosk View

**Summary:** A shared device profile — a tablet or laptop at the store permanently logged in as the store account — that displays the full store task board. Any employee or volunteer walking by can view all tasks, claim an open one, and complete it. No individual login required. Financial and corrective action details are not displayed in this view.

**User Stories:**
- As a **walk-up Store Employee**, I need to see all open tasks on the kiosk and claim one with my name so the system records who completed it.
- As a **volunteer**, I need to enter my name in a text field to claim and complete a store-level task without having an OpsFlow account.
- As a **Store Manager**, I need the kiosk to show live task statuses so the whole team has ambient awareness of shift progress.

**Acceptance Criteria:**
- [ ] Kiosk account is a special `store_kiosk` session — no personal user account, scoped to a single store
- [ ] Displays all Task Instances for the store for today: assigned, unassigned, completed, overdue
- [ ] Claim flow: tap task → prompt "Enter your name or log in" → text field for name OR login button
- [ ] Claiming sets `CompletedByVolunteerName` (text) if no user account; `CompletedByUserId` if logged in
- [ ] Financial task details (Till counts, deposit amounts) are hidden on the kiosk view
- [ ] Corrective action text is shown to the claimant after field submission if a range is breached
- [ ] Board updates live via SignalR (same store group as the Field PWA)
- [ ] Session does not expire — designed for permanent display

**Key Entities:** `Tasks`, `TaskCompletions`  
**API Surface:** `GET /stores/{id}/tasks/today` (same endpoint as Task Board, role-filtered), `POST /tasks/{id}/claim`  
**Out of scope:** Kiosk TV idle/screensaver mode (V2), touch-optimised large-format display (V2)

---

### FD-09: Task Completion & Verification

**Summary:** A Task Completion is the record of a Store Employee (or volunteer) submitting values for all required fields in a Task. The system evaluates submitted values against the Template's range rules and surfaces corrective actions inline if any field is out of range. A Manager then Verifies the completion to close the loop. Photo evidence is uploaded directly to Azure Blob Storage via pre-signed SAS URL.

**Task State Machine:**
```
Open ──────────────────────────────────────► Cancelled (manager only)
  │                                          Deferred  (manager only, auto-resets to Open next day)
  ▼
In Progress
  │
  ├──► Overdue (deadline passed, background job promotes)
  │         │
  │         └──► Corrective Action Raised (grace period exceeded)
  │
  ▼
Completed (employee submits all required fields)
  │
  ▼
Verified (manager sign-off)
```

**User Stories:**
- As a **Store Employee**, I need to open a task, fill in all required fields, and submit so the task moves to Completed.
- As a **Store Employee**, when I submit a numeric value outside the defined range, I need to see the corrective action text immediately so I know what to do next.
- As a **Store Employee**, when a task requires a photo, I need to capture it from my camera and have it upload automatically before I can submit.
- As a **Store Manager**, I need to verify completed tasks to confirm the work meets standards.
- As a **Store Manager**, I need to cancel or defer a task with a mandatory reason so there is an audit trail.

**Acceptance Criteria:**
- [ ] Task Detail view renders all fields dynamically from the Template's `Fields` JSON definition
- [ ] Required fields block submission if empty; validation runs client-side and server-side
- [ ] On submission: `TaskCompletion` row created; Task status transitions to `Completed`
- [ ] Range evaluation runs server-side on submission: if any `Numeric` field value is outside `[rangeMin, rangeMax]`, the corrective action text is returned in the API response and displayed to the employee
- [ ] `Boolean` field set to `false` surfaces the field's `correctiveActionText`
- [ ] `Checklist` field: unchecked required sub-items block submission
- [ ] Photo fields: client requests SAS URL (`GET /tasks/{id}/photo-upload-url`), uploads directly to Blob Storage, submits blob URL as field value
- [ ] `POST /tasks/{id}/complete` is idempotent — submitting twice returns the existing completion
- [ ] `POST /tasks/{id}/verify` available to `store_manager` and above; transitions task to `Verified`
- [ ] `POST /tasks/{id}/cancel` requires `reason: string`; available to `store_manager` and above; terminal state
- [ ] `POST /tasks/{id}/defer` requires `reason: string`; sets `deferredTo: date`; a background job resets the task to `Open` at 06:00 AM on `deferredTo`
- [ ] `CompletedByUserId` is set to the authenticated user; `CompletedByVolunteerName` is set for kiosk/volunteer submissions
- [ ] All completion events broadcast via SignalR to the store group
- [ ] Manager receives FCM Standard push when a task assigned to their store is completed

**Key Entities:** `Tasks`, `TaskCompletions`, `TaskCompletionFieldValues`  
**API Surface:** `GET /tasks/{id}`, `POST /tasks/{id}/complete`, `POST /tasks/{id}/verify`, `POST /tasks/{id}/cancel`, `POST /tasks/{id}/defer`, `GET /tasks/{id}/photo-upload-url`  
**Out of scope:** Completion history audit log UI (data exists; UI deferred to V2)

---

### FD-10: Corrective Actions

**Summary:** Corrective Actions are not a separate entity — they are pre-authored remediation instructions embedded in Task Template field definitions. They surface automatically at completion time when a field value breaches its range or a boolean is set to false. The system records that a corrective action was triggered as part of the `TaskCompletion` record for audit and reporting purposes.

**User Stories:**
- As a **Store Manager authoring a template**, I need to write a corrective action for a temperature field so that if a value exceeds 56°F, the employee sees exactly what to do.
- As a **Store Employee completing a task**, I need to see the corrective action text immediately after submitting an out-of-range value so I can act without waiting for a manager.
- As a **Supervisor**, I need to see which tasks triggered corrective actions this week so I can identify systemic compliance failures.

**Acceptance Criteria:**
- [ ] `TaskTemplateField.correctiveActionText` is a nullable string — authored at template creation time
- [ ] On task completion, server evaluates every field value against its range/boolean rule
- [ ] If triggered: `TaskCompletion.correctiveActionsTriggered` (JSON array) records which fields fired and what text was shown
- [ ] The completion API response includes `triggeredCorrectiveActions: [{fieldName, text}]` so the client can display them
- [ ] The Task Board marks completed tasks that triggered corrective actions with a distinct visual indicator (e.g., amber badge)
- [ ] The Manager Dashboard surfaces tasks with triggered corrective actions as a filterable list

**Key Entities:** `TaskTemplateFields` (embedded), `TaskCompletions.correctiveActionsTriggered`  
**Out of scope:** Corrective actions requiring a secondary sign-off flow (V2), corrective action resolution tracking (V2)

---

### FD-11: Manager Walk

**Summary:** A Manager Walk is a live, session-based store audit. The manager opens a Walk Session against a store using a Walk Template (which follows the same 3-tier scope as Task Templates). They work through scored audit items in real-time, capture photo evidence per item, and submit the session. On submission, the system calculates a composite score, stores the report, and automatically generates corrective action Tasks for any items that failed.

**Walk Template Structure:**
- `WalkTemplate`: name, scope (System/Regional/Store), category, audit items array
- `WalkAuditItem`: label, description, `scoringType` (`Pass/Fail` or `1–5 scale`), `weight` (for composite score), `photoRequired: boolean`, `failCorrectiveActionText`

**User Stories:**
- As a **Store Manager**, I need to start a Walk Session for my store using an audit template so I can systematically evaluate standards during an on-site or remote visit.
- As a **Store Manager**, I need to capture a photo for each failing audit item so there is visual evidence of the issue.
- As a **Supervisor**, I need to review Walk Session reports for all stores in my region so I can track audit scores over time.
- As the system, when a Walk Session is submitted with failed items, I need to automatically generate corrective action Tasks and assign them to the store so nothing falls through the cracks.

**Acceptance Criteria:**
- [ ] `POST /walk-sessions` creates a session with `storeId`, `templateId`, `startedAt`, `conductedByUserId`
- [ ] Session contains one `WalkSessionItem` per template audit item: `status` (Pending/Pass/Fail/NA), `score` (nullable int), `notes` (nullable text), `photoBlobUrl` (nullable)
- [ ] Manager works through items sequentially or in any order; progress is auto-saved (PATCH on each item)
- [ ] Photo upload uses same SAS URL pattern as Task Completion
- [ ] `POST /walk-sessions/{id}/submit` closes the session: calculates composite score, stores `completedAt`
- [ ] On submit: for each failed item with `failCorrectiveActionText`, a Task Instance is created and assigned to the store (`AssignedToStoreId`) with a due date of end-of-day
- [ ] SignalR broadcasts new corrective Tasks to the store group on submit
- [ ] FCM Standard push sent to Store Manager: "Walk session completed — [N] corrective tasks generated"
- [ ] Walk Session report is viewable on the dashboard: score, item-by-item breakdown, photos, generated tasks
- [ ] Supervisor can view all Walk Sessions for stores in their region, filterable by date range and score threshold
- [ ] Walk Templates follow same 3-tier scope as Task Templates; same creation permissions apply

**Key Entities:** `WalkTemplates`, `WalkAuditItems`, `WalkSessions`, `WalkSessionItems`  
**API Surface:** CRUD for `/walk-templates`; `POST /walk-sessions`, `PATCH /walk-sessions/{id}/items/{itemId}`, `POST /walk-sessions/{id}/submit`, `GET /walk-sessions/{id}`, `GET /stores/{id}/walk-sessions`, `GET /walk-sessions/{id}/photo-upload-url`  
**Out of scope:** Walk session scheduling/recurring walks (V2), comparative benchmarking across sessions (V2)

---

### FD-12: MDOG & Inventory

**Summary:** The MDOG (Master Daily Operational Guide) tracks daily on-hand inventory counts. Store Employees record counts via the standard Task completion flow (using a System-level MDOG template). Submitted counts are persisted as `InventorySnapshots` per item per store per date. The next day's MDOG task form pre-populates from the previous snapshot, eliminating manual carry-forward. The 56-Degree Rule is a range constraint on temperature fields.

**The 3-Day Dough Plan:**
The MDOG template includes numeric fields for each dough size (10", 12", 14", 16"). Each field records "On-Hand" count. The template's `correctiveActionText` per field encodes the reorder instruction. The "Day 2 / Day 3 Need" calculation is a derived display: configured need targets per size are stored in `StoreSettings` and compared to the submitted on-hand count to display surplus/deficit in the Task Detail view.

**User Stories:**
- As a **Store Employee**, I need to record today's dough on-hand counts for all four sizes so the system can flag any that are below the 3-day need.
- As a **Store Employee**, when I open today's MDOG task, I need yesterday's counts pre-populated so I only need to update changes.
- As a **Store Manager**, I need to configure the Day 2 and Day 3 need targets per dough size in Store Settings so the system can calculate surplus/deficit accurately.
- As a **Store Manager**, when an on-hand count is below the 3-day need, I need to see the corrective action text ("Place emergency order") so I can act immediately.

**56-Degree Rule:**
- A `Numeric` field on relevant inventory tasks has `rangeMax: 56` (°F)
- `correctiveActionText`: "Product temperature exceeds 56°F — return to refrigeration immediately"
- This is a System-level template rule; Store Managers cannot override it

**Acceptance Criteria:**
- [ ] System-level MDOG Task Template exists with fields for each tracked inventory item (seeded at tenant provisioning)
- [ ] On task submission, server writes `InventorySnapshot` records: `{storeId, date, itemKey, onHandCount, submittedByUserId}`
- [ ] Next day's MDOG task form pre-populates each field from the most recent `InventorySnapshot` for that item/store
- [ ] Task Detail view shows "Day 2 Need" and "Day 3 Need" targets alongside the on-hand input, derived from `StoreSettings.doughNeedTargets`
- [ ] Surplus/deficit displayed as a colour-coded indicator (green = surplus, red = deficit)
- [ ] 56-Degree Rule enforced as a range constraint on temperature fields — cannot be disabled by Store Managers
- [ ] Store Manager can view inventory snapshot history for their store (last 7 days)
- [ ] MDOG task follows standard task lifecycle (Open → Completed → Verified)

**Key Entities:** `InventorySnapshots`, `StoreSettings` (dough need targets), System `TaskTemplates` (MDOG)  
**API Surface:** `GET /stores/{id}/inventory/latest`, `GET /stores/{id}/inventory/history`; snapshot writes handled by `POST /tasks/{id}/complete`  
**Out of scope:** Automated supplier ordering, POS integration, cost calculations

---

### FD-13: Safe, Till & Deposit Log

**Summary:** Financial compliance tracking for the store safe. Till A and Till B counts are captured via a System-level task template (standard task flow, with range validation against the configured base amount and variance recording). The Bank Deposit is a separate, immutable `DepositLog` record with a hard 09:30 AM deadline and a 10:00 AM supervisor escalation.

**F0890 Compliance Rules (Hard-coded):**
- Till A base: $50 or $75 (configured per store in `StoreSettings`)
- Till B base: $50 or $75 (configured per store in `StoreSettings`)
- Variance must be recorded with Manager initials if count differs from base
- Bank Deposit must be submitted by **09:30 AM**
- If deposit is not recorded by **10:00 AM**, a `Critical` FCM push is sent to the Supervisor

**User Stories:**
- As a **Store Manager**, I need to count Till A and Till B and record any variance with my initials so the daily safe record is complete.
- As a **Store Manager**, I need to record the bank deposit by 09:30 AM so the system marks compliance.
- As a **Supervisor**, I need to be alerted by 10:00 AM if a deposit has not been recorded so I can follow up immediately.
- As an **Admin**, I need to view the Deposit Log for any store and any date so I have an immutable financial audit trail.

**Acceptance Criteria:**
- [ ] System-level "Till Count" Task Template with fields: `TillACount` (Numeric, range: base ± tolerance), `TillBCount` (Numeric), `VarianceNote` (Text, required if out of range), `ManagerInitials` (Text, required if out of range)
- [ ] Till base amounts configured per store in `StoreSettings`; range tolerance configurable by Admin
- [ ] `POST /stores/{id}/deposit-log` creates a `DepositLog` record: `{storeId, amount, submittedByManagerId, submittedAt}`
- [ ] `DepositLog` records are immutable — no update or delete endpoints
- [ ] Background job checks at 10:00 AM: if no `DepositLog` record exists for today for an active store, send `Critical` FCM push to the Supervisor for that store's region
- [ ] SignalR broadcasts deposit confirmation to the store group (Standard priority)
- [ ] `GET /stores/{id}/deposit-log` returns paginated deposit history; available to `store_manager` and above
- [ ] Admin can view deposit log across all stores
- [ ] Till Count task follows standard task lifecycle; variance triggers corrective action text

**Key Entities:** `DepositLog`, `StoreSettings`, System `TaskTemplates` (Till Count)  
**API Surface:** `POST /stores/{id}/deposit-log`, `GET /stores/{id}/deposit-log`, `GET /stores/{id}/deposit-log/{date}`  
**Out of scope:** Bank reconciliation, accounting system integration, digital safe integration

---

### FD-14: Red Book [PLACEHOLDER]

**Summary:** The Red Book is the structured asynchronous communication log for manager-to-manager shift handovers. Full specification to be provided by the product owner.

**Known constraints from Shared Design Concept:**
- Each entry is tied to a shift (Morning / Afternoon / Evening)
- Mandatory `category` field: `Staffing | Equipment | Customer | Operational | Other`
- Entries can optionally reference a Task Instance or Corrective Action
- Scoped to a store; Supervisors can read Red Book entries across all stores in their region
- Store Employees do not have access to the Red Book
- In-app reply threads are out of scope for V1

**Acceptance Criteria:** _To be defined when full Red Book specification is received._

**Key Entities:** `RedBookEntries` (structure TBD)  
**API Surface:** TBD  
**Tracer Bullet note:** Red Book is an independently deployable domain. Its absence does not block any other Feature Domain. It should be scheduled as a later Tracer Bullet once the spec is complete.

---

### FD-15: Notification System

**Summary:** OpsFlow pushes notifications via two channels: SignalR (in-app, real-time, requires active connection) and FCM (push, reaches backgrounded or closed PWA). All FCM notifications are categorised as `Critical` or `Standard` to control delivery priority. FCM device tokens are stored per user per device in the database and refreshed on each PWA load.

**Event → Notification Map:**

| Event | Channel | Priority | Recipient |
|-------|---------|----------|-----------|
| New task assigned to user | SignalR + FCM | Standard | Assignee |
| Task assigned to store | SignalR | Standard | Store group |
| Task overdue | SignalR + FCM | Standard | Store Manager |
| Corrective action triggered | SignalR | Standard | Store group |
| Bank deposit not recorded by 10:00 AM | FCM | Critical | Supervisor |
| Temperature out of range | FCM | Critical | Store Manager |
| Walk Session completed | SignalR + FCM | Standard | Store Manager |
| Walk Session generates corrective Tasks | SignalR | Standard | Store group |
| Red Book entry posted | FCM | Standard | Incoming manager (shift-aware) |
| Task verified | SignalR | Standard | Assignee |

**Acceptance Criteria:**
- [ ] Angular Service Worker registers with FCM on first load; token stored via `POST /notifications/register-token`
- [ ] FCM tokens refreshed on every PWA load; stale tokens are cleaned up server-side on FCM delivery failure
- [ ] `Critical` notifications sent with FCM `priority: "high"` — bypasses device battery optimisation
- [ ] `Standard` notifications sent with FCM default priority
- [ ] All SignalR events follow the store-group pattern: `store-{storeId}`; regional events use `region-{regionId}`
- [ ] The .NET `NotificationService` is the single point of dispatch — no feature handler sends notifications directly
- [ ] Background job for deposit escalation runs at 10:00 AM daily (Quartz.NET scheduled job)
- [ ] Notification delivery failures are logged; no retry mechanism in V1

**Key Entities:** `FcmDeviceTokens` (`userId`, `token`, `deviceType`, `lastRefreshedAt`)  
**API Surface:** `POST /notifications/register-token`, `DELETE /notifications/register-token`  
**Out of scope:** User-configurable notification preferences (V2), notification history/inbox (V2), email notifications (V2)

---

### FD-16: Dashboards

**Summary:** Four distinct dashboard surfaces, each tailored to its audience. All dashboards show today's data by default. Historical trending is out of scope for V1.

#### 16a — Store Employee Dashboard (Field PWA)

| Section | Content |
|---------|---------|
| My Tasks | Personally assigned tasks for today — grouped by Checklist |
| Open Store Tasks | Unassigned store-level tasks available to claim |
| My History | Personal task completion record for the last 7 days |
| Store Progress | Today's overall store task completion percentage (read-only) |

**Acceptance Criteria:**
- [ ] My Tasks and Open Store Tasks update in real-time via SignalR
- [ ] My History fetched on page load; shows date, task name, completion time, status
- [ ] Store Progress shows `completedCount / totalCount` as a progress bar
- [ ] Financial data (Till, Deposit) hidden from Store Employee view entirely

#### 16b — Store Kiosk Dashboard

| Section | Content |
|---------|---------|
| Full Store Task Board | All tasks for today — assigned, unassigned, all statuses |
| Claim Flow | Tap task → volunteer name entry or employee login |

**Acceptance Criteria:**
- [ ] All task statuses visible except financial task details (amounts hidden)
- [ ] Live updates via SignalR
- [ ] Session never expires

#### 16c — Store Manager Dashboard (Desktop)

| Section | Content |
|---------|---------|
| Today's Snapshot | Completion rate %, open count, overdue count, active corrective action count |
| Active Corrective Actions | List of tasks with triggered corrective actions, grouped by category |
| Open Overdue Tasks | Tasks past deadline, sortable by elapsed time |
| Walk Frequency | Days since last Manager Walk for this store |
| Unread Red Book Entries | Entries since last login, shift-filtered |
| My Stores Overview | If multi-store: per-store snapshot cards |

**Acceptance Criteria:**
- [ ] All metrics derived from today's Task Instance data — no pre-aggregated tables in V1
- [ ] Overdue tasks link directly to Task Detail view
- [ ] Walk Frequency shows "Last walk: X days ago" or "No walks recorded"
- [ ] Multi-store Store Managers see a card per assigned store

#### 16d — Supervisor Dashboard (Desktop)

| Section | Content |
|---------|---------|
| Regional Leaderboard | All stores ranked by composite score (completion rate + corrective action resolution + walk frequency) |
| Critical Alerts | Stores with missed deposits today, critical FCM events fired |
| Regional Completion Rate | Aggregate completion % across all stores in region |
| Walk Coverage | Stores with no Manager Walk in last 7 days |
| Stores Drill-Down | Click any store → opens that store's Store Manager dashboard view |

**Acceptance Criteria:**
- [ ] Leaderboard composite score formula: `(completionRate * 0.5) + (correctiveActionRate * 0.3) + (walkFrequencyScore * 0.2)` — configurable weights in V2
- [ ] Critical Alerts panel shows real-time FCM Critical events for today
- [ ] Store drill-down does not require a separate login or role switch

#### 16e — Admin Dashboard (Desktop)

| Section | Content |
|---------|---------|
| System Health | System-wide task completion rate, stores with critical escalations today |
| Missed Deposits Today | All stores with no deposit log entry by 10:00 AM |
| Regional Summary | Per-region completion rate cards |
| Active Escalations | Critical FCM events fired in last 24 hours across all stores |

**Acceptance Criteria:**
- [ ] Admin dashboard aggregates across all regions — no scoping required
- [ ] Missed Deposits panel is a mandatory widget (not dismissible)
- [ ] All data is today-scoped; no historical date picker in V1

**Key Entities:** `Tasks`, `TaskCompletions`, `WalkSessions`, `DepositLog`, `RedBookEntries`  
**API Surface:** `GET /dashboard/store/{id}`, `GET /dashboard/region/{id}`, `GET /dashboard/system`  
**Out of scope:** Historical trend charts (V2), custom widget configuration (V2), data export (V2)

---

### FD-17: Admin Panel

**Summary:** The Admin Panel is the settings and management console for the tenant. It is accessible only to users with the `admin` role. Multiple Admin accounts are supported per tenant.

**Sections:**

| Section | Capabilities |
|---------|-------------|
| User Management | Create, edit, deactivate users; assign roles; assign to stores/regions |
| Store Management | Create, edit, deactivate stores; assign to regions |
| Region Management | Create, edit, deactivate regions |
| System Templates | Author and manage System-scope Task Templates and Walk Templates |
| Store Settings | Configure Till base amounts, dough need targets, tenant timezone |
| Tenant Settings | Tenant name, logo, primary contact |

**Acceptance Criteria:**
- [ ] All Admin Panel routes are guarded by `role === 'admin'`
- [ ] User creation form enforces role-appropriate assignment (store_employee requires storeId, etc.)
- [ ] Deactivating a store deactivates all active Recurring Assignments for that store; historical data retained
- [ ] Store Settings: Till base amount (A and B), dough need targets per size (10/12/14/16"), tenant timezone
- [ ] System Templates authoring uses the same template builder as FD-04 (shared `libs/ui` component)
- [ ] Admin can create additional Admin accounts — no limit enforced

**Key Entities:** All entities (Admin has full read/write)  
**API Surface:** Admin-scoped endpoints on all existing resource routes; `GET /admin/audit-log` (V2)  
**Out of scope:** Billing management, multi-tenant admin console (super-admin), audit log UI (V2)

---

## 6. Data Model — Key Entities

This section defines the primary tables and their relationships. Column-level detail is specified at implementation time per Tracer Bullet.

```
Tenants (master DB)
  └── (each tenant has its own database)

Regions
  └── Stores
        └── Users (store_employee: StoreId FK)
        └── UserStoreAssignments (store_manager multi-store)

TaskTemplates [scope: System|Regional|Store]
  └── TaskTemplateFields (JSON or child table)
        └── rangeMin, rangeMax, correctiveActionText, fieldType

Checklists [scope: System|Regional|Store]
  └── ChecklistTemplateItems (ordered: checklistId + templateId + order)

RecurringAssignments
  └── templateId (FK) OR checklistId (FK)
  └── cronExpression, isOneTime, isActive
  └── assignedToUserId (nullable) | assignedToStoreId (nullable) | targetStoreIds (JSON array)

Tasks (instances)
  └── templateId (nullable FK)
  └── checklistInstanceId (nullable FK)
  └── status: Open|InProgress|Overdue|Completed|Verified|CorrectiveActionRaised|Cancelled|Deferred
  └── assignedToUserId (nullable) | assignedToStoreId (nullable)
  └── dueAt, deferredTo (nullable)

TaskCompletions
  └── taskId (FK)
  └── completedByUserId (nullable) | completedByVolunteerName (nullable)
  └── fieldValues (JSON)
  └── correctiveActionsTriggered (JSON)

InventorySnapshots
  └── storeId, date, itemKey, onHandCount, submittedByUserId

DepositLog
  └── storeId, amount, submittedByManagerId, submittedAt, confirmedAt
  └── IMMUTABLE — no updates or deletes

WalkTemplates [scope: System|Regional|Store]
  └── WalkAuditItems (scoringType, weight, photoRequired, failCorrectiveActionText)

WalkSessions
  └── templateId, storeId, conductedByUserId, startedAt, completedAt, compositeScore
  └── WalkSessionItems (auditItemId, status, score, notes, photoBlobUrl)

RedBookEntries [PLACEHOLDER]
  └── storeId, shift, category, body, taskId (nullable), authorId, createdAt

FcmDeviceTokens
  └── userId, token, deviceType, lastRefreshedAt

StoreSettings
  └── storeId, tillABase, tillBBase, doughNeedTargets (JSON), timezoneId
```

---

## 7. Non-Functional Requirements

### Performance
- Task Board initial load: < 2 seconds on 4G connection
- SignalR event delivery: < 500ms from server event to UI update
- API response time (p95): < 300ms for all read endpoints under normal load
- Photo upload: handled client-to-Azure-Blob directly; API is not in the upload path

### Security
- All API endpoints require JWT authentication except `POST /auth/login` and `POST /auth/refresh`
- Access tokens stored in memory only (not `localStorage` or `sessionStorage`)
- Refresh tokens stored in `HttpOnly` cookies
- Azure Blob SAS URLs expire in 15 minutes
- All tenant data access routes through `TenantDbContextFactory` — no raw SQL with tenant filters
- HTTPS enforced on all endpoints; HTTP redirected
- OWASP Top 10 compliance required; security review before production deployment

### Scalability
- Database-per-tenant architecture supports independent scaling per client
- SignalR backplane: Azure SignalR Service (not in-process) to support horizontal API scaling
- Quartz.NET jobs run on a single designated node in V1 (distributed job scheduling in V2)

### Availability
- Target: 99.5% uptime (Azure App Service SLA)
- Partial offline on Field PWA covers short outages without user impact

### Accessibility
- WCAG 2.1 AA compliance for all UI components
- Minimum touch target size: 44×44px on PWA
- High-contrast status indicators (not colour-only)

### Browser/Device Support
- Field PWA: Chrome on Android (latest), Safari on iOS 16+
- Desktop Dashboard: Chrome, Edge, Safari (latest two versions each)
- PWA install prompt supported on Android Chrome

---

## 8. V1 Out-of-Scope (Hard Boundary)

The following are explicitly excluded from V1. Any agent or developer who identifies a requirement touching these areas must flag it as out-of-scope before implementing.

| Feature | Target Version |
|---------|---------------|
| Gamification (points, streaks, leaderboards) | V2 |
| User-configurable notification preferences | V2 |
| Walk template custom scoring weight configuration | V2 |
| Kiosk TV idle/screensaver display mode | V2 |
| CSV / PDF compliance report exports | V2 |
| Third-party integrations (POS, payroll, scheduling systems) | V2 |
| In-app reply threads on Red Book entries | V2 |
| Full offline sync (two-way conflict resolution) | V2 |
| Native mobile app (iOS/Android via Capacitor) | V2 |
| Multi-language / i18n support | V2 |
| Billing / subscription management | V2 |
| Advanced historical trend charts and analytics | V2 |
| Custom dashboard widget configuration | V2 |
| Audit log UI | V2 |
| Distributed Quartz.NET job scheduling | V2 |
| E2E Playwright tests | V2 |
| Completion history audit log UI | V2 |
| Walk session scheduling / recurring walks | V2 |
| Template versioning | V2 |
| Timezone per-store scheduling (V1 uses tenant timezone) | V2 |
| Notification retry mechanism | V2 |
| Self-serve tenant onboarding | V2 |

---

## 9. Definition of Done

V1 is considered **shipped** when all of the following are true:

### Functional
- [ ] All Feature Domains FD-01 through FD-13 and FD-15 through FD-17 have passing integration tests
- [ ] Red Book (FD-14) spec received, implemented, and tested
- [ ] All acceptance criteria in Section 5 are verifiable via automated test or documented manual QA step
- [ ] The 10:00 AM deposit escalation fires correctly in a staging environment with a real Quartz.NET job
- [ ] The 56-Degree Rule corrective action surfaces correctly on the Task Board after a temperature field breach
- [ ] Partial offline: Task Board loads from cache with no network; queued submissions sync on reconnect
- [ ] Multi-tenant isolation verified: integration test confirms tenant A cannot read tenant B's data

### Quality
- [ ] All .NET Vertical Slice handlers have unit test coverage for business logic branches
- [ ] All `libs/data-access` Angular services have unit tests with mocked HTTP
- [ ] Zero TypeScript `any` types in `libs/` (enforced by `tsconfig` strict mode)
- [ ] Zero broken Nx dependency graph boundaries (`nx graph` shows no violations)
- [ ] OWASP Top 10 security review completed and findings addressed

### Infrastructure
- [ ] Azure App Service, Azure SQL, Azure Static Web Apps, and Blob Storage provisioned in production
- [ ] Azure SignalR Service connected (not in-process SignalR)
- [ ] EF Core migration orchestrator runs cleanly against all tenant databases
- [ ] FCM Web Push delivers to both Android Chrome and iOS Safari in staging
- [ ] CI/CD pipeline (GitHub Actions or Azure DevOps) runs tests on every PR and deploys on merge to `main`

### UX (Manual QA — Human Architect Sign-Off Required)
- [ ] Store Employee can complete a full shift: claim tasks, fill dynamic fields, submit photo evidence, see corrective action, view store progress
- [ ] Store Manager can complete a Manager Walk end-to-end: start session, score items, capture photos, submit, confirm corrective Tasks appear on Task Board
- [ ] Supervisor can create a multi-store Recurring Assignment and confirm Task Instances appear on the correct store boards
- [ ] Admin can provision a new store, create a user, assign them to the store, and confirm the user can log in and see their Task Board
- [ ] No instance of App Slop accepted at sign-off — taste review is mandatory

---

*This document is the written record of the Shared Design Concept reached on 2026-06-02. Any deviation from the decisions in Section 3 requires a formal PRD amendment. Agents implementing from this document must treat Section 8 as a hard guard — if a task touches an out-of-scope item, stop and flag before proceeding.*
