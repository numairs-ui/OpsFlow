# OpsFlow Strategy: Zenput-Informed F0890 Store MVP

## Strategy Statement

OpsFlow should become a store-level restaurant operations execution product. The first version should help a manager run today's work, assign and verify employee tasks, capture required operational data, identify exceptions, and close the day with an audit-ready record.

Zenput provides the product inspiration: ranked work, recurring tasks, alerts, corrective actions, evidence, history, and manager coaching. F0890 provides the proving-ground operating model: a real restaurant day split into opening, product management, lunch duties, pre-rush, deployment, closing, and closing admin.

The design target is not "Zenput clone." The target is **Zenput-like execution discipline applied to F0890's restaurant-specific operating rhythm**.

## Product Principles

1. **Today first.** The product opens on the ranked work due today, not a generic menu.
2. **Speed for floor users.** Employees should complete assigned work with minimal taps, clear required fields, and no dashboard clutter.
3. **Visibility for managers.** Managers need section progress, late work, exceptions, assignments, and closeout readiness at a glance.
4. **Auditability everywhere.** Important actions capture timestamp, initials, assignee, completion state, variance, exception, and manager acknowledgement.
5. **Corrective action over simple failure.** When a value is out of range or work cannot be completed, the system asks what was done next.
6. **Templates drive consistency.** F0890 is the seed template, but the model must support other restaurant checklists.
7. **Roadmap discipline.** Store manager and employee execution come first; district and home-office features remain future context.

## MVP Personas

| Persona | Goal | Primary Needs |
| --- | --- | --- |
| Store Manager / Shift Lead | Run today's operating checklist and close the day cleanly | Ranked dashboard, assignment, exceptions, sign-off, report |
| Store Employee | Know what work is assigned and complete it quickly | My Work, task instructions, completion identity, blocker flag |
| Store Account | Shared Chromebook execution for store-level work | Store task queue, typed completer name, initials, audit trail |
| Future District Manager | Review performance and audit stores | Roadmap only: store trends, visits, corrective actions |
| Future Home Office | Roll out templates and monitor organization execution | Roadmap only: projects, hierarchy reporting, announcements |

## Account And Role Model

OpsFlow uses one underlying account model with role/profile permissions. Store Account is a shared-device role/profile for Chromebooks and tablets, not a separate technical account type.

Supported role/profile values:
- Employee
- Store Account
- Store Manager/GM
- Supervisor/Ops Lead
- Auditor
- System Admin

Tasks and checklists can be assigned to any account target: store-level shared account, individual employee, Store Manager/GM, Supervisor/Ops Lead, or Auditor. Store-level assignment should be the default recommendation for high-turnover operational work such as inventory checks, food prep, and basic cleaning.

When work is completed from a Store Account, the UI must require typed employee name and initials. Employee accounts use the logged-in employee identity directly.

## Checklist Structure Rule

OpsFlow uses a two-level operating model:

`Checklist -> Task`

Tasks do not contain nested subtasks. Long F0890 procedures should become separate tasks or task instructions inside a task.

## Feature System

### 1. Today Work Dashboard

The dashboard is the manager's command center.

Required capabilities:
- Ranked list of work due now, overdue, due soon, and later today.
- F0890 time-window progress: Opening, Product Management, Lunch/Ongoing, Pre-rush, Deployment, Closing, Closing Admin.
- Alert strip for late work, unassigned work, out-of-range temperature, cash variance, inventory variance, and unresolved corrective actions.
- Staff workload snapshot.
- Closeout readiness indicator.
- Recent coaching signals: repeated misses, most delayed section, top variance.

### 2. Daily Timeline

The timeline is the operating checklist execution surface.

Required capabilities:
- Sections grouped by time window and operational phase.
- Section due times and manager sign-off state.
- Task rows with status, assignee, deadline, data requirement, exception state, and initials.
- Quick actions: assign, start, complete, flag issue, open detail.
- Section gates such as "deployment tasks must be assigned before opening manager leaves."

### 3. My Work / Employee Mode

Employee mode should be fast and narrow.

Required capabilities:
- Assigned tasks sorted by urgency.
- Clear due time and section context.
- One-tap start and complete where no required data is needed.
- Required task instructions, initials, and comments where needed.
- "Cannot complete" path with reason and optional evidence.
- No manager analytics unless the user is in manager mode.

### 4. Task Detail

Task Detail is the reusable work execution screen.

Required capabilities:
- Title, section, due time, assignee, priority, and status.
- Operational instructions and task-level requirements.
- Required data blocks: temperature, count, cash, inventory, checklist confirmation, initials, photo/comment.
- Corrective action prompt when validation fails.
- Comments and attachment placeholder.
- Audit trail: created, assigned, started, completed, edited, reassigned, exception raised, manager acknowledged.

### 5. Staff Assignment

Assignment is core to the manager workflow.

Required capabilities:
- Unassigned and at-risk tasks first.
- Staff list with workload, role, shift availability, and current task count.
- Assign/reassign/unassign actions for Store Account and individual employee targets.
- Bulk assign a section, especially deployment and closing work.
- Assignment completeness gate before manager sign-off where required.

