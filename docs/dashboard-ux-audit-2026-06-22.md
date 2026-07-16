# OpsFlow Dashboard — UX + Code Audit
> Generated 2026-06-22 · Applied: UX Heuristics, Accessibility (WCAG 2.1 AA), Cognitive Load, Persuasive UX, General Design Review

---

## Verdict: Needs Work

The design system foundation is excellent — strong type scale, solid token set, clean brand identity, good use of Angular Signals. The issues are not about visual taste; they're about **functional bugs that will break rendering**, **jargon leaking into user-facing copy**, **accessibility gaps in interactive components**, and **missing safety rails on irreversible financial actions**. All fixable, none architectural.

---

## Part 1 — Critical Functional Bugs

These are code defects, not design preferences. They will produce broken or confusing UI today.

---

### BUG-01 · `scoreColor()` Returns CSS Class Names, Not Color Values
**File**: `manager/overview/overview.component.html`
**Severity**: Critical

```html
<!-- BROKEN: passing a CSS class string as an SVG attribute -->
[attr.stroke]="scoreColor(d.completionRate)"
[style.color]="scoreColor(d.completionRate)"
```

`scoreColor()` in the supervisor's `overview.component.ts` returns `'score--green'`, `'score--amber'`, `'score--red'` — CSS class names. The manager overview uses these as SVG `stroke` and inline `color` values, which makes both expressions silently invalid. The SVG ring will render with no stroke color and the percentage number will have no color.

**Fix**: Split into two functions, or make `scoreColor()` return a hex value when used for SVG/style binding.

```typescript
// option A — use hex values for SVG/style binding
scoreColor(rate: number): string {
  if (rate >= 0.8) return '#2A7F4F';   // --green
  if (rate >= 0.6) return '#E89F00';   // --amber-deep
  return '#C84A2C';                    // --rust
}

// option B — separate concerns
scoreColorClass(score: number): string { ... }   // returns 'score--green' etc.
scoreColorHex(rate: number): string { ... }      // returns hex for SVG/style
```

---

### BUG-02 · Raw Internal IDs Exposed to Users
**Files**: `manager/deposit/deposit.component.html`, `shared/form-submissions/form-submissions.component.html`
**Severity**: High

```html
<!-- deposit: shows UUID like "3f7b2c1a-..." -->
<div class="today-card__meta">Submitted {{ dep.submittedAt | date:'h:mm a' }} by {{ dep.submittedByManagerId }}</div>

<!-- deposit history table -->
<td class="meta-cell">{{ dep.submittedByManagerId }}</td>
```

Users see internal GUIDs like `3f7b2c1a-9d4f-4b2e-a1c3-...` instead of a human name. This is a trust and clarity problem — managers can't tell if this was submitted by them or another manager on the same store.

**Fix**: The API response should include a `submittedByManagerName` field. Update the DTO and display that instead. If the API can't return a name, display "You" when the UUID matches `auth.currentUser()?.id`, otherwise "Another manager."

---

### BUG-03 · Backend Enum Values Leaked Verbatim into Task Status Badges
**File**: `manager/overview/overview.component.html` (task table)
**Severity**: High

```html
<span class="badge" [class.badge--danger]="t.status === 'CorrectiveActionRaised'">
  {{ t.status }}   <!-- renders: "CorrectiveActionRaised" -->
</span>
```

`CorrectiveActionRaised` and `Overdue` are .NET backend enum names. No real-world user calls it that.

**Fix**: Add a pipe or a `statusLabel()` helper:

```typescript
statusLabel(status: string): string {
  const map: Record<string, string> = {
    'Open': 'Open',
    'Overdue': 'Overdue',
    'CorrectiveActionRaised': 'Action Raised',
    'Completed': 'Done',
  };
  return map[status] ?? status;
}
```

---

### BUG-04 · Elapsed Time Shows Minutes Only — No Hour Handling
**File**: `manager/overview/overview.component.html`
**Severity**: Medium

```html
<td class="elapsed-cell">{{ t.elapsedMinutes }} min ago</td>
```

