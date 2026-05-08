# MealDynamics OpsFlow — UI Style Guide

**Platform:** React Native (Mobile) | Angular 17 (Web)  
**Derived from:** V1 Figma screens (62 screens studied)  
**Version:** 1.0 | May 2026

> **Note:** The V1 Figma designs use a **light theme** with **purple branding**. This guide reflects the actual screens, not the dark/amber theme described in the strategy doc. The strategy doc's dark theme is planned for a future variant.

---

## 1. Design Philosophy

The V1 design is clean, iOS-native, and operationally focused. It prioritizes:

- **Scannable layouts** — two-column label/value rows for dense data, not paragraphs
- **Minimal chrome** — no colored nav bars, content lives on the page background directly
- **Touch-first** — full-width CTAs at the bottom, large row hit targets
- **Status at a glance** — badges, chips, and count bubbles communicate state without reading

---

## 2. Color Palette

### 2.1 Backgrounds

| Token | Hex | Usage |
|---|---|---|
| `bg-screen` | `#F2F2F7` | All screen backgrounds (iOS system gray) |
| `bg-card` | `#FFFFFF` | Cards, list rows, modals |
| `bg-input` | `#FFFFFF` | Default input fields |
| `bg-input-filled` | `#EEF2FF` | Date/input when a value has been entered |
| `bg-icon-tile` | `#EEF2FF` | Icon background squares in category lists |
| `bg-empty-state` | `#F2F2F7` | Circle behind empty-state icon |
| `bg-overlay` | `rgba(0, 0, 0, 0.45)` | Modal/sheet backdrop |
| `bg-location-chip` | `#F0F0F5` | Location pill in confirm dialogs |

### 2.2 Brand / Primary

| Token | Hex | Usage |
|---|---|---|
| `brand-primary` | `#6B63D9` | Buttons, active tabs, active filter pills, selected calendar dates, icon color |
| `brand-primary-light` | `#7B74E8` | Hover/pressed state on primary button |
| `brand-hero-start` | `#3B1FA3` | Hero banner gradient start (left) |
| `brand-hero-end` | `#6B4FE8` | Hero banner gradient end (right) |

### 2.3 Text

| Token | Hex | Usage |
|---|---|---|
| `text-primary` | `#1C1C1E` | Screen titles, card titles, body text |
| `text-secondary` | `#6B7280` | Meta labels ("Checklist Type:", "Date") |
| `text-muted` | `#8E8E93` | Placeholder text, inactive tab labels, out-of-month calendar days |
| `text-on-brand` | `#FFFFFF` | Text on purple buttons, hero banner |

### 2.4 Semantic

| Token | Hex | Usage |
|---|---|---|
| `success` | `#34C759` | Completed badge, supervisor role chip, completed checkbox |
| `danger` | `#FF3B30` | Urgent badge, error states |
| `warning` | `#FF9500` | GM role chip, deadline warnings |
| `info` | `#3B82F6` | In-progress indicators |

### 2.5 Borders

| Token | Hex | Usage |
|---|---|---|
| `border-default` | `#E5E7EB` | Card borders, input borders |
| `border-active` | `#6B63D9` | Focused inputs, active tab underline |

---

## 3. Typography

All text uses the **system font stack**: SF Pro (iOS), Roboto (Android), Inter/system-ui (Web).

### 3.1 Type Scale

| Style | Size | Weight | Color | Usage |
|---|---|---|---|---|
| Screen Title | 22px | 700 Bold | `text-primary` | Header bar title (e.g. "Operational Checklist") |
| Section Header | 17px | 700 Bold | `text-primary` | Card titles, accordion headers, modal titles |
| Body | 16px | 400 Regular | `text-primary` | Row values, list item labels |
| Body Emphasis | 16px | 600 SemiBold | `text-primary` | Bold keyword in modal copy (e.g. **"Submit"**) |
| Meta Label | 14px | 400 Regular | `text-secondary` | Left-column labels in data rows ("Checklist Type:") |
| Meta Value | 14px | 400 Regular | `text-primary` | Right-column values |
| Caption | 13px | 400 Regular | `text-muted` | Timestamps, helper text, sub-descriptions |
| Button Text | 17px | 600 SemiBold | `text-on-brand` or `text-primary` | All CTA buttons |
| Tab Label Active | 15px | 600 SemiBold | `brand-primary` | Active tab |
| Tab Label Inactive | 15px | 400 Regular | `text-muted` | Inactive tab |
| Badge / Chip | 12px | 600 SemiBold | varies | Status badges, role chips, filter pills |
| Link / Action | 14px | 500 Medium | `brand-primary` | "+ Add Sub Task", "+2 more" overflow links |