### 6. Temperature And Food Safety

Temperature capture should feel like operational verification, not paperwork.

Required capabilities:
- Item/location selector.
- Target range and current reading.
- Threshold rules, including F0890 dough temperature at 56 degrees.
- Out-of-range state requiring corrective action.
- Corrective action options: moved to refrigeration, discarded, rechecked, manager notified, custom note.
- Timestamp, initials, and recent readings.

### 7. Inventory And Product Controls

Inventory should cover F0890's prep, dating, FIFO, and variance expectations.

Required capabilities:
- Product/date-label verification task.
- FIFO and expiration check status.
- 3-day dough / hourly dough pull planning entry.
- MDOG prep note capture.
- Target Inventory Cost top-variance review.
- Variance reason and manager note.

### 8. Cash / Till / Deposit

Cash workflow should be structured and auditable.

Required capabilities:
- Till A and B expected values, including $50 or $75 setup.
- Store cash expected remaining value, including $450 or $500 lockup.
- Actual count entry and variance state.
- Deposit log confirmation.
- Nightly Numbers, Bad Order Log, Instant Pay, backup disc, safe/drawer verification as closeout admin tasks.
- Manager initials and timestamp.

### 9. Alerts And Corrective Actions

Alerts should turn exceptions into action.

Required alert types:
- Overdue task.
- Due soon.
- Unassigned required task.
- Out-of-range temperature.
- Cash variance.
- Inventory variance.
- Failed checklist item.
- Incident/escalation.
- Closeout blocker.

Required capabilities:
- Acknowledge, assign, resolve, open source task.
- Resolution note where needed.
- Manager acknowledgement for critical exceptions.
- Alert history included in Daily Review.

### 10. Incident / Escalation Reporting

Incident reporting should live beside work execution.

Required capabilities:
- Lightweight report entry: type, location, affected area/person, severity, notes, photo placeholder.
- Link incident to task/section when relevant.
- Escalation status.
- Include unresolved incidents in closeout and Daily Review.

### 11. Manager Sign-Off

Sign-off is where accountability converges.

Required capabilities:
- Section sign-off for major F0890 phases.
- Blocked sign-off when required tasks are incomplete or unassigned.
- Exception acknowledgement path.
- Manager initials and final notes.
- Day-level closeout sign-off after required sections are cleared or acknowledged.

### 12. Daily Review Report

Daily Review is the audit and coaching record.

Required capabilities:
- Completion rate by section.
- Late, missed, reassigned, and exception tasks.
- Temperature/cash/inventory variance summaries.
- Staff accountability summary.
- Corrective actions and incidents.
- Manager sign-off record.
- Recent history panel for repeated misses or coaching signals.

### 13. Templates / Checklist Builder Entry

The MVP needs a lightweight creation path because the store manager must add tasks and checklists.

Required capabilities:
- Template preview for F0890.
- Create task from template section.
- Add one-off task for today.
- Add recurring task/checklist shell with title, section, due window, recurrence, assignee/role, required fields, and instructions.
- Duplicate F0890 section/task as a starting point.
- Prevent Store Account users from creating personal tasks/checklists.
- Full advanced builder is later/admin scope.

## Screen Inventory

| Screen | Primary User | Purpose | MVP Actions |
| --- | --- | --- | --- |
| Today Dashboard | Manager | See what needs attention now | Open timeline, assign, resolve alert, review closeout readiness |
| Daily Timeline | Manager / Employee | Execute F0890 by time window | Expand sections, complete tasks, open details |
| My Work | Employee | Complete assigned tasks fast | Start, complete, flag blocker |
| Task Detail | Manager / Employee / Store Account | Complete or inspect one task | Instructions, data capture, typed identity when shared device, comments, evidence, corrective action |
| Staff Assignment | Manager | Assign and rebalance work | Assign, reassign, bulk assign section |
| Temperature Log | Manager / Employee | Capture food safety readings | Enter reading, trigger corrective action |
| Inventory / Product Controls | Manager | Verify product, prep, FIFO, variances | Capture plan, record variance, add note |
| Cash / Till | Manager | Count and verify cash | Enter counts, record variance, confirm deposit/admin checks |
| Alerts | Manager | Manage exceptions | Acknowledge, assign, resolve, open source |
| Corrective Actions | Manager | Track failed/out-of-range follow-up | Add action, assign, acknowledge |
| Incidents | Manager | Capture escalations | Create report, link to section/task |
| Manager Sign-Off | Manager | Verify sections and close day | Section initials, exception acknowledgement, final sign-off |
| Daily Review | Manager | Audit and coach from the day's record | Review completion, misses, variances, staff accountability |
| Templates | Manager / Future Admin | Preview and lightly edit checklist structure | Add task/checklist, duplicate, set recurrence |
| Records / History | Manager | Review missed work and trends | View recent days, repeated misses, completion history |

## Core User Flows

### Manager Runs Today

