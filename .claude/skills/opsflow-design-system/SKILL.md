---
name: opsflow-design-system
description: OpsFlow's frontend design system — the canonical tokens, typography, components, and patterns for the Angular dashboard and field-pwa apps. Read and apply this whenever building or changing any OpsFlow UI (components, screens, forms, styles) so the look stays consistent. This is the project's design system; it overrides generic/default styling choices.
---

# OpsFlow Design System

OpsFlow is a multi-tenant **retail operations** platform — data-dense admin screens (dashboard app) and fast, glanceable field screens (field-pwa). The aesthetic is a warm, calm "operations desk": a cream paper ground, near-black ink, one indigo accent for action, and amber/rust/green reserved for status. Type is **Inter** for enterprise legibility on data-heavy screens, with a monospace used as a structural/label voice.

The implementation lives in each app's global stylesheet — apply these classes, don't reinvent them:
- `frontend/apps/dashboard/src/styles.scss`
- `frontend/apps/field-pwa/src/styles.scss`

> **Single source of truth.** The two stylesheets currently duplicate the shared layer (tokens + base components). Keep them in lock-step when you touch the shared layer. If you're adding a genuinely shared component, prefer lifting the shared layer into a `frontend/libs/ui` SCSS partial both apps `@use` (see *Known drift* below) rather than copy-pasting a third time.

## Tokens

Defined as CSS custom properties on `:root`. **Never hard-code these hex values in a component** — reference the variable.

### Color

| Token | Value | Use |
|---|---|---|
| `--cream` | `#F5EFE0` | app background |
| `--cream-deep` | `#ECE3CC` | hover fills, muted chips, row stripes |
| `--paper` | `#FBF6E9` | card / input surface (sits on cream) |
| `--ink` | `#15131F` | headings, primary text, dark buttons |
| `--ink-soft` | `#2B2839` | body text |
| `--muted` | `#6B6678` | secondary text, labels, placeholders |
| `--line` | `#DDD2B6` | borders, dividers |
| `--indigo` / `--indigo-deep` | `#5B4FE9` / `#3A2FBF` | **the** action accent, links, focus |
| `--indigo-glow` | `rgba(91,79,233,.12)` | focus ring, in-progress fills |
| `--amber` / `--amber-deep` | `#FBBE3D` / `#E89F00` | paused / warning status |
| `--rust` | `#C84A2C` | error, overdue, rejected, destructive |
| `--green` | `#2A7F4F` | success: completed / approved / verified |

Status meaning is fixed: **green = good/done, amber = paused/attention, rust = error/overdue/rejected, indigo = in-progress/active.** Don't repurpose them.

### Type

| Token | Value | Role |
|---|---|---|
| `--sans` | **Inter**, system fallbacks | everything — body, headings, buttons |
| `--serif` | **Inter** (same stack) | headings + `em` accents; kept as a separate token so a future display face can be swapped in one place |
| `--mono` | `JetBrains Mono`, ui-monospace | the **structural voice**: eyebrows, labels, table headers, status pills, meta, empty/loading states |

- Base: `html` = 16px; `body` = **0.9375rem (15px)**, `line-height: 1.55`.
- Headings `h1–h3`: `--serif`, weight ~380, `letter-spacing: -0.025em`, tight `line-height: 1.08`.
- `h4`: `--sans`, weight 700.
- `em` is an **indigo italic accent**, not generic emphasis — use it to highlight one word in a heading.
- The monospace + `text-transform: uppercase` + wide `letter-spacing` (~0.12–0.18em) is the recurring "schematic label" treatment. Reach for it for eyebrows, table headers, pills, and meta — not for prose.

### Geometry & motion

`--radius: 14px`, `--radius-lg: 22px`, pills/buttons use `100px` (fully round). Standard easing `--ease: cubic-bezier(0.22, 1, 0.36, 1)`. Inputs use a tighter `8px` radius. Hover lift is `translateY(-1px)` (buttons) / `translateY(-3px)` (cards).

> **Vestigial leftover, harmless but don't chase it:** many heading/serif-styled elements across the dashboard (`.stat-val`, `.gauge-num`, `page__title`, etc.) still carry `font-variation-settings: 'opsz' 72, 'SOFT' 30` and `font-style: italic`. `'opsz'`/`'SOFT'` are **Fraunces**-specific variable-font axes from before the Inter migration mentioned below — `--serif` now maps to plain Inter, which doesn't define those axes, so the browser silently ignores them. The `italic` still renders (Inter does support italic), which is why these elements still read as "the serif/display voice" despite the token being Inter. This is dead-but-harmless CSS, not a bug — don't spend time "fixing" the variation-settings, and don't assume there's a real variable serif font in play if you see them.