### 3.2 Text Rules

- **Never use center-alignment** on form rows — always left-label, right-value
- Screen titles are left-aligned (not centered)
- Hero banner text is left-aligned within the banner
- Empty state title and subtitle are center-aligned only
- Overflow lists use `+N more` in `brand-primary` color

---

## 4. Spacing System

| Token | Value | Usage |
|---|---|---|
| `space-xs` | 4px | Icon-to-label gaps, badge padding |
| `space-sm` | 8px | Between meta rows within a card |
| `space-md` | 16px | Card internal padding, screen horizontal margin |
| `space-lg` | 20px | Between cards/list items |
| `space-xl` | 24px | Modal internal padding |
| `space-xxl` | 32px | Between major screen sections |

**Screen horizontal margin:** 16px on both sides  
**Card internal padding:** 16px all sides  
**Row height (list items):** minimum 56px for touch targets  
**Bottom CTA padding:** 16px below button, 8px above (above home indicator)

---

## 5. Component Library

### 5.1 Buttons

#### Primary CTA (Full-Width)
```
Background:     #6B63D9
Text:           #FFFFFF, 17px SemiBold
Border Radius:  14px
Height:         52px
Width:          100% (full screen width minus 32px margins)
Icon (optional): "+" prefix for create actions, white
Disabled state: #B0C4DE (muted blue-gray), #FFFFFF text
```

#### Secondary / Outline Button
```
Background:     #FFFFFF
Border:         1px solid #E5E7EB
Text:           #1C1C1E, 17px SemiBold
Border Radius:  14px
Height:         52px
Usage:          "Cancel" in dialogs, paired alongside primary
```

#### View Details (Card-inline)
```
Background:     #F0F0F5
Border:         none
Text:           #1C1C1E, 15px Regular
Border Radius:  10px
Height:         44px
Width:          100% of card width
Usage:          Secondary action within list cards
```

#### Floating Action Button (FAB)
```
Background:     #6B63D9
Shape:          Circle, 56px diameter
Icon:           "+" in white, 24px
Position:       Fixed, bottom-right, 20px from edges
Shadow:         0 4px 12px rgba(107, 99, 217, 0.4)
Usage:          Primary create action on list screens
```

### 5.2 Cards

#### Standard List Card
```
Background:     #FFFFFF
Border Radius:  16px
Border:         1px solid #E5E7EB
Padding:        16px
Margin bottom:  12px
Shadow:         none (border only)
```

#### Data Row (within card)
```
Layout:         flex-row, space-between
Label:          14px, #6B7280, left-aligned
Value:          14px, #1C1C1E, right-aligned
Vertical gap:   10px between rows
Divider:        none (spacing only)
```

#### Hero Banner Card
```
Background:     linear-gradient(135deg, #3B1FA3, #6B4FE8)
Border Radius:  16px
Height:         ~150px
Padding:        20px
Text:           #FFFFFF
Illustration:   Right-aligned decorative image (restaurant/people)
Usage:          Top of checklist category screens, top of dashboard
```

### 5.3 List Row (Category / Navigation Row)

Used in category picker screens ("Operational Checklist" > list of categories).

```
Background:         #FFFFFF
Border Radius:      14px
Height:             60px min
Padding:            14px horizontal, 12px vertical
Layout:             icon tile | label | [count badge] | arrow

Icon Tile:
  Size:             36x36px
  Border Radius:    10px
  Background:       #EEF2FF
  Icon Color:       #6B63D9
  Icon Size:        20px

Label:              16px, #1C1C1E, Regular
Count Badge:        see §5.5
Arrow:              "→" glyph or chevron, #8E8E93
```

