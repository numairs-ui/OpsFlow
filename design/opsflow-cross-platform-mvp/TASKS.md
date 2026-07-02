# Build Tasks: OpsFlow Cross-Platform MVP

Generated from: `.design/opsflow-cross-platform-mvp/DESIGN_BRIEF.md`  
Date: May 6, 2026

## Foundation

- [x] **Apply light-purple token baseline**: Convert the app theme from dark/amber to the V1 light-purple operational system, including light launch surfaces and typography/spacing tokens. _Modifies: `colors.js`, `typography.js`, `app.json`._
- [x] **Build responsive app shell**: Replace the current mobile-only tab shell with a shell that uses bottom tabs on phones and a persistent left rail plus content region on Chromebook/web. _Modifies: `App.js`; reuses: existing screens._
- [x] **Create shared operational primitives**: Update reusable Button, Card, Input, and StatusBadge styles and add reusable header, data row, metric tile, section accordion, and sticky action patterns. _Modifies: existing components; creates: shared layout primitives._

## Core UI

- [x] **Rebuild Today Dashboard**: Create the manager-first home view with current shift status, time-window progress, exceptions, assignment snapshot, and closeout readiness. _Modifies: `DashboardScreen`; depends on: responsive app shell and shared primitives._
- [x] **Rebuild Daily Timeline**: Present F0890 time windows as expandable sections with progress, assignees, deadlines, required data indicators, and task actions. _Modifies: `DailyShiftTimelineScreen`; creates/refines: section accordion and task row._
- [x] **Rebuild Task Detail**: Support instructions, subtasks, data fields, notes, completion states, unable-to-complete reason, and audit trail. _Modifies: `TaskDetailScreen`; reuses: data row, sticky action area, status badge._
- [ ] **Rebuild Staff Assignment**: Show staff workload, unassigned/at-risk tasks, assignment actions, and manager-friendly reassignment feedback. _Modifies: `StaffAssignmentScreen`; reuses: task row and status chips._
- [ ] **Rebuild Inventory Capture**: Model the F0890 3-day dough/cheese planning workflow with on-hand, dates, needs, total, action state, notes, and variance cues. _Modifies: `InventoryManagementScreen`; reuses: data capture panel._
- [ ] **Rebuild Cash/Till Capture**: Create fast denomination counting with expected amount, live total, variance state, initials, and sign-off. _Modifies: `CashManagementScreen`; reuses: mono values and sticky action._
- [ ] **Rebuild Temperature Logger**: Build location/range selection, reading capture, validation feedback, notes, and recent history. _Modifies: `TemperatureLoggerScreen`; reuses: data capture panel and alert state._
- [ ] **Rebuild Alerts Center**: Turn alerts into action cards for overdue, due soon, variance, and completed states with source task navigation. _Modifies: `OverdueAlertScreen`; reuses: alert card and status filters._
- [ ] **Rebuild Closeout Sign-off**: Show readiness blockers, section sign-offs, exception acknowledgement, day-level initials, timestamp, and final notes. _Modifies: `ManagerSignOffScreen`; reuses: section accordion and sticky action._
- [ ] **Rebuild Daily Review Report**: Summarize completion, exceptions, staff accountability, and signed closeout status for manager review. _Modifies: `DailyReviewReportScreen`; reuses: metric tiles and data rows._
- [x] **Add lightweight Template Preview/Edit entry**: Keep full builder out of MVP while providing a template list/preview/edit entry point for the active operating checklist. _Modifies: `ChecklistBuilderScreen`; reuses: section list and data rows._
- [ ] **Refine Employee Task Mode**: Make employee mode a focused task queue with urgency ordering, assigned work, completion flow, and task history. _Modifies: `EmployeeTaskViewScreen`; reuses: task row and task detail patterns._

## Interactions & States

- [x] **Normalize navigation actions**: Ensure dashboard cards, alerts, timeline rows, and task queues navigate consistently across mobile and web shell states. _Modifies: `App.js` and screen props._
- [ ] **Normalize status states**: Use one shared state vocabulary: pending, in progress, due soon, overdue, completed, variance, signed, unable. _Modifies: `StatusBadge` and screen data._
- [ ] **Add validation states to data capture**: Temperature out-of-range, cash variance, inventory shortage/overage, required initials, and required notes should be visible and hard to miss. _Modifies: Inventory, Cash, Temperature, Task Detail._
- [ ] **Add empty and success states**: Alerts, Tasks, Templates, Sign-off, and data-capture screens need clear empty/success confirmations. _Modifies: relevant screens._

## Responsive & Polish

- [ ] **Chromebook layout pass**: Verify 1024px+ web layouts use sidebar/two-pane density and do not stretch mobile cards awkwardly. _Breakpoints: 1024, 1280._
- [ ] **Mobile layout pass**: Verify 375px and common phone widths keep text readable, actions reachable, and bottom navigation stable. _Breakpoints: 375, 390, 430._
- [ ] **Accessibility pass**: Check contrast, focus state, keyboard navigation on web, semantic labels where possible, and 44px+ hit targets. _Required by brief._
- [ ] **Prototype verification**: Run Expo web export and inspect the app for compile/runtime issues after each major slice. _Uses: existing Expo app._

## Review

- [ ] **Design review**: Run `/design-review` against the saved brief after the core flows are built.
