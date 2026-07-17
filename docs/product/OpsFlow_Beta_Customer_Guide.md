# Welcome to OpsFlow — Beta Customer Guide

*A plain-language guide to what OpsFlow does, what your role can do, and the terms you'll see on screen.*

---

## 1. What OpsFlow Is

OpsFlow replaces paper checklists, group chats, and spreadsheets with one system for running daily
store operations: assigning tasks, completing checklists, flagging problems the moment they happen,
and giving managers a live view of what's actually getting done across every store.

Two things you'll use, depending on your role:

- **The Field App** — for people working *in* a store: completing tasks, checking things off,
  taking photos, submitting forms. Works great on a phone or a tablet mounted at the register.
- **The Dashboard** — for people managing stores: assigning work, reviewing what's been done,
  building checklists, and watching performance across one store, one region, or the whole business.

You'll be told which one (or both) to log into, along with your web address and login details,
by whoever is onboarding you.

---

## 2. Your Role

OpsFlow has six roles. You'll be set up with exactly one. Find yours below — you don't need to
read the others, though it's useful context for understanding what your manager or team sees.

### Store Employee
**You work a shift and get things done.**

- You see a **task board** for your store: what needs doing today, grouped by checklist, plus any
  "standalone" one-off tasks.
- Anything unassigned is an **Open Task** — grab it by tapping it and entering your name, no login
  needed for that step.
- Complete tasks by filling in the fields they ask for: check a box, enter a number (like a
  temperature), write a note, or take a photo as proof.
- If something's out of range (a temperature too high, a checklist item marked "No"), OpsFlow
  automatically flags it — you don't need to remember to tell anyone.
- You can submit **Forms** (like an incident report) if your store uses them.

### Store Kiosk
**A shared tablet/device at the store, not a personal login.**

- Anyone can walk up, tap an open task, type their name, and complete it — no password needed for
  that day-to-day flow.
- Shows a running "X of Y tasks done today" progress strip.
- Financial details (deposits, till counts) are hidden on kiosk devices — that's manager-level
  information.

### Store Manager
**You run one store (or a few) day-to-day.**