### 5.4 Tabs (Segmented / Underline)

Used for "New / Completed", "All / Created by Me", "Once a Month / Once a Week".

```
Style:              Underline (not pill/capsule)
Container:          Full-width, flex-row, bottom border #E5E7EB 1px

Active tab:
  Text:             15px SemiBold, #6B63D9
  Underline:        2px solid #6B63D9, full tab width

Inactive tab:
  Text:             15px Regular, #8E8E93
  Underline:        none

Padding:            12px vertical per tab
Equal width tabs:   flex: 1 each
```

### 5.5 Badges & Chips

#### Count Badge (on nav row)
```
Shape:          Rounded pill, ~28px min-width, 26px height
Background:     none
Border:         1.5px solid #34C759
Text:           13px SemiBold, #34C759
Padding:        4px 8px
```

#### Status Badge (on card)
```
Completed:
  Border: 1px solid #34C759 | Text: #34C759

New:
  Background: #6B63D9 | Text: #FFFFFF (filled, no border)

Urgent:
  Border: 1px solid #FF3B30 | Text: #FF3B30

All badges:
  Border Radius:  20px (pill)
  Font:           12px SemiBold
  Padding:        3px 10px
```

#### Role Chip (on user avatar row)
```
GM / Manager:       Border #FF9500, Text #FF9500
Supervisor:         Border #34C759, Text #34C759
Other roles:        Border #6B63D9, Text #6B63D9
Border Radius:      20px | Font: 12px SemiBold | Padding: 2px 8px
```

#### Filter Pills (horizontal scroll)
```
Active:     Background #6B63D9, Text #FFFFFF
Inactive:   Background none, Text #8E8E93
Border Radius: 20px | Padding: 6px 16px | Font: 14px Medium
```

### 5.6 Input Fields

#### Text Input (standard)
```
Background:     #FFFFFF
Border:         1px solid #E5E7EB
Border Radius:  12px
Padding:        14px horizontal, 13px vertical
Height:         48px
Font:           16px Regular, #1C1C1E
Placeholder:    #8E8E93

Focus state:
  Border:       1.5px solid #6B63D9
```

#### Date Input Field
```
Background:     #FFFFFF (empty) / #EEF2FF (filled)
Border:         1px solid #E5E7EB (empty) / #6B63D9 tint (filled)
Border Radius:  10px
Width:          ~180px (right-aligned in 2-col form layout)
Height:         44px
Trailing icon:  Calendar icon, #8E8E93
Placeholder:    "Completion Date", #8E8E93
Value:          "14 March, 2025", #1C1C1E
```

#### Search Bar
```
Background:     #F0F0F5
Border:         none
Border Radius:  12px
Height:         44px
Leading icon:   Magnifier, #8E8E93
Placeholder:    "Search", #8E8E93
Font:           16px Regular
```

#### Textarea (Notes)
```
Background:     #FFFFFF
Border:         1px solid #E5E7EB
Border Radius:  12px
Padding:        12px
Min height:     100px
Font:           16px Regular
```

### 5.7 Modals / Bottom Sheets

```
Trigger:        Appears from bottom OR centered (confirm dialogs)
Background:     #FFFFFF
Border Radius:  20px top corners (bottom sheet) | 20px all (centered)
Padding:        24px
Max width:      100% screen (bottom sheet) | 85% (centered dialog)

Structure:
  Title:          18px Bold, #1C1C1E
  Body text:      15px Regular, #6B7280
  Bold keyword:   16px SemiBold, #1C1C1E (e.g. "Submit" quoted)
  Location chip:  #F0F0F5 background, map-pin icon, 14px, 12px border-radius
  Notes label:    13px Regular, #6B7280
  Textarea:       see §5.6

  Buttons:        Side-by-side, flex-row, gap 12px
    Cancel:       Outline style (§5.1)
    Confirm:      Primary filled style (§5.1)
```