A task 90 minutes overdue shows "90 min ago." At 180 minutes it shows "180 min ago." Neither is how humans communicate time.

**Fix**:
```typescript
elapsedLabel(minutes: number): string {
  if (minutes < 60) return `${minutes}m ago`;
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return m > 0 ? `${h}h ${m}m ago` : `${h}h ago`;
}
```

---

## Part 2 — Accessibility Findings (WCAG 2.1 AA)

---

### A-01 · Slide-Overs Are Not Accessible Dialogs
**Files**: `shared/form-submissions/form-submissions.component.html`
**Severity**: High — WCAG 1.3.1 (Info and Relationships), 2.1.1 (Keyboard), 4.1.2 (Name, Role, Value)

Both slide-overs (template picker and detail) are plain `<div>` elements. They have no:
- `role="dialog"` or `aria-modal="true"`
- `aria-labelledby` pointing to the `<h2>` inside
- Focus trap — Tab walks straight out of the modal into the obscured background
- Escape key handler to close

Screen reader users won't know they're in a modal. Keyboard users can tab into background content.

**Fix**:
```html
<div class="slide-over"
     role="dialog"
     aria-modal="true"
     [attr.aria-labelledby]="'detail-title'"
     (keydown.escape)="closeDetail()">
  <div class="slide-over__header">
    <h2 id="detail-title">...</h2>
```

Add a `cdkTrapFocus` directive (from Angular CDK) or implement manual focus management — move focus to the first interactive element inside on open, restore focus to the trigger on close.

---

### A-02 · Clickable Table Rows Are Not Keyboard Accessible
**File**: `shared/form-submissions/form-submissions.component.html`
**Severity**: High — WCAG 2.1.1

```html
<tr class="clickable-row" (click)="openMine(s)">
```

Click-only `<tr>` elements are invisible to keyboard and screen reader users. `<tr>` has no implicit role that accepts keyboard events. Tab won't land here; Enter won't trigger.

**Fix**: Add `tabindex="0"`, `role="button"`, and a keydown handler:

```html
<tr class="clickable-row"
    tabindex="0"
    role="button"
    [attr.aria-label]="'Open ' + s.formTemplateName"
    (click)="openMine(s)"
    (keydown.enter)="openMine(s)"
    (keydown.space)="openMine(s)">
```

Same fix applies to supervisor overview leaderboard rows if they ever become clickable.

---

### A-03 · Inline Error Messages Not Programmatically Associated to Inputs
**File**: `login/login.component.html`
**Severity**: Medium — WCAG 1.3.1, 4.1.3

```html
<input [attr.aria-invalid]="email.invalid && email.touched ? 'true' : null" />
@if (email.invalid && email.touched) {
  <span class="error-text">Enter a valid email address.</span>
}
```

`aria-invalid` is correctly set, but the error text span has no `id`, and the input has no `aria-describedby` pointing to it. Screen readers announce the field is invalid but don't automatically read the error message.

**Fix**:
```html
<input aria-describedby="email-error" [attr.aria-invalid]="..." />
<span id="email-error" class="error-text" role="alert">...</span>
```

---

### A-04 · Decorative Character Icons Used as Meaningful Indicators
**Files**: Multiple
**Severity**: Medium — WCAG 1.4.1 (Use of Color), 1.1.1 (Non-text Content)

| Character | Context | Problem |
|---|---|---|
| `&#9679;` (●) | Sidebar nav icons | No semantic meaning; purely decorative noise |
| `&#33;` (!) | Deposit "no deposit" status | Meaning depends entirely on color context |
| `&#10003;` (✓) | Deposit "logged" status | Same issue — color confirms meaning |
| `&#9888;` (⚠) | Supervisor alert panel header | No `aria-hidden` or `aria-label` |

**Fix**: Add `aria-hidden="true"` to decorative characters. For meaningful status icons, add a visually hidden label:

```html
<!-- sidebar nav — mark as decorative -->
<span class="sidebar__icon" aria-hidden="true">&#9679;</span>

<!-- deposit status — make meaning explicit -->
<div class="stat-value" aria-label="Deposit logged">&#10003;</div>
<div class="stat-value" aria-label="No deposit today">&#33;</div>
```

