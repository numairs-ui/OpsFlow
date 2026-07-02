# Information Architecture: OpsFlow Cross-Platform MVP

## Site Map

- Today `/`
  - Dashboard `/today`
  - Daily Timeline `/today/timeline`
    - Task Detail `/today/tasks/:taskId`
  - Alerts `/today/alerts`
    - Alert Source Task `/today/tasks/:taskId?from=alert`
  - Daily Review `/today/review`
- Tasks `/tasks`
  - My Tasks `/tasks/my`
  - Task Detail `/tasks/:taskId`
- Operations `/operations`
  - Staff Assignment `/operations/assignments`
  - Inventory `/operations/inventory`
  - Cash/Till `/operations/cash`
  - Temperature Log `/operations/temperature`
  - Variance Detail `/operations/variance/:varianceId`
- Closeout `/closeout`
  - Manager Sign-off `/closeout/signoff`
  - Daily Review Report `/closeout/report`
- Templates `/templates`
  - Template Preview `/templates/:templateId`
  - Lightweight Template Edit `/templates/:templateId/edit`
- Settings `/settings`
  - User/Profile `/settings/profile`
  - Store Context `/settings/store`

## Navigation Model

- **Primary navigation**: Today, Tasks, Operations, Closeout, Templates. Keep this to five items for mobile and Chromebook consistency.
- **Secondary navigation**: Context tabs or segmented controls inside each section, such as All / Due Soon / Overdue in Alerts, or Assigned / Unassigned in Staff Assignment.
- **Utility navigation**: Store selector, profile, sync/status placeholder, and help/settings. These should sit in the top app bar on web and an overflow/menu entry on mobile.
- **Mobile navigation**: Bottom tabs for Today, Tasks, Operations, Closeout, and More. Templates and Settings can live behind More if space is tight.
- **Chromebook/web navigation**: Persistent left rail/sidebar with the same primary sections. Detail pages can use two-pane layouts where practical: list on the left, detail on the right.

## Content Hierarchy

### Today Dashboard

1. Current shift status -- The manager needs to know what needs attention now.
2. Current time window and next deadline -- Daily work is time-based, not just category-based.
3. Exceptions and alerts -- Overdue, variance, and out-of-range items drive manager action.
4. Team assignment snapshot -- Shows who owns work and where bottlenecks are forming.
5. Closeout readiness -- Indicates whether the day can be signed off yet.

### Daily Timeline

1. Time windows -- Opening, midday, afternoon, close, or custom F0890 sections.
2. Section progress -- Completed, in progress, overdue, and unassigned counts.
3. Task rows -- Task title, assignee, deadline, required data indicators, status.
4. Context actions -- Assign, start, complete, flag issue, open detail.

### Task Detail

1. Task status and required action -- Users should know immediately what they can do.
2. Instructions and subtasks -- Operational details belong close to completion controls.
3. Required data fields -- Temperature, inventory, cash, notes, initials, or variance reason.
4. Audit trail -- Timestamps, assignee, completed by, changes, exceptions.
5. Related alerts/history -- Secondary, but available when investigating.

### Staff Assignment

1. Unassigned or at-risk tasks -- Assignment screen should reduce manager chasing.
2. Staff availability/workload -- Prevent blind delegation.
3. Assignment actions -- Assign, reassign, unassign, assign all within a section.
4. Status summary -- Show what changed and what remains unowned.

### Inventory

1. Items requiring action -- Shortage/overage/expiring stock first.
2. F0890 3-day planning table -- On hand, dates, daily needs, total needed, action.
3. Variance/action state -- A/B/C action and notes.
4. Save/complete audit metadata -- Who captured it and when.

### Cash/Till

1. Till selector and expected amount -- Establish context before counting.
2. Denomination count grid -- Fast entry with live total.
3. Variance display -- Make mismatch impossible to miss.
4. Manager initials/sign-off -- Accountability before completion.

### Temperature Log

1. Location selector and target range -- Context drives validation.
2. Reading entry -- Fast capture with numeric keyboard on mobile.
3. Inline result -- In range/out of range with note requirement for exceptions.
4. Recent readings -- Helps managers spot repeat problems.

### Alerts

1. Critical/overdue alerts -- Highest urgency first.
2. Due-soon warnings and variance alerts -- Prevent issues before they become failures.
3. Action buttons -- Complete, assign, acknowledge, resolve, open source.
4. Empty states -- Confirm when a category has no issues.

### Closeout / Manager Sign-off

1. Sign-off readiness summary -- Show blockers before asking for initials.
2. Section sign-offs -- Opening, prep, inventory, cash, close, etc.
3. Exceptions requiring acknowledgement -- Variances, overdue items, out-of-range readings.
4. Day-level signature -- Initials, timestamp, final notes.

### Daily Review Report

1. Completion summary -- What got done and what did not.
2. Exceptions summary -- Variance, overdue, temperature, cash issues.
3. Staff/accountability summary -- Assigned/completed by person or role.
4. Export/share placeholders -- Included visually, not wired to backend.

### Templates