1. Manager opens Today Dashboard.
2. Dashboard ranks overdue, due-now, due-soon, unassigned, and later work.
3. Manager opens the current F0890 time window.
4. Manager assigns unowned tasks and checks exception states.
5. Manager monitors section progress through the shift.
6. Manager resolves or acknowledges exceptions.
7. Manager closes the day from Sign-Off and Daily Review.

### Manager Creates Or Adds Work

1. Manager opens Templates or Create Work.
2. Manager chooses one-off task, recurring task, or checklist.
3. Manager selects section, due window, assignee/role, recurrence, required data, and instructions.
4. Task appears on Today or future recurring work queue.
5. Any required fields and audit metadata are enforced at completion.

### Employee Completes Work

1. Employee opens My Work.
2. Employee sees assigned tasks sorted by urgency.
3. Employee opens a task, follows instructions, adds required data, and adds initials.
4. If data is required, completion remains blocked until entered.
5. If work cannot be completed, employee flags issue with reason/comment.
6. Manager sees the exception on Alerts and Dashboard.

### Out-Of-Range Temperature

1. Employee or manager enters a temperature reading.
2. OpsFlow detects an out-of-range value.
3. The task switches to corrective-action required.
4. User records action taken, note, initials, and optional evidence.
5. Manager acknowledges the action.
6. Daily Review stores the reading, action, and acknowledgement.

### Deployment Before Manager Leaves

1. Opening manager opens Deployment section.
2. System highlights unassigned deployment tasks.
3. Manager assigns tasks to staff.
4. Section sign-off remains blocked until required assignments exist.
5. Later progress updates flow into Dashboard and Daily Review.

### Closing And Admin Closeout

1. Closing manager opens Closing Checklist.
2. Area-based cleaning tasks are completed with initials and comments where needed.
3. Manager opens Cash/Till and enters counts.
4. Any cash or inventory variance requires a note/acknowledgement.
5. Manager completes closing admin tasks.
6. Sign-Off confirms unresolved blockers, exceptions, and final initials.
7. Daily Review becomes the audit record.

## Data Model Direction

| Entity | Important Fields |
| --- | --- |
| Account | id, displayName, roleProfile, accountKind, storeId, sharedDevice |
| WorkItem | id, title, type, taskFamily, sectionId, dueWindow, recurrence, priority, status, instructions |
| Section | id, name, phase, dueBy, signOffRequired, assignmentRequired, order |
| Assignment | workItemId, assigneeAccountId, assigneeTargetType, assignedBy, assignedAt, reassignedAt |
| Completion | workItemId, completedByAccountId, completedByDisplayName, initials, completedAt, completionRoleProfile, notes, evidenceIds |
| DataCapture | workItemId, captureType, value, expectedRange, unit, result, capturedBy, capturedAt |
| CorrectiveAction | sourceId, reason, actionTaken, ownerId, status, acknowledgedBy, acknowledgedAt |
| Alert | sourceId, type, severity, status, createdAt, resolvedAt |
| Incident | type, severity, linkedTaskId, notes, evidenceIds, escalationStatus |
| AuditEvent | actorId, action, sourceType, sourceId, timestamp, metadata |
| Template | id, name, version, sections, recurrenceRules, activeStoreIds |

## MVP Versus Roadmap

### Store MVP

- Store manager, employee, and Store Account shared-device modes.
- F0890 active daily checklist.
- Task/checklist creation entry point.
- Recurrence and due-window representation.
- Assignment, completion, initials, timestamps, comments, evidence placeholders.
- Temperature, inventory, cash/till, alerts, corrective actions.
- Manager sign-off, Daily Review, recent history/coaching signals.
- Responsive web/mobile layout for Chromebook and mobile.

### Later Admin / Field / Home Office

- Advanced template builder with versioning and approval.
- District manager audit and store-visit workflows.
- Home-office projects, announcements, hierarchy reporting, read tracking.
- Push notifications, offline mode, backend auth/database.
- POS/payroll/bank/thermometer integrations.
- Photo gallery, exportable reports, automated recurring scheduling engine.

## Strategy Changes To Existing Design Flow

The next design-flow artifacts should be updated with these decisions:

- Design Brief should define OpsFlow as a store operations execution system.
- Information Architecture should add Records/History, Corrective Actions, Incidents, and Create Work as explicit concepts.
- Design Tokens should preserve the V1 light-purple direction, but use stronger operational states for alerts, warnings, success, and variance.
- TASKS should prioritize the complete manager/employee execution loop before decorative dashboard polish.
- Frontend Design plan should make mobile and Chromebook layouts equal citizens.

## Acceptance Criteria

The strategy is complete when the next OpsFlow prototype supports, at least with front-end state:

- Ranked today dashboard.
- F0890 time-window timeline.
- Manager task/checklist creation and store-or-employee assignment.
- Employee and Store Account task completion flows, including typed name and initials for shared devices.
- Temperature threshold with corrective action.
- Inventory/product variance capture.
- Cash/till/admin closeout capture.
- Alerts and corrective actions.
- Incident/escalation entry.
- Manager sign-off and Daily Review.
- Recent history/coaching metrics.
- Lightweight template preview/edit.