Long-term: replace `&#9679;` sidebar dots with actual SVG icons that communicate meaning.

---

### A-05 · SVG Ring Has No Accessible Text
**File**: `manager/overview/overview.component.html`
**Severity**: Medium — WCAG 1.1.1

```html
<svg class="ring" viewBox="0 0 100 100">
  <!-- No title, no aria-label, no role -->
</svg>
<div class="ring-label">
  <div class="ring-pct">{{ d.completionRate | percent }}</div>
```

The SVG communicates completion visually. Screen readers will ignore it, but the adjacent text `ring-pct` does repeat the value — so this is a partial win. The SVG itself should be hidden from assistive tech since the adjacent label covers it.

**Fix**:
```html
<svg class="ring" viewBox="0 0 100 100" aria-hidden="true" focusable="false">
```

---

### A-06 · Boolean Toggle Buttons Lack Toggled State
**File**: `shared/form-submissions/form-submissions.component.html`
**Severity**: Medium — WCAG 4.1.2

```html
<button type="button" class="bool-btn" [class.bool-btn--active]="getFieldValue(field.id) === 'true'"
  (click)="setFieldValue(field.id, 'true')">Yes</button>
<button type="button" class="bool-btn--no" [class.bool-btn--active]="getFieldValue(field.id) === 'false'"
  (click)="setFieldValue(field.id, 'false')">No</button>
```

Selection state is communicated only via CSS class — screen readers don't know which button is "selected." The `bool-btn--active` class has no ARIA equivalent.

**Fix**:
```html
<div role="group" [attr.aria-labelledby]="field.id + '-label'">
  <button type="button" [attr.aria-pressed]="getFieldValue(field.id) === 'true'" ...>Yes</button>
  <button type="button" [attr.aria-pressed]="getFieldValue(field.id) === 'false'" ...>No</button>
</div>
```

---

### A-07 · Tab Component Missing ARIA Role and State
**File**: `shared/form-submissions/form-submissions.component.html`
**Severity**: Low-Medium — WCAG 4.1.2

```html
<button class="tab" [class.tab--active]="activeTab() === 'mine'" (click)="switchTab('mine')">My Submissions</button>
```

Active state is CSS-only. Add `role="tab"`, `aria-selected`, and `role="tablist"` on the container.

```html
<div role="tablist" aria-label="Submissions view">
  <button role="tab" [attr.aria-selected]="activeTab() === 'mine'" ...>My Submissions</button>
  <button role="tab" [attr.aria-selected]="activeTab() === 'review'" ...>Pending Review</button>
</div>
```

---

## Part 3 — UX Heuristics Findings

---

### UX-01 · No Confirmation Step Before Recording a Deposit (H5 — Error Prevention)
**File**: `manager/deposit/deposit.component.html`
**Severity**: High

The "Record Deposit" button submits immediately on click. Bank deposits are:
- Financial records
- Irreversible (no delete endpoint)
- Potentially submitted with a typo ($12.00 instead of $1,200.00)

There is no confirmation step, no "are you sure?" gate, and no undo after submission. A mistyped amount creates a permanent bad record.

**Fix — Option A (inline confirmation)**: After clicking "Record Deposit," transform the button area to show a summary line and two buttons:

```
  You're about to record a deposit of $1,200.00
  [Cancel]  [Confirm Deposit]
```

**Fix — Option B (amount format guard)**: At minimum, add a format preview that shows the typed number rendered as currency before submission:

```html
<p class="amount-preview" *ngIf="amount()">You are recording: {{ amount() | currency }}</p>
```

Option A is recommended for a financial feature.

---

### UX-02 · No Success Feedback After Deposit Is Recorded (H1 — Visibility of System Status)
**File**: `manager/deposit/deposit.component.ts`
**Severity**: Medium

After `submit()` succeeds, `todayDeposit.set(d)` causes the form to disappear and the status card to change. No toast, no animation, no "Deposit recorded!" message. The UI just rearranges silently. Users may click again thinking the form glitched.