### 5.8 Calendar Picker (Modal)

```
Container:      White modal, rounded 20px
Header:         Month name (17px Bold, center) with "<" ">" nav arrows
Date input:     Text field + "Today" pill button (outlined)
Day headers:    Mo Tu We Th Fr Sa Su, 13px, #8E8E93
Day cells:      16px Regular, #1C1C1E
Selected day:   Filled circle #6B63D9, white text
Out-of-month:   14px, #C7C7CC
Buttons:        Cancel + Apply, same pattern as modal (§5.7)
```

### 5.9 Accordion Rows (Checklist Builder)

```
Collapsed:
  Background:   #FFFFFF, border 1px #E5E7EB, border-radius 12px
  Title:        16px SemiBold, #1C1C1E
  Trailing:     Chevron-down icon, #8E8E93

Expanded (first section open):
  Sub-items:    14px Regular, #1C1C1E, indented 8px
  Avatar chip:  Circular avatar 24px + name 13px + × button
  Add link:     "+ Add Sub Task", 14px Medium, #6B63D9
  Border-bottom: 1px #E5E7EB separating sub-items
```

### 5.10 Empty State

```
Container:      Full screen, centered vertically
Icon container: Circle, 80px diameter, #F2F2F7 background
Icon:           Document/page icon, 36px, #8E8E93
Title:          18px Bold, #1C1C1E, center, margin-top 20px
Subtitle:       15px Regular, #8E8E93, center, max-width 280px
CTA Button:     Auto-width (not full-width), primary style, margin-top 24px
```

---

## 6. Screen Layout Patterns

### 6.1 Standard Screen Anatomy

```
┌──────────────────────────────────┐
│  Status Bar (system)             │
├──────────────────────────────────┤
│  Navigation Header               │
│  < Back Title        [Action]    │
├──────────────────────────────────┤
│  [Hero Banner — optional]        │
├──────────────────────────────────┤
│  [Tabs — optional]               │
├──────────────────────────────────┤
│  Screen content (scrollable)     │
│  · 16px horizontal padding       │
│  · Cards stacked with 12px gap   │
├──────────────────────────────────┤
│  [Sticky CTA — optional]         │
│  Full-width primary button       │
│  16px padding all sides          │
└──────────────────────────────────┘
```

### 6.2 Navigation Header

```
Background:     #F2F2F7 (matches screen, not a colored bar)
Height:         ~52px
Layout:         flex-row, space-between

Left:           "< Title"
  Back arrow:   SF Symbol chevron.left or "‹", 20px, #1C1C1E
  Title:        22px Bold, #1C1C1E, 8px gap from arrow

Right action (optional, one of):
  Home icon:    Pentagon/house outline, 26px, #1C1C1E
  Text action:  "+ Add Task", 15px Medium, #6B63D9
  No separator or border on header
```

### 6.3 Dashboard / Home Screen

```
Header:
  Left:   Wave emoji + "Hi [Name]," — 24px Bold
  Right:  Settings gear icon

Hero banner (full width, 16px margin, ~150px tall)

Module grid:
  Columns: 2
  Item:    White card, 16px radius, icon top-left, label bottom-left
  Icon:    Colorful illustration (not purple tile — home uses colored icons)
  Label:   16px Regular, #1C1C1E
  Gap:     12px
```

### 6.4 Category List Screen (e.g. Operational Checklist)

```
Header: "< Operational Checklist" + home icon
Optional hero banner
"Choose Category:" label — 14px Regular, #6B7280

List of nav rows (§5.3), full width, 12px gap
```

### 6.5 Checklist / Task List Screen

```
Header: "< [Category Name]" + home icon
Tabs: e.g. "New" | "Completed" or "All" | "Created by Me" (with count badge)

List of task cards, each card:
  Title: 16px Bold
  Meta rows: label left / value right
  Overflow: "+2 more" in brand-primary
  "View Details" button (full width, gray fill)
  Assignee row: avatar + name + role chip
```

### 6.6 Form / Data Entry Screen (e.g. Monthly Cleaning)