## Shell chrome (sidebar / tabbar / mobile nav) — same tokens, no exceptions

**Found twice already (2026-07-18): both the supervisor-shell and manager-shell components had their entire chrome — sidebar, mobile tabbar, page background — hardcoded to an unrelated slate-blue Tailwind-style palette (`#f8f9fa` bg, `#1e293b` sidebar/tabbar, `#3b82f6`/`#60a5fa` accent, `#94a3b8`/`#64748b` muted text), byte-identical in both files.** This almost certainly came from copy-pasting the same non-cream starter/scaffold twice and never revisiting it — admin-shell was the only one actually built against this design system. Both have since been fixed to match. If a new role shell is ever added (or any shell-level component is touched), use exactly this mapping — don't reach for generic slate/blue defaults:

| Element | Token |
|---|---|
| Page/shell background | `var(--cream)` |
| Sidebar / mobile tabbar background | `var(--ink)` |
| Sidebar body text (default) | `rgba(245,239,224,0.6–0.7)` (cream at reduced opacity, *not* a grey hex) |
| Sidebar section-label (e.g. "OVERVIEW", "CONFIGURATION") | `rgba(251,190,61,0.6)` — amber-tinted, mono, uppercase |
| Active nav link | background `rgba(91,79,233,0.15)` (indigo tint), text `var(--cream)`, left border `var(--amber)` |
| Active mobile-tabbar icon/label | `var(--amber)` |
| Role badge (sidebar brand area) | background `var(--amber)`, text `var(--ink)` |
| Logout button border/hover | `rgba(245,239,224,0.18)` border → `var(--amber)` on hover |
| Brand/logo text | `var(--serif)` (Inter), `var(--cream)` |

Before touching any shell file, `grep` it for raw hex colors (`#[0-9a-f]{3,6}`) first — if you find any outside of `rgba(21,19,31,...)` overlay/shadow values, that's this exact drift recurring, not a new pattern to design.

## Mobile patterns (added 2026-07-18)

Concrete, reusable patterns from a full mobile-viewport (390×844) audit pass — reach for these rather than inventing a new mobile treatment each time:

- **Wide data tables:** wrap every `<table class="table">` in a `<div class="table-wrap">` with `overflow-x: auto; overflow-y: hidden; -webkit-overflow-scrolling: touch;`. This lets a table scroll horizontally *inside its own card* on mobile instead of blowing out the page width (the difference between a contained, scrollable table and one that clips or drags the whole layout sideways). For simple record-list pages (not dense analytics tables), the stronger pattern already established in `admin-listing.scss` swaps to a `.list-cards`/`.list-card` stacked-card layout below 768px instead — use table-wrap for dense multi-column analytics tables, list-cards for simple entity lists.
- **Stat tile strips** (`app-stats-strip`/`app-stat-tile`, `frontend/libs/ui/core/src/lib/stat-tile/`): already wrap correctly below 640px (`flex: 1 1 45%`) — reuse this shared component rather than hand-rolling a stat row; two local component-owned copies existed before this session's cleanup pass and both needed the same fix independently.
- **Multi-item pill/tab toggles** (e.g. the Templates type toggle): don't let a toggle wrap to a second line on mobile — it reads as broken, not responsive. Shrink padding/font-size at a tighter breakpoint instead, and give the container `overflow-x: auto` as a safety net for very narrow phones. `flex-wrap: wrap` on a pill-tab row is the wrong instinct here.
- **Full-viewport drawers/slide-overs** (`.slide-over`): a fixed pixel width (e.g. `420px`) with only `right: 0` set has no mobile fallback and sits partially off-canvas below that width, clipping content on the left. Any `.slide-over`-style panel needs `width: 100vw; left: 0;` under a mobile breakpoint.
- **Mobile-only buttons need their own reset:** a `<button>` sharing a class designed for `<a>` tags (e.g. a nav-style "link" class) will fall back to native button chrome (light background, outset border) unless the shared class explicitly resets `background`, `border`, `width`, `text-align`, `cursor`. This produced a visible unstyled white block in the More-sheet's "+ Create" row — check for this whenever a `<button>` and `<a>` share a nav-link class.
- **"Modular" mobile dashboards:** collapsing a multi-column grid to a single stacked column fixes overflow but not perceived bulk — every card still renders at full desktop padding/type-scale, reading as "too big, too much scroll." A real mobile pass needs both: (1) group same-weight cards (e.g. single-stat tiles) into a 2-up sub-grid instead of full-width stacking, and (2) reduce card padding and heading/number font-size at the mobile breakpoint, not just the column count.

