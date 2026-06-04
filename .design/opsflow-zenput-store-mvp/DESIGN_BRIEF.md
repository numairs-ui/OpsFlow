# Design Brief: Zenput-Informed OpsFlow Store MVP

## Product Definition

OpsFlow is a responsive restaurant operations execution app for store managers and employees. It helps a manager run today's checklist, assign work, capture required data, catch exceptions, and close the day with accountability.

The first version uses F0890 as the proving-ground operating checklist and Zenput as product inspiration for urgency ranking, recurrence, corrective actions, evidence, history, and manager coaching.

## Audience

Primary:
- Store managers and shift leads using Chromebook and mobile.
- Store employees using mobile-first task completion.
- Store Account users completing store-level work on shared Chromebooks or tablets.

Secondary:
- Future district managers reviewing store execution.
- Future home-office users managing templates, rollouts, announcements, and reporting.

## Platforms

OpsFlow must work well on:
- Chromebook/web, about 50 percent of customers.
- Mobile, about 50 percent of customers.

The UI should not assume a large monitor. Mobile should be an equal design target, especially for My Work, Task Detail, Temperature Log, Corrective Actions, and quick completion.

## Core Job To Be Done

Help a restaurant manager run today's operating checklist, assign work, capture required data, spot exceptions, coach the team, and close the day with an audit-ready record.

## MVP Scope

Include:
- Ranked Today Dashboard.
- Daily Timeline based on F0890 phases.
- Employee My Work.
- Shared Store Account completion with typed employee name and initials.
- Task Detail with instructions, initials, comments, evidence placeholder, audit trail.
- Manager task/checklist creation entry point.
- Staff Assignment.
- Temperature Log and corrective actions.
- Inventory/Product Controls.
- Cash/Till and Closing Admin.
- Alerts and Corrective Actions.
- Incident/Escalation Reporting.
- Manager Sign-Off.
- Daily Review Report.
- Records/History preview and coaching signals.
- Template preview/edit entry point.

Defer:
- Backend auth/database.
- Real push notifications.
- Offline sync.
- POS/payroll/bank integrations.
- Bluetooth thermometer or IoT integration.
- District manager audit app.
- Home-office console.
- Full template builder with versioning and approvals.

## Required Product Behaviors

### Work Ranking

The app should rank work by operational urgency:
1. Critical unresolved exceptions.
2. Overdue required work.
3. Work due in the current time window.
4. Unassigned required work.
5. Due soon.
6. Later today.
7. Future recurring work.

### Accountability

Important actions should show:
- Assignee.
- Completed by.
- Initials.
- Timestamp.
- Manager acknowledgement when required.
- Notes/evidence when required.
- Variance or corrective-action record when something fails.

When work is completed from a shared Store Account, typed employee name and initials are required because the device login is shared.

### Account Structure

OpsFlow uses one account model with role/profile permissions: Employee, Store Account, Store Manager/GM, Supervisor/Ops Lead, Auditor, and System Admin. Store Account is a role/profile for shared-device use, not a separate technical account type.

Tasks and checklists can be assigned to store-level accounts or individual accounts. Store-level assignment is recommended for high-turnover work such as inventory checks, food prep, and basic cleaning.

### Checklist Structure

The system uses a two-level task structure: `Checklist -> Task`. Tasks do not contain nested subtasks. Detailed F0890 procedures should become separate tasks or task instructions.

### F0890 Fit

The prototype should visibly model:
- Opening/setup.
- Product Management due by 11:00 AM.
- Ongoing lunch duties.
- Pre-rush walkthrough due by 3:30 PM.
- Deployment assignment before opening manager leaves.
- Closing checklist.
- Closing admin checklist.

### Manager And Employee Modes

Managers need the whole operating picture: dashboard, timeline, assignment, alerts, records, sign-off, and creation.

Employees need a narrowed experience: assigned work, clear instructions, simple completion, required data entry, and blocker reporting.

Store Account users need the same fast execution flow, with typed name and initials collected before submission.

## UX Direction

Style:
- V1 light purple style guide.
- Operational, polished, fast, and trustworthy.
- Adequate contrast and state colors for alerts, warnings, success, and blocked work.
- Avoid dashboard theater. Metrics must help managers act.

Interaction:
- Dense but readable on Chromebook.
- Single-column, thumb-friendly flows on mobile.
- Sticky primary actions for Task Detail and Sign-Off.
- Segmented controls/tabs for filters.
- Icons for repeated controls where useful.
- No landing page; open directly into the app.

## Success Criteria

The next prototype is successful if a user can:
- See what matters right now.
- Add a new task/checklist and assign it.
- Complete employee work quickly.
- Capture a temperature reading and handle out-of-range corrective action.
- Record cash/till or inventory variance.
- Resolve an alert.
- Sign off sections and close the day.
- Review what was missed, late, corrected, or completed.
