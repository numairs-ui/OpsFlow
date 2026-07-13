# OpsFlow — Product Requirements Document (V2, As-Built)

**Version:** 2.0
**Status:** Current — reflects the system as built after the July 2026 post-audit release
**Date:** 2026-07-13
**Supersedes:** OpsFlow_PRD_V1.md (V1.1, 2026-06-09)
**Companion doc:** OpsFlow_Release_Notes_2026-07.md (plain-language "what's new")

> **How V2 differs from V1.1.** V1.1 was the *design spec*. A PRD audit found several V1.1 requirements were never built, some were built differently, and one whole domain (Manager Walk) was deliberately replaced. V2 is the **as-built** record: it describes what the system actually does today. Sections that changed materially are marked **⟳ Changed** with a short "V1.1 → V2" note; unchanged domains are summarized and carry forward from V1.1.

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [What Changed in V2 (Change Log)](#2-what-changed-in-v2-change-log)
3. [Architecture (Locked)](#3-architecture-locked)
4. [Roles & Permissions Matrix](#4-roles--permissions-matrix)
5. [Feature Domains](#5-feature-domains)
6. [Data Model — Key Entities](#6-data-model--key-entities)
7. [Non-Functional Requirements](#7-non-functional-requirements)
8. [Out-of-Scope / Deferred](#8-out-of-scope--deferred)
9. [Open Follow-Ups](#9-open-follow-ups)

---

## 1. Executive Summary

OpsFlow is a multi-tenant operational-compliance platform for franchise restaurant operators. It replaces paper checklists, WhatsApp threads, and spreadsheets with a structured, real-time system for task management, scored audits, inventory compliance, financial record-keeping, and approval workflows.

**Two surfaces, one API:**
- **Field PWA** (`field-pwa`) — Angular app for store employees and the shared kiosk: task board, task completion, scored checklist sessions, photo capture.
- **Dashboard** (`dashboard`) — Angular app for managers, supervisors, and admins: authoring (templates, checklists, recurring assignments, forms), dashboards, user/store/region administration.
- **API** — .NET 9 Vertical-Slice API, EF Core, SignalR, Quartz background jobs. Runs on Azure Container Apps (prod) with a Supabase/Postgres or SQL Server data layer selectable per environment.

**The core model (V2):** **Task is the single execution primitive.** A `TaskInstance` moves through `Pending → InProgress → Completed → Verified` (plus Cancel/Defer). A task may be backed by a checklist, by a single template, or by nothing (notes-only). Checklists add **scoring** on top of tasks: a completed scored checklist yields a composite score and auto-generates corrective follow-up tasks for failed items. This unified model replaced the separately-specced "Manager Walk" domain.

**Who uses it:** Store Employee, Store Manager, Supervisor (regional), Admin (owner, region-scoped or global), Super Admin (network-wide), and the shared Store Kiosk profile.

---

## 2. What Changed in V2 (Change Log)

The July 2026 release delivered 9 workstreams. Each maps to feature domains below.

| # | Change | Domains | V1.1 status |
|---|--------|---------|-------------|
| A1 | **Standalone tasks** — tasks can exist without a checklist (single-template or notes-only) | FD-05, FD-07, FD-09 | Specced (nullable `ChecklistId`) but not implemented |
| A2 | **Checklist scoring + full editor** — per-item scoring config; admin checklist screen is now editable | FD-05, FD-17 | Not specced (was "Walk Template") |
| A3 | **Scored checklist sessions** — composite score computed on completion | FD-05, FD-09 | Was FD-11 "Manager Walk" (separate domain) |
| A4 | **Auto-corrective tasks** — failed scored items spawn claimable follow-up tasks | FD-05, FD-10 | Was part of FD-11 |
| A5 | **Unified Create entry point** | FD-07, FD-17 | Not specced |
| A6 | **Import pipeline rework** — checklist imports build real scored checklists; fixed fake-success bug | FD-17 | Partially specced; buggy |
| B1 | **Kiosk session refresh** — kiosk survives token expiry | FD-08 | Bug |
| B2 | **Admin password reset** — one-time temp password, no email | FD-02 | Specced as email flow |
| B3 | **Missed-deposit dashboard flag** — daily job flags stores past deadline | FD-13, FD-16 | Specced as 10:00 AM FCM push |
| B4 | **Multi-store recurring broadcast** — one assignment → many stores | FD-06 | Specced but single-store only in code |
| B5 | **Photo upload** — real capture + direct-to-storage upload | FD-04, FD-09 | Specced; placeholder in UI |
| B6 | **Role dashboards** — admin/employee/kiosk dashboards made real | FD-16 | Partially built |

**Retired from the spec:** the standalone **Manager Walk** domain (FD-11) and its `WalkTemplates`/`WalkSessions` entities are **not built** — the capability is delivered through scored Checklists instead. **FCM push notifications** are not part of this release; real-time delivery is SignalR-only, and deposit escalation is dashboard-flag-only.

---

## 3. Architecture (Locked)

Unchanged from V1.1. Summary:

- **Frontend:** Angular (standalone components, Signals), Nx monorepo, two apps (`field-pwa`, `dashboard`) consuming shared `libs/data-access`, `libs/ui`, `libs/util`.
- **Backend:** .NET 9, Vertical-Slice Architecture (`Features/{Domain}/{Action}/`), MediatR, FluentValidation, EF Core 9 (`UseNpgsql` / `UseSqlServer` via `DATABASE_PROVIDER`).
- **Auth:** JWT access (15 min) + refresh (7 days). Claims: `sub`, `tenantId`, `role`, `storeId?`, `regionId?` (multi-region admins carry a set).
- **Real-time:** SignalR, store-scoped groups (`store-{storeId}`).
- **Background jobs:** Quartz.NET (`GenerateTaskInstancesJob`, `OverduePromotionJob`, `ActivateDeferredTasksJob`, and new `DepositEscalationJob`).
- **Storage:** `IStorageProvider` with Azure Blob and Supabase implementations; signed-URL uploads.
- **Authorization:** one `ScopeSpec` primitive derives every caller's reach (global / region-set / store); handlers assert through it. Region scope is a **set** (an admin can own several regions).

---

## 4. Roles & Permissions Matrix

**Roles:** `super_admin`, `admin` (region-scoped or global), `supervisor` (single region), `store_manager` (one or more stores), `store_employee` (one store), `store_kiosk` (shared store device).

⟳ **Changed vs V1.1:** admin can now view the system dashboard (scoped to its regions); multi-store recurring broadcast is a real capability; password reset is admin-triggered; scoring/editing checklists is a manager+ capability.

| Capability | Employee | Kiosk | Manager | Supervisor | Admin | Super Admin |
|---|:-:|:-:|:-:|:-:|:-:|:-:|
| View / complete own tasks | ✓ | — | ✓ | ✓ | ✓ | ✓ |
| Claim & complete open store task | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Create one-time task (own store) | — | — | ✓ | ✓ | ✓ | ✓ |
| Complete a **scored checklist** session | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Author / edit checklist + scoring | — | — | ✓ (store) | ✓ (region) | ✓ | ✓ |
| Create Task Template (Store / Regional / System) | — | — | Store | +Regional | +Regional | All |
| Create Recurring Assignment (own store) | — | — | ✓ | ✓ | ✓ | ✓ |
| **Recurring broadcast to multiple stores** | — | — | — | ✓ (in region) | ✓ (in regions) | ✓ |
| Cancel / defer / verify a task | — | — | ✓ | ✓ | ✓ | ✓ |
| Record / view Deposit Log | — | — | ✓ | ✓ | ✓ | ✓ |
| View store dashboard | ✓ | ✓ (board) | ✓ | ✓ | ✓ | ✓ |
| View region dashboard | — | — | — | ✓ | ✓ | ✓ |
| **View system dashboard** | — | — | — | — | ✓ (own regions) | ✓ (all) |
| Submit a Form / act on approval steps | ✓ | — | ✓ | ✓ | ✓ | ✓ |
| **Reset a user's password** | — | — | — | — | ✓ (own regions) | ✓ |
| Import templates from JSON | — | — | — | — | — | ✓ |
| Manage users / stores / regions | — | — | — | — | ✓ (own regions) | ✓ |
| Tenant settings | — | — | — | — | — | ✓ |

---

## 5. Feature Domains

### FD-01 Multi-Tenancy — *unchanged*
Per-tenant isolated database; tenant resolved from the JWT `tenantId` claim; all data access routed through `TenantDbContextFactory`. No cross-tenant access. Provisioning is a CLI/admin operation.

### FD-02 Authentication & Authorization — ⟳ Changed
JWT access + refresh as in V1.1. Route guards enforce role access. Denied scope maps to **401** (per the global exception handler).

**⟳ Password reset (B2):** *V1.1 specced an email one-time-link flow. V2 is admin-triggered, no email:*
- `POST /users/{userId}/reset-password` (admin/super_admin; region-scoped admins limited to users within their regions, mirroring UpdateUser's containment check).
- Generates a random temp password satisfying the identity policy (upper/lower/digit/symbol), or accepts an explicit one; returns it **once** in the response — never logged, never stored.
- Outstanding refresh tokens for that user are revoked so old sessions can't survive the change.
- Dashboard: a **Reset Password** row action on the Users screen shows the temp password in a one-time modal for manual handoff.

*Deferred:* self-service / email-based reset.

### FD-03 Store, Region & User Management — *unchanged (+ store reactivate)*
Admins manage regions, stores, users. Store deactivate now has a region-scope check and a matching **reactivate** endpoint (`POST /stores/{id}/reactivate`).

### FD-04 Task Template System — ⟳ Changed (Photo)
Reusable field blueprints at System / Regional / Store scope; field types `Numeric` (range + corrective), `Boolean` (corrective on "No"), `Text`, `Photo`, `Checklist` (sub-items).

**⟳ Photo fields (B5):** the `Photo` field type is now **fully wired end-to-end** (was a placeholder). See FD-09.

### FD-05 Checklist System — ⟳ Changed (major: scoring, sessions, standalone)
A Checklist is an ordered grouping of template-backed items. **V2 makes checklists the scored-audit tool** and decouples tasks from checklists.

**⟳ Standalone tasks (A1):** `TaskInstance.ChecklistId` is **nullable**. A task is one of:
- **Checklist-backed** — original behavior; can be scored (below).
- **Single-template** — references one `TaskTemplate` directly via `AdHocTaskTemplateId`; validates/completes against that template's fields.
- **Notes-only** — no structured fields; bare notes + optional photo.

`CreateTaskCommand` accepts at most one of `ChecklistId` / `TaskTemplateId`; both unset = notes-only. On the field board, no-checklist tasks appear in a **"Standalone Tasks"** group.

**⟳ Per-item scoring (A2):** each `ChecklistTemplateItem` carries optional scoring config:
- `ScoringType`: `null` (unscored) | `PassFail` | `Scale1To5`
- `Weight` (decimal, default 1.0)
- `PhotoRequired` (bool)
- `FailCorrectiveActionText` (nullable)
- `FailScoreThreshold` (int 1–5, only for `Scale1To5`)

All nullable/defaulted, so pre-existing flat checklists remain valid. `UpdateItems` validates the combination (threshold only for `Scale1To5`, weight > 0). The admin **checklist-detail screen is now a full editor**: add items by searching templates, remove, reorder, and set per-item scoring; save is a full-replace.

**⟳ Scored sessions (A3):** a checklist-backed `TaskInstance` **is** the audit session (no separate Walk entity). On completion the client submits per-item scores; the server:
- enforces `PhotoRequired` per item,
- computes a **composite score %** = weighted average of item percents (Pass/Fail → 100/0; 1–5 → score/5·100), persisted as `TaskCompletion.CompositeScorePercent` with raw scores in `ItemScoresJson`,
- determines **failures** (Pass/Fail scored fail, or 1–5 at/below threshold) that carry corrective text.

Scoring logic lives in a pure, unit-tested `ChecklistScoring` domain class (modeled on `ApprovalWorkflow`).

**Acceptance highlights:**
- [x] Checklists carry name/description/scope/region/store; ordered items.
- [x] Items may be scored or unscored; scoring config validated.
- [x] Completing a scored checklist returns and stores a composite %.
- [x] Standalone (no-checklist) tasks create and complete end-to-end.

### FD-06 Recurring Assignments & Scheduling — ⟳ Changed (multi-store)
Binds a checklist to a cron schedule; Quartz `GenerateTaskInstancesJob` fires instances.

**⟳ Multi-store broadcast (B4):** *V1.1 specced this but the code targeted a single store.* V2:
- New join entity **`RecurringAssignmentStore`** (composite key `RecurringAssignmentId + StoreId`), modeled on `UserStoreAssignment`. The scalar `StoreId` was **removed** after a **backfill migration** (one join row per existing assignment) — the only V2 migration that touches existing data.
- `CreateRecurringAssignmentCommand` takes `TargetStoreIds[]`; validated non-empty and distinct; `AssertCanManageStore` per target, **all-or-nothing**.
- A specific-employee assignee is **disallowed when targeting more than one store** (a broadcast lands unassigned/claimable on each store's board).
- The generator **fans out one instance per target store per firing**, with the dedup check now keyed on `(RecurringAssignmentId, DueAt, StoreId)` — *fixing a latent bug that would have silently under-generated all-but-the-first store.*
- Dashboard: store picker is multi-select; the assignee picker hides when >1 store is selected.

### FD-07 Task Board — Field PWA — ⟳ Changed
Real-time board grouped by checklist, standalone tasks in their own group. Completions and the 13-minute token refresh keep the session alive.

**⟳ Unified Create (A5):** the dashboard has a single **"+ Create"** entry point (sidebar + mobile sheet) opening a modal with four destinations — One-time task / Recurring / Checklist / Form — each routing to its existing screen. The **New One-Time Task** screen creates standalone tasks (template-based or notes-only, optional assignee).

### FD-08 Store Kiosk View — ⟳ Changed
Shared permanently-logged-in store device; claim-by-name flow; financial details hidden.

**⟳ Session survival (B1):** the kiosk now runs the same **13-minute token-refresh timer** as the field app, so an unattended station survives past the 15-minute access-token expiry. *(Note: if a refresh ultimately fails, the board still routes to login — acceptable for V2; unattended re-auth is a future consideration.)*

**⟳ Shift progress (B6):** a lightweight **"X of Y tasks done today"** strip, computed client-side from data already fetched.

### FD-09 Task Completion & Verification — ⟳ Changed (photo, scoring)
Dynamic field rendering; server + client validation; idempotent complete; verify/cancel/defer as in V1.1.

**⟳ Photo upload (B5):**
- `POST /tasks/{id}/photo-upload-url` (body `{templateId, fieldId}`) returns `{uploadUrl, blobUrl}` via `IStorageProvider`; blob path is `{tenantId}/{taskId}/{templateId}-{fieldId}-{guid}.jpg`.
- The client **compresses on device** (canvas resize/re-encode) then **PUTs directly to storage**, bypassing the API — Azure Blob gets `x-ms-blob-type: BlockBlob`; Supabase uses its signed-upload shape. The returned blob URL becomes the field value and submits with the rest of the completion.

**⟳ Scored completion (A3):** `POST /tasks/{id}/complete` accepts optional `itemScores[]` (`templateId`, `score`, optional `photoUrl`). Response now includes `compositeScorePercent` and `spawnedCorrectiveTaskIds`. Field UI renders a Pass/Fail toggle or 1–5 picker per scored item, an optional per-item photo, and shows the composite score after submit.

### FD-10 Corrective Actions — ⟳ Changed (auto-generated tasks)
Field-level corrective text still surfaces at completion (out-of-range numeric / false boolean), recorded on the completion.

**⟳ Auto-corrective tasks (A4):** when a **scored checklist item fails** and has corrective text, the completion handler **spawns a standalone corrective `TaskInstance`** in the same transaction:
- `ChecklistId = null`, `SourceTaskInstanceId` = the session task (audit trail link),
- `Status = Pending`, `AssignedToUserId = null` (claimable), `DueAt = completion time + 24h`,
- `Notes` references the item + its corrective text.
A `TaskCreated` SignalR event is broadcast per spawned task; the completion response returns their ids ("N corrective tasks created").

### FD-11 Manager Walk — **RETIRED / REPLACED**
*V1.1 specced a separate Walk domain (`WalkTemplates`, `WalkSessions`, `/walk-sessions/*`).* **V2 does not build it.** The capability — scored audit items, per-item photos, composite score, auto-generated corrective tasks — is delivered by **scored Checklists (FD-05) + scored sessions (FD-09) + auto-corrective tasks (FD-10)**. There are no Walk entities or endpoints. A one-time migration (see §9) converts legacy flat "walk" templates into scored checklists.

### FD-12 MDOG & Inventory — *unchanged*
System MDOG template; `InventorySnapshots` per item/store/day; next-day pre-population; 3-day dough plan targets in `StoreSettings`; 56-degree rule as a range constraint. Inventory snapshots are written from checklist-backed completions.

### FD-13 Safe, Till & Deposit Log — ⟳ Changed (escalation)
Till counts via a system template with variance rules; immutable `DepositLog`.

**⟳ Missed-deposit escalation (B3):** *V1.1 specced a 10:00 AM Critical FCM push.* V2 is **dashboard-flag-only, deadline-aware**:
- `StoreSettings.DepositDeadlineLocalTime` (nullable `TimeOnly`, default 9:00 PM local) sets each store's deadline.
- Quartz `DepositEscalationJob` (daily, cron configurable via `Jobs:DepositEscalationCron`) iterates active tenants/stores: if a store has no `DepositLog` in today's window **and** its local deadline has passed, it writes a `MissedDepositFlag` (one per store per business day; idempotent).
- The region/system dashboards read those flags into their existing missed-deposit signal (single definition, no false alarms before the deadline).
- **Limitation (accepted):** reaches only a supervisor with the dashboard open; no push/offline delivery.

### FD-14 Red Book — *retired (unchanged)*
Content, not a feature. Delivered as preloaded System-scope templates/forms.

### FD-15 Notification System — ⟳ Changed (SignalR-only in V2)
Real-time in-app events via SignalR store-groups: `TaskUpdated`, and now **`TaskCreated`** (for auto-corrective tasks and recurring fan-out). *V1.1's FCM push channel and the "deposit → Critical push" mapping are **not built** in V2* — deposit escalation is dashboard-flag-only (FD-13). FCM/web-push remains deferred.

### FD-16 Dashboards — ⟳ Changed (all roles real)
**⟳ Admin (B6):** `GetSystemDashboardHandler` now allows **region-scoped admins** (results narrowed to `spec.RegionIds`), not just super_admin. The admin overview calls `getSystemDashboard()` directly instead of client-aggregating per-region dashboards. Store-scoped roles are denied.

**⟳ Store employee (B6):** the field board is restructured into the four PRD sections — **My Tasks / Open Store Tasks / My History (7 days) / Store Progress %** — via `myTasks()` (assigned to me) and `openStoreTasks()` (unassigned + open) computed signals. The old thin `/dashboard` screen was removed and the route now serves the enriched board.

**⟳ Kiosk (B6):** shift-progress strip (see FD-08).

Region/system dashboards read the missed-deposit flag (FD-13) for their critical-alert and missed-deposit widgets.

### FD-17 Admin Panel — ⟳ Changed (import, create, editor)
User/store/region/settings management; system template authoring.

**⟳ Template import (A6):** *fixed the fake-success bug* where `Type == "Checklist"` rows incremented the "created" count without persisting anything. Now:
- Only rows that actually persist count as created.
- A `Checklist` import row carries an `items[]` array; each item becomes a `TaskTemplate` + a **scored** `ChecklistTemplateItem` (instead of one flat template with N crammed fields). Scoring fields validated.
- `Form` type remains rejected (Forms have their own creation flow).

**⟳ Unified Create (A5):** the "+ Create" entry point (FD-07) lives in the admin shell.

### FD-18 / FD-19 Forms & Approval Engine — *unchanged*
Form Templates (`Sequential` / `Parallel` / `NotificationOnly`) and the submission approval state machine are as specced in V1.1 and were already working per the audit. Not modified this release.

---

## 6. Data Model — Key Entities

⟳ **Changed / new entities and columns in V2:**

| Entity | V2 change |
|---|---|
| `TaskInstance` | `ChecklistId` → **nullable**; new `AdHocTaskTemplateId` (FK→TaskTemplate), `SourceTaskInstanceId` (self-FK, SetNull) |
| `ChecklistTemplateItem` | new `ScoringType`, `Weight` (default 1.0), `PhotoRequired`, `FailCorrectiveActionText`, `FailScoreThreshold` |
| `TaskCompletion` | new `CompositeScorePercent` (nullable), `ItemScoresJson` |
| `RecurringAssignment` | scalar `StoreId` **removed**; now a `TargetStores` collection |
| `RecurringAssignmentStore` | **new** join entity (`RecurringAssignmentId` + `StoreId`) |
| `StoreSettings` | new `DepositDeadlineLocalTime` (nullable `TimeOnly`) |
| `MissedDepositFlag` | **new** (`StoreId`, `BusinessDate`, `FlaggedAt`; unique per store/day) |

**Retired vs V1.1:** `WalkTemplates`, `WalkAuditItems`, `WalkSessions`, `WalkSessionItems` are **not implemented**. `FcmDeviceTokens` is not implemented (no push in V2).

Stable entities carry forward: `Tenants`, `Regions`, `Stores`, `UserProfiles`, `UserStoreAssignments`, `RefreshTokens`, `TaskTemplates`, `Checklists`, `DepositLog`, `InventorySnapshots`, `FormTemplates`, `FormSubmissions`, `FormSubmissionApprovalSteps`.

**Migrations delivered:** `StandaloneTasks`, `ChecklistItemScoring`, `ChecklistSessionScoring`, `AddDepositEscalation`, `MultiStoreRecurringAssignments` (backfill-then-drop). All additive/nullable except the last, which backfills before dropping the scalar `StoreId`.

---

## 7. Non-Functional Requirements

Unchanged from V1.1 in intent. Notes for V2:
- **Testing:** pure domain logic (`ChecklistScoring`, `TaskFieldValidator.ValidateAdHoc`, job logic) unit-tested; feature flows covered by xUnit + `WebApplicationFactory` integration tests. Current suite: **65 unit + 123 integration, all passing.**
- **Security:** signed-URL photo uploads never route file bytes through the API; the app JWT is not attached to direct-to-storage PUTs. Temp passwords are returned once and never persisted/logged.
- **Provider portability:** storage and auth are behind swappable interfaces; EF migrations target Npgsql (the migration set applied in the deployed environments).

---

## 8. Out-of-Scope / Deferred

- **Push notifications (FCM/web-push)** — SignalR only in V2.
- **Deposit escalation beyond the dashboard** — no push/offline reach.
- **Self-service / email password reset** — admin-triggered only.
- **Manager Walk as a separate domain** — replaced by scored checklists.
- Historical trend charts, data export, notification inbox/preferences, offline-first completion queue, per-store timezone scheduling — all deferred to a later version.

---

## 9. Open Follow-Ups

1. **Run the legacy-template migration on a test DB.** `execution/migrate_flat_walks_to_checklists.py` explodes old flat "walk" templates into scored checklists. It is written and self-tested (pure transform) but has **not** been run against production; it defaults to dry-run and requires an explicit `--apply` + a target database.
2. **Align the `execution/` seed generators** (`populate_empty_templates.py`, `seed_real_checklists.py`) to the new nested-checklist shape so future re-seeds don't regenerate flat walk templates.
3. **Pre-existing supervisor bug (not part of this release):** `supervisor/overview.component.ts` reads the legacy singular `regionId` instead of the plural `regionIds`, so a multi-region supervisor sees only their first region. Flagged for a follow-up fix.
4. **Optional:** decide whether the composite-score deposit component and per-store escalation should later gain push delivery.