## Components

Use the existing class; only add component-scoped SCSS for things genuinely unique to that screen.

### Buttons — `.btn` + a variant
Variants: `.btn-ink` (primary dark→indigo on hover), `.btn-indigo` (accent), `.btn-amber`, `.btn-ghost` (outline), `.btn-danger` (rust, dashboard). Fully round, weight 600.
**Required states (all buttons):** `:focus-visible` → 2px indigo outline (offset 2px); `:active` → `scale(0.98)`; `:disabled` → reduced opacity + `pointer-events: none`. Keep an action's label stable through its flow (the button that says "Publish" yields a "Published" toast).

### Cards — `.card`
`--paper` surface, `--line` border, `--radius`, hover → indigo border + `-3px` lift.

### Status pills — `.pill` + `.pill--<status>`
Round, mono, uppercase, bordered in `currentColor`. Map the domain status to the fixed color: `--completed/--approved/--verified` green, `--inprogress` indigo, `--pending/--draft` muted, `--overdue/--rejected/--inactive` rust, `--paused` amber. (Statuses come from the domain — see `CONTEXT.md`.)

### Forms
Two patterns coexist; **prefer `.form-group` + `.input-field` for new forms** (the updated, app-wide standard):
- `.form-group` — column layout, `gap: .5rem`, bottom margin. `label` is 0.875rem/600 in ink.
- `.input-field` — full-width input/select/textarea: paper bg, `--line` border, **8px** radius. `:hover` → muted border; `:focus` → indigo border + `0 0 0 3px var(--indigo-glow)` ring; `:disabled` → cream-deep bg.
- Legacy `.field` (dashboard) uses a mono-uppercase label and 14px-radius input; leave existing usages, but build new forms with `.form-group`/`.input-field`.

### Tables — `.data-table` (dashboard)
Mono uppercase `th`, `--line` row dividers, `--cream-deep` row hover. For dense admin lists.

### Structural & state bits
- `.eyebrow` — mono uppercase kicker with a leading rule; `--amber`/`--muted` modifiers. Precedes section titles.
- `.meta` — mono, small, muted (timestamps, counts).
- `.state-loading` / `.state-error` — the canonical empty/loading and error blocks. Errors explain what went wrong in the interface's voice; empty states invite an action, never dead-end.
- `.spinner`, themed `::-webkit-scrollbar` — provided; reuse.

## Layout & writing

- Generous whitespace on cream; let cards and the single indigo accent carry the page. Cap intensity — one bold thing per view, everything else quiet.
- **Role-based density.** The field-pwa `/dashboard` scales by role: admins/managers get a wide aggregate view (metrics, completion bars, pending approvals); field workers get focused, action-first cards ("You have N tasks due"). Match information density to who's looking.
- **Copy is design material.** Name things by what the user controls (a person "submits a form", not "posts a FormSubmission"). Active voice on controls ("Record deposit", not "Submit"). Use the domain vocabulary in `CONTEXT.md` — don't drift to synonyms.

## Recent updates folded in (2026-06)

- Type migrated from Fraunces/Manrope → **Inter** for data-heavy legibility; body standardized to 15px.
- Added the `.form-group` / `.input-field` form standard.
- Buttons gained `:focus-visible`, `:active`, and robust `:disabled` states.
- field-pwa gained a role-based `/dashboard` landing route.

## Known drift (reconcile, don't propagate)

The two `styles.scss` files are maintained in parallel and have drifted before (the Inter migration first landed only in field-pwa). When you change the shared layer, change **both**, or extract a shared `frontend/libs/ui` SCSS partial that each app `@use`s — that's the durable fix and the recommended next step. Dashboard-only extras today: `.data-table`, `.btn-danger`, the legacy `.field` form pattern.

**Status as of 2026-07-18 — reconciled, not aspirational:** a mobile design/re-skin pass went through every admin dashboard screen directly (reading each component's SCSS, not just eyeballing it) and fixed everything that was actually still drifted: Settings, Roster, Deposit (hardcoded hex → tokens), and — the bigger find — the entire supervisor/manager shell chrome, which had never been on this design system (see "Shell chrome" above). Stores/Users/Checklists/Templates/Regions/Form Submissions, previously listed here as drifted, are confirmed clean. If something looks visually "off" again in the future: check the component's own SCSS for raw hex colors before assuming a bigger redesign is needed — both real drift incidents so far turned out to be exactly that.
