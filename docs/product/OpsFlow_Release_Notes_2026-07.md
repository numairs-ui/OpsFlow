# OpsFlow — What's New (July 2026 Release)

**Date:** 2026-07-14
**Scope:** 8 post-PRD-audit workstreams — Track A (structural: task/checklist model) + Track B (5 targeted fixes). Multi-store recurring broadcast (originally B4) has been **carved out and deferred** to its own future release — see note below.
**Status:** Implemented and tested. Not yet deployed.

This release closes the gaps a PRD audit found between the shipped product and the original spec, and adds the checklist-scoring capability that replaces the never-built "Manager Walk" as a separate feature. Everything below is in the codebase on `feat/track-b-post-prd-audit`, backed by a **single additive database migration** — nothing existing is dropped or rewritten, so this is a low-risk release.

---

## Headline changes

### 1. Tasks no longer require a checklist
Previously every task had to belong to a checklist. Now a task can be created three ways:
- **From a checklist** (as before),
- **From a single template** (a standalone task with structured fields), or
- **Notes-only** (a quick "do this" task with optional photo).

A new **"New One-Time Task"** screen in the dashboard creates these, and standalone tasks show in their own "Standalone Tasks" group on the field board.

### 2. Scored checklists (replaces "Manager Walk")
Checklists are now the audit tool. Each checklist item can be configured with:
- a **scoring type** — Pass/Fail or a 1–5 scale,
- a **weight** (how much it counts toward the score),
- **photo required** on/off,
- a **corrective action** and, for 1–5 items, a **fail threshold**.

The admin checklist screen is now a **full editor** (add items by searching templates, remove, reorder, set scoring per item). When a store completes a scored checklist, the system computes a **composite score (%)** and shows it on completion.

### 3. Failed items auto-create follow-up tasks
When a scored item fails, the system automatically creates a **claimable corrective task** on that store's board (due in 24 hours, linked back to the checklist it came from). The person completing the checklist sees "N corrective tasks created."

### 4. One unified "Create" button
The dashboard now has a single **+ Create** entry point that offers all four things you can make — One-time task, Recurring, Checklist, Form — instead of hunting through the sidebar.

### 5. Photo evidence actually works
The "Photo upload coming soon" placeholder is gone. Field staff can now **take/choose a photo** on any task with a photo field; it's compressed on-device and uploaded directly to storage. Works with both storage backends (Azure / Supabase).

### 6. Admin-triggered password reset
Admins can **reset a user's password** from the Users screen and get a **one-time temporary password** to hand off. No email infrastructure required. (Self-service email reset remains deferred.)

### 7. Missed-deposit alerting
A daily background job flags any store that hasn't logged its deposit by its **deadline** (configurable per store, default 9:00 PM local). Flagged stores surface on the region/system dashboards. *Delivery is dashboard-only — no push notification in this release.*

### 8. Real dashboards for every role
- **Admins** now load the system dashboard directly (previously only super-admins could; admins are scoped to their own regions).
- **Store employees** get a proper board: My Tasks / Open Store Tasks / My History (7 days) / Store Progress %. The old thin `/dashboard` screen was merged into the richer one.
- **Kiosk** gains an "X of Y done today" shift-progress strip.

### 9. Kiosk stays logged in
The shared kiosk board previously fell offline after ~15 minutes when its token expired. It now refreshes its session on the same schedule as the field app, so an unattended station keeps working through a shift.

---

## Deferred to a later release: multi-store recurring broadcast

The original plan let a supervisor broadcast one recurring assignment across many stores at once. Implementing it required removing the single `StoreId` column from `RecurringAssignment` in favor of a store-list, which — on a live database — meant dropping a column real assignments depend on. That's real risk for a supervisor convenience feature, so **it's been pulled out of this release**.

**Nothing changes for store managers or supervisors today** — single-store recurring assignments work exactly as they do in production right now. Multi-store broadcast will come back later as its own release, designed so it doesn't require dropping anything live.

---

## Schema changes — one additive migration

Everything above is delivered by **a single migration, `SafeReleaseSchema`**, and every change in it is purely additive: new nullable columns, new columns with safe defaults, and one new table. **Nothing is dropped, nothing existing is rewritten.** Specifically, it:
- makes `TaskInstance.ChecklistId` optional and adds two new nullable columns to `TaskInstance` (for standalone tasks and corrective-task linking),
- adds scoring columns to `ChecklistTemplateItem` (all optional / safely defaulted),
- adds a composite-score column and a scores column to `TaskCompletion`,
- adds a deposit-deadline column to `StoreSettings`,
- creates a new `MissedDepositFlag` table.

Because it's fully additive, applying it is low-risk and doesn't require any special ordering relative to the code deploy.

---

## Known limitations / deliberately deferred

- **Multi-store recurring broadcast** — carved out of this release (see above); single-store recurring assignments are unaffected.
- **Deposit escalation is dashboard-only** — it reaches a supervisor only if they have the dashboard open. No push/offline reach (out of scope this round).
- **The one-time data migration** that converts old flat "walk" templates into scored checklists is written and self-tested, but has **not been run against production** — it needs to be pointed at a test database first.
- **Seed generators** (`execution/`) still emit the old flat-template shape; aligning them to the new nested checklist shape is a follow-up.
- **Password reset** is admin-triggered only; no self-service email flow.

---

## For reviewers

- Branch: `feat/track-b-post-prd-audit`.
- Verification: full backend test suite passing; both `dashboard` and `field-pwa` build clean.
- Full detail and updated requirements: see **OpsFlow_PRD_V2.md**.