```
Header: "< [Form Name]" + home icon
Tabs if multiple sub-sections

2-column form rows:
  Left:   Task/item name, 15px Regular, #1C1C1E, flex: 1
  Right:  Date input field (~180px), or checkmark if complete

Completed row:
  Left:   Green checkmark + bold task name
  Right:  Filled date input with #EEF2FF background

Grouped sections:
  Section header: 16px Bold, #1C1C1E
  Section container: White card, 16px radius, items inside as rows

Sticky bottom:
  "Submit" button — primary style, full width
  Disabled (blue-gray) when not all required fields filled
```

### 6.7 Checklist Builder (Task Assignment)

```
Header: "< Checklist Task" + "+ Add Task" action right
Search bar below header
Accordion list of task categories
  Expanded first category shows sub-tasks with assignee chips
"Continue" primary button sticky at bottom
```

---

## 7. Icon System

### 7.1 Icon Style
- **Style:** Outlined/linear (not filled, not solid)
- **Size:** 20–24px standard, 36px for module tiles
- **Color:** `#6B63D9` on lavender tile background, `#8E8E93` for UI chrome (arrows, calendar, back chevron)
- **Library:** SF Symbols (iOS) / Material Icons (Android) — outlined variants

### 7.2 Category Module Icons (Home Grid)
These use full-color illustration icons (not the purple tile style):
- Repair & Maintenance: Green wrench/tools
- Expense Management: Orange/yellow document
- Accounts Payable: Purple dollar document
- Reporting: Gray bar chart document
- Purchasing: Blue shopping cart
- Scheduling: Red briefcase with clock
- Checklist: Green clipboard with check

### 7.3 Navigation / UI Icons
- Back: `chevron.left` — #1C1C1E
- Home: Pentagon house outline — #1C1C1E
- Settings: Gear — #1C1C1E
- Arrow (row navigate): `→` or `chevron.right` — #8E8E93
- Calendar: Calendar outline — #8E8E93
- Search: Magnifier — #8E8E93
- Add: `+` in button or FAB — #FFFFFF
- Chevron down (accordion): `chevron.down` — #8E8E93
- Map pin (location): Pin outline — #8E8E93
- Checkmark (complete): Filled green circle with white check — #34C759

---

## 8. Motion & Transitions

| Interaction | Duration | Easing |
|---|---|---|
| Screen push/pop | 300ms | ease-in-out |
| Modal slide up | 280ms | ease-out |
| Modal dismiss | 220ms | ease-in |
| Tab switch | 150ms | ease-in-out |
| Accordion expand | 200ms | ease-out |
| Button press scale | 80ms | ease-in |
| Calendar date select | 100ms | ease-in |

Button press: scale to 0.97, opacity to 0.9, then snap back.

---

## 9. States Reference

### 9.1 Button States
| State | Primary | Secondary |
|---|---|---|
| Default | `#6B63D9` fill | White fill, gray border |
| Pressed | `#7B74E8` (lighter) + scale 0.97 | Light gray fill |
| Disabled | `#B0C4DE` fill | Same but opacity 0.5 |
| Loading | Spinner replaces text, same bg | — |

### 9.2 Input States
| State | Border | Background |
|---|---|---|
| Empty | `#E5E7EB` | `#FFFFFF` |
| Focused | `#6B63D9` 1.5px | `#FFFFFF` |
| Filled | `#E5E7EB` or tint | `#EEF2FF` |
| Error | `#FF3B30` 1.5px | `#FFF5F5` |

### 9.3 Task/Checklist Row States
| State | Visual treatment |
|---|---|
| New/Pending | No badge or "New" purple badge |
| Completed | "Completed" green outlined badge |
| Urgent | "Urgent" red outlined badge, possible red accent |
| In Progress | Blue "In Progress" badge |

---

## 10. Do's and Don'ts