**Fix**: Add a brief confirmation state:
```typescript
readonly justSubmitted = signal(false);

next: (d) => {
  this.todayDeposit.set(d);
  this.justSubmitted.set(true);
  setTimeout(() => this.justSubmitted.set(false), 4000);
}
```

In template, show a green banner for 4 seconds: `"Deposit of {{ dep.amount | currency }} recorded at {{ dep.submittedAt | date:'h:mm a' }}."`

---

### UX-03 · Sidebar Shows Role, Not User Identity (H6 — Recognition Over Recall)
**Files**: `manager/manager-shell/manager-shell.component.html`, `supervisor/supervisor-shell/supervisor-shell.component.html`
**Severity**: Medium

```html
<span class="sidebar__user-email">{{ roleLabel(user()?.role) }}</span>
```

The sidebar footer shows "Manager" (the role label). The class name says `user-email` suggesting this was intended to show an email. More importantly: the user's **name** isn't visible anywhere in the shell. In a shared-device retail environment, managers may hand off devices — not knowing who's logged in is a real problem.

**Fix**: Show name + role in the footer:
```html
<div class="sidebar__user">
  <span class="sidebar__user-name">{{ user()?.firstName }} {{ user()?.lastName }}</span>
  <span class="sidebar__user-role">{{ roleLabel(user()?.role) }}</span>
</div>
```

Also: show the **store name** (for manager) or **region name** (for supervisor) prominently in the sidebar brand area or page heading. A manager needs to know which store they're managing at a glance.

---

### UX-04 · Pagination Has No Total Count (H1 — Visibility of System Status)
**File**: `manager/deposit/deposit.component.html`
**Severity**: Low-Medium

```html
<span class="page-info">Page {{ page() }}</span>
```

"Page 1" with no denominator. Users can't tell if there are 2 pages or 20.

**Fix**:
```typescript
readonly totalPages = computed(() => Math.ceil(this.totalCount() / 14));
```
```html
<span class="page-info">Page {{ page() }} of {{ totalPages() }}</span>
```

---

### UX-05 · Leaderboard Composite Score Has No Context (H6 — Recognition Over Recall)
**File**: `supervisor/overview/overview.component.html`
**Severity**: Low-Medium

```html
<span class="score-badge {{ scoreColor(s.compositeScore) }}">{{ s.compositeScore }}</span>
```

A number like "73" or "91" is shown with no unit, no legend, no tooltip. Is this out of 100? Is 73 good or bad? What does it include?

**Fix**: Add a `title` attribute as a minimum: `title="Composite score out of 100 — weighted average of completion rate, overdue tasks, and deposit compliance"`. Long-term, add an info icon that opens a legend popover.

---

### UX-06 · CSS Class Naming Inconsistency Across Components (H4 — Consistency)
**Severity**: Low (but accumulates)

The codebase uses two different BEM separator styles for the same structural pattern:

| Component | Page Header Class |
|---|---|
| Manager Overview | `.page-header`, `.page-title`, `.page-subtitle` |
| Deposit | `.page-header`, `.page-title`, `.page-subtitle` |
| Form Submissions | `.page__header`, `.page__title`, `.page__subtitle` |

Table class is `.table` in components but `.data-table` in `styles.scss` (global). The global styles define `.data-table` but components use `.table` — so the shared global table styles are never applied.

**Fix**: Standardize on `.page-header` / `.page-title` / `.page-subtitle` (no double-underscore) and `.data-table` (or update `styles.scss` to `.table`). Pick one and sweep.

---

### UX-07 · Two Nearly Identical Shell Components Not Shared (H4 — Consistency)
**Files**: `manager-shell.component.*`, `supervisor-shell.component.*`
**Severity**: Low (code quality / maintainability)

Both shells are structurally identical: brand header, role tag, nav list, footer with user info and logout. Any change to one must be mirrored in the other.

