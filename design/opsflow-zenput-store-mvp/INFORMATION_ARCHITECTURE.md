# Information Architecture: Zenput-Informed OpsFlow Store MVP

## Primary Navigation

Mobile bottom tabs:
- Today
- My Work
- Create
- Alerts
- More

Chromebook/web sidebar:
- Today
- Timeline
- My Work
- Staff
- Operations
- Alerts
- Records
- Templates
- Sign-Off

Mobile `More` includes Staff, Operations, Records, Templates, Sign-Off, and Settings.

## Site Map

- Today `/today`
  - Dashboard `/today`
  - Daily Timeline `/today/timeline`
  - Current Section `/today/timeline/:sectionId`
  - Task Detail `/tasks/:taskId`
  - Create Work `/create`
- My Work `/my-work`
  - Assigned Work `/my-work`
  - Task Detail `/tasks/:taskId`
  - Blocker Report `/tasks/:taskId/blocker`
- Create `/create`
  - New Task `/create/task`
  - New Checklist `/create/checklist`
  - New Recurring Work `/create/recurring`
  - Duplicate From Template `/create/from-template`
  - Assignment Target Picker `/create/target`
- Staff `/staff`
  - Assignment Board `/staff/assignments`
  - Staff Workload `/staff/workload`
- Operations `/operations`
  - Temperature Log `/operations/temperature`
  - Inventory / Product Controls `/operations/inventory`
  - Cash / Till `/operations/cash`
  - Closing Admin `/operations/closing-admin`
- Alerts `/alerts`
  - Alert Detail `/alerts/:alertId`
  - Corrective Actions `/alerts/corrective-actions`
  - Incident Report `/alerts/incidents/new`
- Records `/records`
  - Daily Review `/records/today`
  - Previous Days `/records/history`
  - Coaching Signals `/records/coaching`
- Templates `/templates`
  - F0890 Template Preview `/templates/f0890`
  - Lightweight Template Edit `/templates/:templateId/edit`
- Sign-Off `/sign-off`
  - Section Sign-Off `/sign-off/sections`
  - Day Closeout `/sign-off/day`

## Today Dashboard Content Order

1. Store/date/shift state.
2. Critical alert banner.
3. Ranked work due now.
4. F0890 phase progress.
5. Unassigned work and staff workload.
6. Operational data cards: temperature, inventory, cash.
7. Closeout readiness.
8. Coaching signals and recent missed-work history.

## Daily Timeline Sections

The F0890 proving-ground template should ship with:

1. Opening Setup.
2. Product Management, due 11:00 AM.
3. Ongoing Lunch Duties.
4. Pre-Rush Walkthrough, due 3:30 PM.
5. Deployment Guide.
6. Closing Checklist.
7. Closing Admin Checklist.

Each section supports:
- Due time or active window.
- Assignment required flag.
- Manager sign-off flag.
- Task count, completed count, late count, exception count.
- Section notes.

## Task Types

| Type | Examples | Required UI |
| --- | --- | --- |
| Standard task | Sweep lobby, wipe monitors | Instructions, complete, initials |
| Checklist task | Bathroom cleaned, make-line stocked | Task instructions, complete, initials |
| Data capture | Temperature reading, cash count | Structured fields, validation |
| Verification | Product labels dated, FIFO checked | Pass/fail, note on fail |
| Assignment gate | Deployment assigned | Assignee controls, blocker state |
| Coaching task | Promo awareness, phone upsell | Verified/not verified, note |
| Incident | Equipment issue, safety concern | Severity, notes, evidence |

## Core Screens

### Today Dashboard

Purpose: Give managers an immediate answer to "what needs attention now?"

Primary actions:
- Open current section.
- Assign unowned tasks.
- Resolve alert.
- Add work.
- Start closeout.

### My Work

Purpose: Let employees complete assigned work without manager noise.

Primary actions:
- Start task.
- Complete task.
- Enter required data.
- Flag blocker.
- Add comment/evidence placeholder.
- Require typed employee name and initials when active role/profile is Store Account.

### Create Work

Purpose: Let managers add operational work without needing the later full admin builder.

Primary actions:
- Create one-off task.
- Create recurring task.
- Create simple checklist.
- Duplicate from F0890 template.
- Set section, due window, assignee/role, required fields, recurrence, and instructions.
- Assign to Store Account or an individual account.
- Block personal task creation from Store Account context.

### Staff Assignment

Purpose: Make accountability explicit before work becomes missed work.

Primary actions:
- Assign task.
- Reassign task.
- Bulk assign section.
- Filter unassigned, overdue, due soon, section.
- Choose assignment target type: Store Account or individual account.

### Operations

Purpose: House structured data capture that should not be buried inside generic checklists.

Sub-areas:
- Temperature Log.
- Inventory/Product Controls.
- Cash/Till.
- Closing Admin.

### Alerts And Corrective Actions

Purpose: Convert exception states into resolved operational actions.

Primary actions:
- Acknowledge.
- Assign.
- Resolve.
- Add corrective action.
- Open source task.
- Create incident.

### Records

Purpose: Preserve auditability and reveal coaching opportunities.

Primary views:
- Today's Daily Review.
- Recent history.
- Repeated misses.
- Staff/section trends.

### Templates

Purpose: Show F0890 as configurable source structure and support lightweight edit/duplicate flows.

Primary actions:
- Preview template.
- Duplicate task or section.
- Edit title, due window, recurrence, assignee role, and required fields.

### Sign-Off

Purpose: Make manager accountability explicit before the day closes.

Primary actions:
- Sign section.
- Acknowledge exception.
- Add final note.
- Close day.

## State Model

Task states:
- Not started.
- Assigned.
- In progress.
- Blocked.
- Needs corrective action.
- Submitted.
- Manager review.
- Complete.
- Late.
- Missed.

Account role/profile states:
- Employee.
- Store Account.
- Store Manager/GM.
- Supervisor/Ops Lead.
- Auditor.
- System Admin.

Assignment target states:
- Store-level shared account.
- Individual person account.
- Manager/supervisor/auditor account.

Section states:
- Not ready.
- In progress.
- Ready for sign-off.
- Signed.
- Signed with exceptions.
- Blocked.

Alert states:
- New.
- Acknowledged.
- Assigned.
- Corrective action pending.
- Resolved.
- Closed in review.

## Responsive Behavior

Mobile:
- Bottom tab navigation.
- Task cards instead of dense rows.
- Sticky completion action on Task Detail.
- Sheets for assignment and corrective action.
- Create button available from Today and nav.

Chromebook/web:
- Persistent sidebar.
- Dashboard cards plus dense operational tables.
- Two-pane task and alert review where useful.
- Assignment board with staff column and task queue.