### Do
- Use full-width CTAs for primary actions — never center-floating
- Keep the header clean: back arrow + title only (plus max 1 right action)
- Show count/progress as green pill badges on nav rows
- Use the #EEF2FF lavender tint for filled inputs and icon tile backgrounds — it ties everything to the brand color subtly
- Always show a "View Details" secondary action on list cards for navigation
- Use "+N more" links in brand-primary to truncate overflow content
- Use the same modal pattern everywhere (white sheet, location chip, notes, Cancel+Submit)
- Use the hero banner on top-level section screens (not on drill-down detail screens)

### Don't
- Don't use colored header bars — keep the header transparent/matching screen bg
- Don't use the dark theme from the strategy doc in V1 — the current design is light
- Don't use amber/orange as a primary brand color — purple is the brand in V1
- Don't stack two full-width buttons vertically in CTAs — place Cancel + Submit side-by-side
- Don't use centered text for form rows or list items
- Don't add drop shadows to cards — use a 1px `#E5E7EB` border instead
- Don't use pill/capsule tabs — use underline tabs only
- Don't mix icon styles (no filled icons alongside outlined ones)

---

## 11. React Native Implementation Notes

```javascript
// Design tokens (constants/theme.ts)
export const Colors = {
  bgScreen:       '#F2F2F7',
  bgCard:         '#FFFFFF',
  bgIconTile:     '#EEF2FF',
  bgInputFilled:  '#EEF2FF',

  brandPrimary:   '#6B63D9',
  brandLight:     '#7B74E8',
  heroStart:      '#3B1FA3',
  heroEnd:        '#6B4FE8',

  textPrimary:    '#1C1C1E',
  textSecondary:  '#6B7280',
  textMuted:      '#8E8E93',
  textOnBrand:    '#FFFFFF',

  success:        '#34C759',
  danger:         '#FF3B30',
  warning:        '#FF9500',
  border:         '#E5E7EB',
  borderActive:   '#6B63D9',
};

export const Radii = {
  card:     16,
  button:   14,
  input:    12,
  chip:     20,
  iconTile: 10,
  modal:    20,
};

export const Spacing = {
  xs:   4,
  sm:   8,
  md:   16,
  lg:   20,
  xl:   24,
  xxl:  32,
  screenH: 16,  // horizontal screen margin
};

export const Typography = {
  screenTitle:  { fontSize: 22, fontWeight: '700' },
  sectionTitle: { fontSize: 17, fontWeight: '700' },
  body:         { fontSize: 16, fontWeight: '400' },
  metaLabel:    { fontSize: 14, fontWeight: '400' },
  metaValue:    { fontSize: 14, fontWeight: '400' },
  caption:      { fontSize: 13, fontWeight: '400' },
  button:       { fontSize: 17, fontWeight: '600' },
  badge:        { fontSize: 12, fontWeight: '600' },
  link:         { fontSize: 14, fontWeight: '500' },
};
```

---

## 12. Angular Web Adaptation Notes

The mobile screens are the design source of truth. When adapting to Angular 17 web:

- **Sidebar navigation** replaces the back-arrow drill-down
- **Cards become wider** — max-width 480px on desktop, 100% on mobile
- **2-column grid** for dashboard modules (not 2-col on desktop — use 3–4 col)
- **Tab component** maps to Angular Material tabs with custom `brand-primary` underline
- **Bottom CTAs** become right-aligned action bars or sticky footers on forms
- All color tokens, typography, and component styles remain identical
- Use Angular Material `mat-card`, `mat-chip`, `mat-button` with custom theme overriding to match these specs

CSS custom properties to configure in Angular theme:

```scss
:root {
  --color-bg-screen:       #F2F2F7;
  --color-bg-card:         #FFFFFF;
  --color-brand-primary:   #6B63D9;
  --color-brand-light:     #7B74E8;
  --color-text-primary:    #1C1C1E;
  --color-text-secondary:  #6B7280;
  --color-text-muted:      #8E8E93;
  --color-success:         #34C759;
  --color-danger:          #FF3B30;
  --color-warning:         #FF9500;
  --color-border:          #E5E7EB;
  --radius-card:           16px;
  --radius-button:         14px;
  --radius-input:          12px;
}
```