**Fix**: Create a `shared/app-shell/app-shell.component` that accepts `navItems: NavItem[]` as input. Both Manager and Supervisor shells become thin wrappers providing their specific nav config.

---

## Part 4 — Cognitive Load Findings

---

### CL-01 · Slide-Over Mode Is Ambiguous When Fill/Review/View Look Identical
**File**: `shared/form-submissions/form-submissions.component.html`
**Severity**: Medium

Three distinct modes — fill, review, view — share the same slide-over container. Only the `<h2>` header ("Fill Out Form" / "Review Submission" / "Submission Detail") differentiates them. Available actions (Submit, Approve, Reject, Return) vary by mode, but until the user scrolls to the bottom, they don't know what they can do.

**Fix**: Add a mode indicator chip below the header:

```html
<div class="mode-badge mode-badge--{{ detailMode() }}">
  @if (detailMode() === 'fill') { Filling in progress }
  @if (detailMode() === 'review') { Awaiting your review }
  @if (detailMode() === 'view') { Read-only }
</div>
```

Also: don't show the Approval Trail when `detailMode() === 'fill'` and the submission hasn't been submitted yet — it'll be empty and confusing.

---

### CL-02 · Approval Trail Shows During Form Fill Before Any Steps Have Run
**File**: `shared/form-submissions/form-submissions.component.html`
**Severity**: Medium

```html
@if (detail(); as d) {
  <div class="approval-trail">
    @for (step of d.approvalSteps; track step.stepOrder) { ... }
  </div>
}
```

