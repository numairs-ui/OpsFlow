# F0890 DP Learnings: OpsFlow Store MVP

## Source

This document translates the F0890 daily operations packet into OpsFlow product requirements. F0890 is treated as the proving-ground checklist for the store MVP, while the system should remain configurable for other restaurant checklists.

## Core Lesson

F0890 is not one checklist. It is a full store-day operating system spread across time windows, role accountability, task assignment, required initials, product controls, cleaning standards, cash handling, inventory variance review, and end-of-day verification.

OpsFlow should therefore model restaurant work as **time-bound operational execution**, not as a flat checklist.

## F0890 Operating Structure

### Opening And Setup

F0890 includes early-day setup work such as customer lobby setup, deposits, deployment chart posting, MDOG prep planning, dough planning, till and cash counts, open/neon signs, dated product verification, empty dough tray handling, make-line date checks, and Pizza Academy posting.

Product implications:
- Opening tasks need deadline windows and manager initials.
- Cash, deposit, and till setup need structured capture rather than simple completion.
- Prep, dough, and product dating tasks need evidence of verification.
- Opening should feed the dashboard because misses early in the day create downstream risk.

### Product Management By 11:00 AM

F0890 explicitly groups product-management responsibilities and requires completion by 11:00 AM.

Product implications:
- Sections need due-by times.
- The dashboard should show a section countdown and late state.
- Store managers need a way to sign a section after checking the required work.
- Employees should see only the work they own, while managers see the whole section.

### Ongoing Duties During Lunch

Lunch duties include clean-as-you-go standards, unit cleanliness, dough temperature management, down-stacking/patty placement within 24 hours of delivery, and dough refrigeration when dough reaches 56 degrees.

Product implications:
- Some tasks recur continuously during a shift, not once per day.
- Temperature thresholds must trigger corrective action.
- Corrective action should capture what happened, who acted, when, and whether a manager acknowledged it.
- OpsFlow should support "monitoring" tasks and "complete by" tasks.

### Pre-Rush Walkthrough By 3:30 PM

The pre-rush checklist covers make-line stock, sweeping, trash, paper towels, kitchen tables, prep area, dishes, blaster printer, staff positioning, and promotion awareness.

Product implications:
- Pre-rush should be a named time window with a hard deadline.
- Manager walkthrough mode should let a manager clear several related checks quickly.
- Staff awareness tasks, such as promotion and phone upsell behavior, should support a coach/verify action.
- Task Detail should support short operational instructions, not just labels.

### Deployment Guide

F0890 says deployment tasks must be assigned before the opening manager leaves. Deployment items include Pepsi cooler FIFO, trash, dishes and sanitizer area, bathrooms, walk-in, dough trays, oven wipe-down, order monitors, phones, undershelves, store sweep, clock-out decisions, make-line wells, and safe chemical-use constraints.

Product implications:
- Assignment is a first-class manager workflow, not an admin afterthought.
- A section can require all tasks to be assigned before sign-off.
- Tasks need role/assignee, due window, status, initials, and audit trail.
- Some tasks need safety instructions that remain visible at completion time.
- Long deployment and cleaning standards should be represented as separate tasks or task instructions, because OpsFlow uses a two-level `Checklist -> Task` model.

### Closing Checklist

Closing includes lobby, mat, windows, sweep/mop standards, trouble-area cleaning, chairs/benches, Pepsi cooler, driver area, front counter, slap station, sauce station, make line, dish area, trash cans, late-night trash security note, expiring food disposal, and detailed production/prep/dry-storage sweeping.

Product implications:
- Closing needs dense but scannable section groups.
- Repeated cleaning patterns should be templated, not hand-entered daily.
- Safety/security notes should be highlighted when relevant.
- Photos/comments should be available when a manager wants proof or coaching context.
- Closing completion should feed day closeout readiness.

### Closing Admin Checklist

Closing admin includes cash count, till A/B expected values, remaining store cash locked in time-delay safe, deposit log, Nightly Numbers sheet, Bad Order Log, top five variances on Target Inventory Cost report, closing checklist review with driver staff, tills in safe with drawers open, backup discs, Instant Pay verification, clock-out, and system close.

Product implications:
- Cash/till workflows need expected totals, actual totals, variance, note, and manager initials.
- Inventory variance review needs a structured "top variances" capture.
- Closeout cannot be only task completion; it must include financial/admin verification.
- End-of-day report should summarize operational work, exceptions, variances, and sign-offs.

## Required F0890 Concepts To Model

| F0890 Concept | OpsFlow Product Concept | MVP Treatment |
| --- | --- | --- |
| Daily operating packet | Active daily checklist | Build as the main store-day experience |
| Product Management, Pre-rush, Closing | Time-window sections | First-class dashboard and timeline grouping |
| Initial When Complete | Completion attestation | Initials, timestamp, completed-by |
| Mgr's Initials | Manager verification | Section sign-off and closeout sign-off |
| "Completed by 11:00 AM" / "3:30 PM" | Due windows | Ranked dashboard, late alerts |
| Deployment assigned before manager leaves | Assignment gate | Staff assignment page and section blocker |
| Dough temp 56 degrees | Threshold rule | Corrective action trigger |
| FIFO/date labels/expiration checks | Compliance verification | Task checks with exception option |
| Cash/till/deposit log | Structured data capture | Cash/Till page and closeout summary |
| Target Inventory Cost variances | Variance review | Inventory variance capture |
| Promotions/phone upsells | Coaching verification | Pre-rush coaching task |
| Photos/comments/proof | Evidence | Task evidence placeholder |
| Shared Chromebook completion | Store Account role/profile | Typed employee name + initials required |

## Flows OpsFlow Must Not Miss

1. Manager starts the day and sees ranked time-sensitive work.
2. Manager reviews opening setup, product management, cash setup, and prep planning.
3. Manager assigns deployment work before leaving or changing shift ownership.
4. Employee opens assigned work, follows task instructions, adds required initials, and flags blockers.
5. Manager monitors lunch/ongoing duties, especially temperature and cleanliness issues.
6. Out-of-range dough temperature or failed compliance item creates corrective action.
7. Manager conducts pre-rush walkthrough and verifies staff positioning/promo awareness.
8. Closing team completes detailed area-based cleaning and evidence/comment capture when needed.
9. Closing manager completes cash/till/deposit/admin verification.
10. Manager closes the day only after unresolved exceptions are acknowledged.
11. Daily Review captures what was completed, missed, late, reassigned, or corrected.
12. History shows repeated misses and coaching signals by section, shift, or person.

## MVP Boundaries From F0890

Include in the front-end prototype:
- Today dashboard with F0890 time windows.
- Daily timeline grouped by opening, product management, ongoing duties, pre-rush, deployment, closing, and closing admin.
- Task detail with instructions, initials, required data, comments, attachment placeholder, and audit trail.
- Store Account completion requiring typed employee name and initials.
- Staff assignment for deployment and section ownership.
- Temperature threshold corrective action.
- Cash/till and deposit-style capture.
- Inventory variance capture.
- Manager sign-off and daily review report.
- Lightweight template preview/edit entry point.

Defer:
- Real payroll/POS integration.
- Real bank deposit workflow.
- Bluetooth thermometer integration.
- Push notifications and backend persistence.
- District manager and home-office reporting console.
- Full drag-and-drop template builder.
