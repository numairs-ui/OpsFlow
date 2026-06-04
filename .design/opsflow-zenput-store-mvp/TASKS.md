# Build Tasks: Zenput-Informed OpsFlow Store MVP

## Build Order

The next prototype pass should prioritize the manager/employee execution loop before expanding dashboard polish.

## Phase 1: App Shell And Data Model

- [ ] Add/update sample data for F0890 sections, tasks, staff, assignments, alerts, corrective actions, incidents, records, and templates.
- [ ] Add sample account/profile data for Employee, Store Account, Store Manager/GM, Supervisor/Ops Lead, Auditor, and System Admin.
- [ ] Treat Store Account as a role/profile in the same account model, not a separate technical account type.
- [ ] Define task states, section states, alert states, and capture types in front-end state.
- [ ] Define assignment target types for Store Account and individual account targets.
- [ ] Support manager, employee, and Store Account modes in local state.
- [ ] Ensure responsive navigation supports Chromebook sidebar and mobile bottom tabs.

Acceptance:
- The app can render manager, employee, and Store Account experiences from one shared store-day data model.

## Phase 2: Today Dashboard

- [ ] Rank work by critical alerts, overdue, current window, unassigned, due soon, and later today.
- [ ] Show F0890 phase progress.
- [ ] Add alerts strip for overdue, unassigned, out-of-range, cash variance, inventory variance, and corrective actions.
- [ ] Add staff workload snapshot.
- [ ] Add closeout readiness card.
- [ ] Add coaching signal panel for repeated misses and late sections.

Acceptance:
- A manager can immediately see what needs attention now and where to go next.

## Phase 3: Daily Timeline

- [ ] Build F0890 timeline sections: Opening Setup, Product Management, Ongoing Lunch Duties, Pre-Rush, Deployment, Closing, Closing Admin.
- [ ] Show due times and section status.
- [ ] Add task rows/cards with assignee, deadline, status, required data, initials, and exception state.
- [ ] Add quick actions for assign, complete, flag issue, and open detail.
- [ ] Add section sign-off readiness.

Acceptance:
- F0890 feels like a real operating checklist organized by the restaurant day.

## Phase 4: My Work And Task Detail

- [ ] Add Employee My Work sorted by urgency.
- [ ] Add Store Account shared-device My Work for store-level tasks.
- [ ] Add Task Detail with instructions, initials, comments, evidence placeholder, and audit trail.
- [ ] Require typed employee name and initials before Store Account completion.
- [ ] Require fields before completion where relevant.
- [ ] Add "cannot complete" blocker flow.
- [ ] Add manager review state for tasks needing acknowledgement.

Acceptance:
- Employees can complete work quickly, and managers get accountable records.

## Phase 5: Create Work And Template Entry

- [ ] Add Create Work entry point from dashboard/navigation.
- [ ] Support one-off task creation in front-end state.
- [ ] Support simple checklist creation with multiple tasks.
- [ ] Support recurring work shell with due window and recurrence label.
- [ ] Support duplicate-from-F0890 template flow.
- [ ] Add lightweight template preview/edit for title, section, due window, role, recurrence, and required fields.
- [ ] Block personal task/checklist creation from Store Account context.
- [ ] Use two-level checklist structure only: Checklist -> Task.

Acceptance:
- Store managers can add new tasks/checklists for employees without a backend.

## Phase 6: Assignment

- [ ] Build Staff Assignment view.
- [ ] Show unassigned and at-risk tasks first.
- [ ] Add staff workload and role chips.
- [ ] Support assign, reassign, and unassign to Store Account and individual employee targets in front-end state.
- [ ] Add bulk assignment for deployment and closing sections.
- [ ] Add assignment gate for deployment before manager sign-off.

Acceptance:
- Managers can explicitly assign work and see ownership gaps.

## Phase 7: Operations Data Capture

- [ ] Build Temperature Log with target range and F0890 dough 56-degree threshold.
- [ ] Trigger corrective action on out-of-range values.
- [ ] Build Inventory/Product Controls with FIFO, expiration, MDOG prep, 3-day dough planning, and variance notes.
- [ ] Build Cash/Till with expected/actual totals, till A/B values, store cash, deposit, and variance notes.
- [ ] Feed data capture outcomes into Alerts and Daily Review.

Acceptance:
- Required operational data is captured in structured forms and creates exceptions when needed.

## Phase 8: Alerts, Corrective Actions, Incidents

- [ ] Build Alerts list with filters for overdue, due soon, unassigned, variance, out-of-range, incident, and blocker.
- [ ] Add corrective action detail with action taken, owner, note, initials, and manager acknowledgement.
- [ ] Add lightweight incident report form.
- [ ] Link alerts back to source task or operation.
- [ ] Include resolved/unresolved state in dashboard and review.

Acceptance:
- Exceptions become visible, owned, and resolved instead of disappearing into notes.

## Phase 9: Sign-Off And Daily Review

- [ ] Build section sign-off screen.
- [ ] Block sign-off for incomplete required work unless manager acknowledges exception.
- [ ] Build day closeout with initials and final note.
- [ ] Build Daily Review with completion, late/missed work, variances, corrective actions, incidents, staff accountability, and sign-off record.
- [ ] Add Records preview for recent days and coaching signals.

Acceptance:
- The manager can close the day with an audit-ready summary.

## Phase 10: Visual Polish And Verification

- [ ] Apply light-purple V1 style with stronger operational state colors.
- [ ] Tighten mobile spacing, sticky actions, and touch targets.
- [ ] Tighten Chromebook layout with dense but readable tables/cards.
- [ ] Verify text does not overflow on mobile or web.
- [ ] Run Expo web export or build check.
- [ ] Review prototype against Zenput learnings and F0890 flows.

Acceptance:
- The prototype looks polished, works on mobile and Chromebook, and covers the required product flows.
