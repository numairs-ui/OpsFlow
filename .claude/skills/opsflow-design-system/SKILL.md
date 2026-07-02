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