1. Template list/preview -- Show the structure of the active operating checklist.
2. Lightweight edit entry -- Rename, view sections, preview task types.
3. Full builder deferral -- Clearly mark advanced builder as later/admin scope.

## User Flows

### Manager Runs Today's Shift

1. Manager lands on Today Dashboard.
2. Manager sees current time window, overdue/due-soon items, and closeout readiness.
3. Manager opens Daily Timeline.
4. Manager expands a section and reviews tasks.
   - If a task is unassigned -> assign staff from Staff Assignment panel/sheet.
   - If a task requires data -> open Task Detail with relevant fields.
   - If a task is overdue -> alert state remains visible until resolved.
5. Manager returns to Dashboard to monitor progress.

### Employee Completes Assigned Task

1. Employee opens Tasks.
2. Employee sees My Tasks ordered by urgency and current time window.
3. Employee opens a task.
4. Employee follows instructions and completes subtasks.
   - If no data is required -> mark complete.
   - If data is required -> enter fields before completion is enabled.
   - If unable to complete -> provide reason/note and flag manager.
5. Task updates with completion status and timestamp.

### Manager Captures Operational Data

1. Manager opens Operations.
2. Manager chooses Inventory, Cash/Till, or Temperature Log.
3. Manager enters required values.
   - If values are normal -> save and mark related task complete.
   - If values are outside expected range -> system creates/updates variance or alert state.
4. Manager adds notes/initials as required.
5. Data becomes part of Daily Review and Closeout.

### Manager Resolves Alert

1. Manager opens Alerts from dashboard or nav.
2. Manager filters by Overdue, Due Soon, Variance, or Completed.
3. Manager opens an alert or acts from the alert card.
   - Complete -> marks related task done if requirements are satisfied.
   - Assign -> opens assignment control.
   - Resolve -> asks for resolution note where appropriate.
   - View Source -> opens Task Detail.
4. Alert changes state and the dashboard summary updates.

### Manager Closes the Day

1. Manager opens Closeout.
2. Manager reviews section readiness and unresolved exceptions.
3. Manager signs individual sections where required.
   - If blockers remain -> sign-off remains disabled or requires acknowledgement.
   - If all requirements are satisfied -> day-level sign-off is enabled.
4. Manager enters initials and final notes.
5. Daily Review Report shows signed status and audit summary.

## Naming Conventions

| Concept | Label in UI | Notes |
| ------- | ----------- | ----- |
| Daily operating home | Today | Short, immediate, works for mobile nav. |
| Time-based checklist | Timeline | Better than Checklist for time-window execution. |
| Individual checklist item | Task | Clear for employees and managers. |
| Group of tasks | Section | Maps to F0890 operational groupings. |
| Person responsible | Assignee | Standard accountability term. |
| Data mismatch/problem | Variance | Used for inventory and cash exceptions. |
| Urgent system state | Alert | Includes overdue, due soon, variance, out-of-range. |
| End-of-day approval | Sign-off | Matches manager accountability language. |
| End-of-day summary | Daily Review | More operational than report alone. |
| Reusable checklist structure | Template | Admin/configurable model. |

## Component Reuse Map

| Component | Used on | Behavior differences |
| --------- | ------- | -------------------- |
| Responsive app shell | All views | Mobile bottom tabs; web sidebar with wider content region. |
| Top status bar/header | All main views | Shows store, date/shift, profile/settings entry. |
| Section accordion | Timeline, Sign-off, Templates | Expand/collapse on mobile; can remain expanded in web two-pane view. |
| Task row/card | Timeline, Tasks, Alerts source lists | Condensed row on web; tappable card on mobile. |
| Status badge/chip | All operational views | Semantic state plus label/icon, never color alone. |
| Data capture panel | Task Detail, Inventory, Cash, Temperature | Inline on web; stacked or sheet-like on mobile. |
| Alert card | Dashboard, Alerts, Closeout | Summary on dashboard; full actions in Alerts. |
| Label/value row | Dashboard, Report, detail cards | Consistent dense operational facts. |
| Sticky action area | Task Detail, Sign-off, data forms | Bottom CTA on mobile; right/top action region on web. |
| Empty state | Alerts, Tasks, Templates | Icon/title/copy plus one clear action where useful. |

## Content Growth Plan

- Tasks and timeline sections grow daily. Use date/shift filtering later; MVP can use today's sample data.
- Alerts accumulate throughout a shift. Use tabs/filters by state and type.
- Templates grow as restaurants add checklist types. MVP should show list and preview; later add search, duplication, versioning, and full builder.
- Daily Review reports accumulate historically. MVP shows today's report; later add archive, export, and filters.
- Staff lists grow by store. Assignment UI should support search/filter later, but MVP can show a compact fixed staff set.

## URL Strategy

- Pattern: section-first paths such as `/today/timeline`, `/operations/cash`, `/closeout/signoff`.
- Dynamic segments: `:taskId`, `:templateId`, `:varianceId`.
- Query parameters: use for source/context, filtering, and lightweight state, e.g. `?from=alert`, `?filter=overdue`.
- Mobile native equivalent: route names should mirror URL sections so web and mobile mental models stay aligned.
