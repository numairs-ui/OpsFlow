# Design Brief: OpsFlow Cross-Platform MVP

## Problem

Restaurant managers are trying to run a shift while standing between staff, prep, cash, food safety, inventory, alerts, and end-of-day accountability. Paper checklists and generic task apps force them to chase status verbally, re-enter operational data, and reconstruct what happened after the fact. Employees need fast, obvious task completion flows; managers need visibility, delegation, exceptions, and proof without slowing the floor down.

## Solution

OpsFlow provides a cross-platform operating cockpit for restaurant daily execution. On mobile and Chromebook, a store manager can open today's operating timeline, assign work, capture required inventory/cash/temperature data, resolve exceptions, and close the day with sign-off. Employees get a simpler task-completion mode that shows what is assigned, what is due, what requires data, and what has been completed. The experience should feel like the digital version of a well-run shift: calm, quick, traceable, and hard to misread.

## Experience Principles

1. Speed with proof -- Every common floor action should be fast, but important actions must leave timestamps, initials, status, and variance context.
2. Manager visibility without dashboard theater -- The manager view should surface what needs attention now, not bury operations in decorative analytics.
3. Configurable structure over one-off screens -- F0890 pizza operations are the proving ground, but the patterns must support other restaurant checklists, time windows, task types, and approval rules.

## Aesthetic Direction

- **Philosophy**: Light, operational, iOS-native clarity adapted for web and mobile. Use the V1 light-purple style guide as the base, with refinements for accessibility, density, and cross-platform ergonomics.
- **Tone**: Calm, authoritative, efficient, and accountable. It should feel built for a busy restaurant manager, not like a consumer habit tracker or a dark control-room dashboard.
- **Reference points**: iOS Settings/List ergonomics, Square-style operational clarity, Notion-like structured data readability, and modern task-management affordances where useful.
- **Anti-references**: Dark/amber command-center styling, decorative SaaS dashboards, marketing-style hero layouts, playful gamified task apps, and spreadsheet-like cramped forms.

## Existing Patterns

The current repo contains an Expo React Native prototype with hardcoded sample data, local state, shared theme files, and reusable primitives. The strategy and roadmap documents describe a broader platform, but this flow targets a polished front-end prototype with clean state boundaries, not a backend build.

- Typography: Current prototype uses system fonts through React Native; the V1 guide specifies SF Pro / Roboto / system UI with 22px screen titles, 17px section headers, 16px body text, 14px meta rows, and 12px badges.
- Colors: Current app is dark/amber, but the chosen direction is the V1 light-purple guide: screen background `#F2F2F7`, card background `#FFFFFF`, brand purple `#6B63D9`, semantic green/red/amber/blue, and neutral borders.
- Spacing: V1 guide uses 16px screen margins, 16px card padding, 56px minimum row hit targets, 52px primary CTAs, and 12-20px vertical rhythm between cards/sections.
- Components: Existing `Button`, `Card`, `Input`, and `StatusBadge` components should be revised rather than discarded. Existing screen concepts should be reorganized around a clearer cross-platform shell.

## Component Inventory

| Component | Status | Notes |
| --------- | ------ | ----- |
| App shell / responsive navigation | Modify | Must support mobile bottom tabs and Chromebook/sidebar or split layout behavior. |
| Manager dashboard | Modify | Should focus on today, exceptions, timeline progress, and closeout readiness. |
| Daily timeline | Modify | Time-window grouped checklist execution is the core operating view. |
| Task detail | Modify | Needs instructions, subtasks, data fields, notes, attachments placeholder, and audit trail. |
| Employee task queue | Modify | Secondary mode optimized for assigned tasks and fast completion. |
| Staff assignment | Modify | Must support assignment, unassignment, workload/status visibility. |
| Inventory capture | Modify | Should model F0890 3-day dough/cheese planning with variance/action states. |
| Cash/till capture | Modify | Denomination counting, expected amount, variance, timestamp, initials. |
| Temperature logger | Modify | Location, target range, reading validation, notes, history. |
| Alerts center | Modify | Due soon, overdue, variance, and completion exceptions with clear actions. |
| Manager sign-off | Modify | Section-level and day-level sign-off with accountability metadata. |
| Daily review report | Modify | End-of-day digest of completion, exceptions, variances, and unresolved items. |
| Template preview/edit entry | New | Lightweight administrative entry point; full builder remains out of MVP. |
| Status badges/chips | Modify | Must support pending, in progress, due soon, overdue, completed, variance, signed. |
| Data row / label-value row | New | Core pattern for dense operational facts on cards and reports. |
| Responsive card/list grid | New | Same information architecture should work on phone and Chromebook without feeling stretched. |

## Key Interactions

- A manager opens OpsFlow and sees today's operating state: current time window, overdue/due-soon items, assigned staff, data capture needs, and closeout readiness.
- The manager enters the Daily Timeline, expands a time window, opens a task, assigns or completes it, and sees immediate status feedback.
- Employees enter a simplified task queue, start tasks, add required readings/notes when prompted, and mark tasks complete.
- Data-capture tasks validate values inline. Temperature readings outside range, cash variance, and inventory shortage create visible exception states.
- Staff assignment updates task ownership and should make workload/status obvious before assigning.
- Alerts are actionable, not just informational: complete, assign, acknowledge, resolve, or view source task.
- Manager sign-off gathers initials/timestamp and summarizes unresolved exceptions before allowing day closeout.
- Template preview/edit exposes checklist structure lightly, while full template building is deferred.

## Responsive Behavior

The product must work well for roughly equal use on mobile devices and Chromebooks.

- Mobile: single-column, bottom navigation, sticky primary actions, large touch targets, progressive disclosure through accordions and detail screens.
- Chromebook/web: use a wider layout with persistent navigation, two-pane patterns where useful, denser dashboard summaries, and detail panels beside lists rather than always replacing the screen.
- Shared behavior: the same tasks, statuses, and forms should appear across breakpoints; only navigation and information density should change.
- Forms: avoid long uninterrupted forms. Group data entry by operational meaning and keep primary actions visible.

## Accessibility Requirements

- Meet WCAG 2.1 AA contrast for text, controls, badges, and semantic states.
- Do not rely on color alone for status; pair color with labels, icons, or shape.
- All interactive elements should have at least 44px touch/click targets; 52-56px is preferred for primary row actions.
- Web/Chromebook layouts must support keyboard navigation, visible focus states, and logical tab order.
- Modals, sheets, and overlays must manage focus and provide clear close/cancel actions.
- Text should remain readable under browser zoom and mobile dynamic type assumptions.

## Out of Scope

- Backend, database, authentication, API integration, offline sync, push notifications, GPS capture, and file upload implementation.
- Angular web application implementation; this flow targets the current Expo/React Native codebase and web-capable prototype behavior.
- Full checklist template builder/admin console. MVP includes only a lightweight template preview/edit entry.
- Multi-store/HQ analytics, supervisor dashboards, payroll, purchasing, accounts payable, and ROIP modules beyond navigation/context references.
- Production security, deployment, and app store packaging.