When `detailMode() === 'fill'`, the detail object exists (it's a draft) and the approval trail renders — showing placeholder steps with no actions taken yet. This is noise that increases cognitive load at the exact moment the user needs to focus on filling the form.

**Fix**: Gate the approval trail on `detailMode() !== 'fill'` or `d.approvalSteps.some(s => s.action !== 'Pending')`.

---

### CL-03 · Auto-Refresh Has No Visual Liveness Indicator
**Files**: `manager/overview/overview.component.html`, `supervisor/overview/overview.component.html`
**Severity**: Low-Medium

Both dashboards poll every 60 seconds with only a subtitle line: "refreshes every 60 seconds." If data is stale (network error mid-poll), users have no way to know. If data just refreshed, they can't tell either.

**Fix**: Show a small "last updated at [time]" line that updates after each successful fetch. A subtle pulsing dot on the live indicator adds ambient awareness without dominating the UI:

```typescript
readonly lastUpdated = signal<Date | null>(null);
// in load() next handler:
this.lastUpdated.set(new Date());
```

```html
<p class="live-indicator">Updated {{ lastUpdated() | date:'h:mm:ss a' }}</p>
```

---

### CL-04 · Leaderboard Has No Trend Signals — Score Delta Is Unknown
**File**: `supervisor/overview/overview.component.html`
**Severity**: Low

The leaderboard ranks stores by composite score, but there's no indication of movement. A store ranked #3 today was it #1 last week? Is #7 trending down fast? Without deltas, the leaderboard is a snapshot with no actionable signal for the supervisor.

**Recommendation**: API change required — add `scoreDelta` to `StoreScoreDto`. In the UI, show a small `▲+4` or `▼−12` next to each score badge. This is a persuasive UX win: supervisors will take action when they see a store dropping.

---

## Part 5 — Component-by-Component Quick Reference

| Screen | Status | Key Issues |
|---|---|---|
| **Login** | Solid | Missing `aria-describedby` on inputs (A-03); no "forgot password" link; submit arrow `↗` should be `aria-hidden` |
| **Manager Shell** | Needs work | Shows role not user identity (UX-03); `&#9679;` dots not real icons (A-04); no store name |
| **Manager Overview** | Critical fix needed | `scoreColor()` bug breaks SVG ring (BUG-01); raw enum status values (BUG-03); no elapsed hour handling (BUG-04) |
| **Deposit** | Needs work | Raw manager UUID shown (BUG-02); no confirmation before submit (UX-01); no success feedback (UX-02); no page totals (UX-04) |
| **Supervisor Shell** | Needs work | Same issues as Manager Shell + should share component (UX-07) |
| **Supervisor Overview** | Needs work | Composite score has no context (UX-05); no trend deltas (CL-04); no liveness indicator (CL-03) |
| **Form Submissions** | Needs work | Slide-overs not accessible dialogs (A-01); clickable rows not keyboard accessible (A-02); bool toggles missing aria-pressed (A-06); tabs missing role (A-07); approval trail shown during fill (CL-02); mode ambiguity (CL-01) |

---

## Part 6 — Prioritized Action Plan

### Do First (High Impact, Low Effort)
- [ ] **BUG-01** — Fix `scoreColor()` to return hex values for SVG/style binding
- [ ] **BUG-02** — Replace `submittedByManagerId` UUID display with manager name
- [ ] **BUG-03** — Add `statusLabel()` helper; stop displaying raw enum values
- [ ] **BUG-04** — Add elapsed time hour formatting helper
- [ ] **A-03** — Add `aria-describedby` + `role="alert"` to inline error messages (login + form fields)
- [ ] **A-04** — Add `aria-hidden="true"` to decorative character icons; add `aria-label` to meaningful ones
- [ ] **A-05** — Add `aria-hidden="true"` to SVG ring
- [ ] **UX-02** — Add success toast/banner after deposit is recorded
- [x] **UX-03** — Show user identity (email) in sidebar footer. _Done 2026-07-13 via an `email` JWT claim (resolved at refresh, no schema change). Store/region name in the sidebar still open._
- [ ] **UX-04** — Show "Page N of M" in deposit history pagination
- [ ] **CL-02** — Hide approval trail when `detailMode === 'fill'`

### Plan Carefully (High Impact, More Effort)
- [ ] **A-01** — Add `role="dialog"`, `aria-modal`, `aria-labelledby`, focus trap, and Escape key to all slide-overs
- [ ] **A-02** — Add `tabindex="0"`, `role="button"`, keyboard handlers to all clickable table rows
- [ ] **A-06** — Add `aria-pressed` to boolean Yes/No toggle buttons
- [ ] **A-07** — Add proper `role="tablist"` / `role="tab"` / `aria-selected` to tab component
- [ ] **UX-01** — Add inline confirmation step before deposit submission (financial safety)
- [ ] **UX-07** — Extract shared `AppShellComponent` to eliminate duplication

### Quick Wins (Low Impact, Low Effort)
- [ ] **UX-05** — Add `title` tooltip to composite score badge explaining the scale
- [ ] **UX-06** — Normalize CSS class naming: pick `.page-header` vs `.page__header` and sweep
- [x] **CL-03** — Add "Last updated at [time]" line to dashboard pages. _Done 2026-07-13 (supervisor overview; manager already had it)._
- [ ] **BUG-03** (related) — Fix elapsed time label to show hours when > 60 min

### Invest When Ready (Strategic, More Effort)
- [ ] **CL-01** — Add mode badge chip to slide-over header so fill/review/view is unambiguous
- [ ] **CL-04** — Add `scoreDelta` to store score API and show trend arrows in leaderboard
- [ ] **UX-05** (full) — Add info icon + popover explaining composite score methodology
- [ ] **A-04** (full) — Replace `&#9679;` sidebar dots with a real SVG icon set

---

## Accessibility Quick Wins Summary

```
High risk items requiring immediate fix:
  A-01  Slide-overs: add role="dialog", aria-modal, focus trap, Escape key
  A-02  Clickable rows: add tabindex, role="button", keyboard handlers

Medium risk items:
  A-03  Inputs: link error messages with aria-describedby
  A-04  Icons: aria-hidden on decorative chars, aria-label on meaningful ones
  A-06  Bool toggles: add aria-pressed
  A-07  Tabs: add role="tablist", role="tab", aria-selected

Low risk / easy fixes:
  A-05  SVG ring: aria-hidden="true" + focusable="false"
```

---

*Audit applied frameworks: UX Heuristics (H1–H10) · WCAG 2.1 AA · Cognitive Load / Conversion · Persuasive UX · General Design Review*
