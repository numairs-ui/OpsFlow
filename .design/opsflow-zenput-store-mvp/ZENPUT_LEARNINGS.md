# Zenput Learnings: OpsFlow Store MVP

## Source

This document summarizes product lessons from the Zenput product tour transcript and translates them into design implications for the OpsFlow store-manager and employee MVP. Zenput is treated as a major inspiration source, not a product to copy directly.

## Core Product Lesson

Zenput positions itself as an **operations execution platform**, not merely a checklist tool. The strongest lesson for OpsFlow is that daily restaurant work should be organized around execution, visibility, accountability, and coaching. The app should help store teams know what needs to happen now, complete the work quickly, capture proof, escalate exceptions, and give managers enough history to improve future performance.

## Store-Level MVP Learnings

### 1. The Home View Should Rank Work By Urgency

Zenput’s store dashboard makes it clear what work is assigned today and what is due soon. OpsFlow should do the same.

Implications for OpsFlow:
- The first screen should not be a generic module menu.
- It should show today’s ranked work: overdue, due soon, current time window, assigned tasks, and recurring checklist items.
- Tasks due later in the week or future rollouts can appear lower-priority or in a secondary area.
- Store managers and shift leads should immediately understand what needs attention.

### 2. Tasks Need Due Dates, Recurrence, And Notifications

Zenput emphasizes work assigned by date, time frame, and recurrence, with reminders when critical work is due.

Implications for OpsFlow:
- Tasks/checklists should not be flat lists.
- Each work item should support due time, recurrence, assignment, priority, and status.
- The prototype should visually represent notification/reminder behavior even before backend push notifications exist.
- Daily checklists, weekly cleaning, temperature logs, food safety checks, and special rollouts should share the same task model.

### 3. Digital Data Capture Should Save Time

Zenput’s major store-team value proposition is replacing pen-and-paper logs with faster digital forms, especially for temperature checks and food-safety logs.

Implications for OpsFlow:
- Temperature logging, inventory counts, and cash counts should be designed as fast capture flows.
- Forms should avoid long generic fields when a structured input would be faster.
- Temperature and food-safety forms should feel like walking the line, not filling out paperwork.
- Bluetooth thermometer and IoT integrations are future scope, but the UI should leave room for device-assisted capture later.

### 4. Out-Of-Range Values Should Trigger Corrective Action

Zenput shows that undercooked food or failed temperature values should immediately prompt alerts, corrective action, and escalation.

Implications for OpsFlow:
- Validation should not only show “error”; it should ask “what action was taken?”
- Out-of-range temperatures, cash variances, inventory shortages, and failed checklist items should create visible exception states.
- Corrective actions should include notes, discard/retemp/reheat style actions, reassignment, and manager acknowledgement.
- Above-store escalation is future scope, but store-level alert behavior belongs in the MVP.

### 5. Managers Need Coaching Metrics, Not Dashboard Theater

Zenput highlights completion rates, shift performance, recurring missed items, and top issues as coaching tools for GMs.

Implications for OpsFlow:
- The manager dashboard should include concise, actionable metrics.
- Useful metrics include completion rate, overdue count, repeated misses, top failed checklist items, and shift/person patterns.
- Avoid decorative analytics that do not change manager behavior.
- The MVP should include “areas to coach” or “recurring issues” as a visible pattern, even if powered by sample data.

### 6. Missed-Work History Should Be Easy To Review

Zenput’s recurring-project calendar lets managers see which days work was missed without digging through paper records.

Implications for OpsFlow:
- Daily Review should not be only today’s report; it should hint at history.
- A future calendar/archive should show missed days, late submissions, and completed records.
- In the MVP, a compact “recent completion history” or “missed work” panel can communicate the pattern.

### 7. Incident And Escalation Reporting Belongs Beside Daily Work

Zenput treats incidents and escalations as part of the same operations execution system, replacing texts, calls, email threads, and paper.

Implications for OpsFlow:
- Store managers should have a path to report incidents or issues from the same app they use for checklists.
- Incident reporting can be lightweight in MVP: issue type, notes, optional photo placeholder, affected area/person, and escalation status.
- Escalation should connect to Alerts and Daily Review.
- Full HR/legal/ownership workflows are future scope.

### 8. Photo And Comment Evidence Matters

Zenput uses photos and comments for audits, coaching, corrective action, and visual verification.

Implications for OpsFlow:
- Task Detail should reserve space for attachments/photos and comments.
- Failed tasks and corrective actions should encourage evidence capture.
- Photo gallery/reporting is future scope, but the MVP should not design itself into a text-only corner.

### 9. Communication Should Be Attached To The Work

Zenput emphasizes two-way communication around tasks and announcements, so conversation is connected to the operational item.

Implications for OpsFlow:
- “Ask Manager” and comments should belong inside task context.
- Announcements can remain future scope, but store-level messages tied to work should be represented.
- Avoid separate chat-like experiences that disconnect communication from execution.

## Future-Phase Learnings

### Field / District Manager

Zenput gives field teams mobile access to store performance before visits, audit flows, photo evidence, corrective actions, scoring, and store-level communication.

OpsFlow roadmap implication:
- District manager views should become a future phase.
- Store visit/audit tooling should not be part of the immediate store MVP, but the current data model and IA should not block it.
- Future features should include audits, visit prep, scoring, photo evidence, corrective tasks, and store comments.

### Home Office / Leadership Console

Zenput’s web console supports SOP rollout, projects, hierarchy-level progress, heartbeat dashboards, accountability metrics, reports, photo galleries, and announcements.

OpsFlow roadmap implication:
- Home-office web console is future scope.
- Current MVP should document the eventual need for projects/templates, reporting, hierarchy filters, announcement read tracking, and organization-level insights.
- The store MVP should focus on creating high-quality execution data that a future console can report on.

## What OpsFlow Should Copy In Spirit

- Operations execution as the center of the product.
- Work ranked by time and urgency.
- Fast digital data capture.
- Corrective action when something fails.
- Completion history and coaching insight.
- Communication tied to work.
- Evidence capture where it improves accountability.

## What OpsFlow Should Not Copy Blindly

- A broad all-at-once platform scope in the first MVP.
- Home-office reporting before store execution is excellent.
- Heavy dashboards that slow down shift work.
- Audit/field workflows before the store manager and employee flows are clear.
- Generic checklist creation that ignores restaurant time windows and required data.

## Store MVP Priorities From Zenput

1. Today’s ranked work dashboard.
2. Recurring checklist and task model.
3. Fast employee task completion.
4. Manager assignment and reassignment.
5. Temperature, inventory, and cash data capture.
6. Corrective action and alert handling.
7. Daily review and missed-work history.
8. Manager coaching signals.
9. Lightweight incident/escalation capture.
10. Attachment/comment placeholders for proof.

## Design Flow Impact

The Zenput learnings should change the next design-flow pass in these ways:

- The Design Brief should describe OpsFlow as a store-level operations execution platform, not just a manager checklist tool.
- The IA should make Today, My Work, Corrective Actions, Records, and Create Work first-class concepts.
- The task list should prioritize work ranking, recurrence, corrective action, data capture, and coaching metrics earlier than broad admin/template features.
- Frontend build work should make the dashboard and task execution loop feel complete before expanding into field/home-office features.

## Related Strategy Artifacts

- `F0890_LEARNINGS.md` translates the F0890 daily operations packet into product requirements.
- `OPSFlow_ZENPUT_F0890_STRATEGY.md` combines Zenput product inspiration with F0890 operating flows into the updated store MVP strategy.