- Everything a Store Employee can do, plus:
- **Assign tasks** to specific people, or leave them open for anyone to claim.
- **Create one-time tasks** and **recurring assignments** (e.g., "Morning Opening checklist, every
  day at 7am") for your store.
- **Build and edit checklists** — decide what's on them, and turn on **scoring** (see the glossary)
  so completions produce a pass/fail or 1–5 score instead of just "done."
- **Verify, cancel, or defer** tasks your team has completed or missed.
- **Record deposits** and view your store's deposit log.
- See your **store's dashboard**: what's open, overdue, completed today, and how your checklists are
  scoring over time.

### Supervisor
**You oversee one region — several stores.**

- Everything a Store Manager can do, but across every store in your region, not just one.
- **Region dashboard**: compare stores side by side — which ones are falling behind, which
  checklists are failing most often, which recurring schedules look stale (i.e., not generating
  tasks the way they should).
- Author checklists and templates at the **regional** level so every store in your region uses the
  same standard, not just your own store's copy.

### Admin
**You administer one or more regions (sometimes all of them) — the operational owner's view.**

- Everything a Supervisor can do, across every region you're responsible for.
- **Manage stores, regions, and users** in your scope: add a new store, deactivate one that's
  closed, create user accounts, reset a forgotten password.
- Author **System-level** templates and checklists — the master standard every region can adopt.
- **Import** templates in bulk (from a spreadsheet/JSON export) rather than building one at a time.
- **System-wide dashboard**, scoped to whichever regions you own.

### Super Admin
**Full, network-wide access — the account your organization's owner or IT lead holds.**

- Everything every other role can do, everywhere, with no region limit.
- Manages tenant-wide settings (new-store defaults: timezone, grace periods, till/cash bases, etc.)
  so every new store you add starts correctly configured out of the box.
- The one role that can see and manage absolutely everything in your OpsFlow account.

---

## 3. Core Concepts (What You'll See On Screen)

**Tasks** are the basic unit of work in OpsFlow. Every checklist, every recurring schedule, every
one-off assignment eventually becomes a **Task Instance** that moves through a simple lifecycle:
Pending → In Progress → Completed → Verified (or Cancelled/Deferred along the way).

**Checklists** group a set of fields (numeric readings, yes/no checks, text notes, photos, or
nested sub-checklists) into one thing your team completes together — e.g., "Morning Opening."
A checklist can optionally be **scored**: each item counts toward a composite percentage score,
and any failed item can automatically spawn a **Corrective Action** task for someone to follow up on.

**Recurring Assignments** are how a checklist gets scheduled — "run this checklist at this store
every day at this time" — so your team doesn't have to remember to create it manually.

**Corrective Actions** are the safety net: when a task is missed, or a checklist item comes back
out of range or marked "No," OpsFlow automatically creates a follow-up task so nothing falls through
the cracks silently.

**Forms** are for things that aren't routine checklists — incident reports, audits — and can route
through an approval chain (submit → review → approve/reject) instead of just being marked done.

**Templates** are the reusable blueprints behind checklists and tasks — the actual questions/fields,
built once and reused across many checklists or stores, at System, Regional, or Store scope
depending on who's meant to standardize them.

**Dashboards** give every role a live view scoped to what they're responsible for — a store manager
sees their store, a supervisor sees their region, an admin/super admin sees as much of the network
as their role covers. Nobody sees more than their role and scope allow, and nobody sees *less* than
they should either — if your role should have visibility into something and doesn't, that's worth
reporting as beta feedback.

---

## 4. Glossary

**General OpsFlow terms**

| Term | Meaning |
|---|---|
| Open Task | A task with no one specifically assigned — anyone at the store can claim and complete it. |
| Task Instance | One dated occurrence of a task — e.g., today's copy of "Morning Opening." |
| Checklist | An ordered group of fields/items completed together as one task. |
| Scored Checklist | A checklist where each item counts toward a composite percentage score. |
| Composite Score | The weighted average result of a scored checklist completion. |
| Corrective Action | An automatic follow-up task created when something is missed or fails a check. |
| Recurring Assignment | A checklist bound to a store and a repeating schedule (e.g., daily, weekly). |
| Standalone Task | A one-off task that isn't part of any checklist — a single instruction or note. |
| Template | The reusable set of fields/questions behind a checklist or task. |
| Proof of Work | A photo or filled-in field attached to a task as evidence it was done correctly. |
| Form | A structured submission (e.g., incident report) that can route through approval steps. |
| Region / Store Scope | The slice of the business a role can see and act on — one store, one region, or everything. |
| Deposit Log | The record of cash deposits made by a store, and whether they were made on time. |

**Restaurant-operations terms you may see in checklist content**

| Term | Meaning |
|---|---|
| MDOG | Make Line Dough Optimization Guide — the prep sheet for hourly dough pulls and daily prep needs. |
| Make Line | The main food-assembly area — stocked, equipped, and date-labeled per the MDOG. |
| Lexans | The food storage containers in the make line's top/bottom wells that get cleaned and rotated. |
| Walk-in | The main refrigerated storage area, including dough temperature control. |
| Slap Station | The dough stretching/prep counter. |
| Dustinator | The flour/dusting mixture at the slap station — regularly swept and wiped during closing. |
| Till A / Till B | The specific cash drawers tracked during opening, shift changes, and closing audits. |

---

## 5. Tips for Beta Testers

- **Explore within your role first.** Try the everyday flow you'd actually use — completing tasks
  if you're an employee, building a checklist if you're a manager — before poking at edge cases.
- **If something looks like it's missing or in the wrong place,** that's exactly the kind of feedback
  this beta is for. Note what you expected to see and where you looked for it.
- **Photos and scored checklists** are worth testing deliberately — try marking an item as failing
  and confirm a follow-up (corrective) task appears.
- Your onboarding contact can answer questions about your specific login, store setup, or anything
  that seems broken rather than intentionally different.
