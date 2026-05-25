# Design Tokens: OpsFlow Cross-Platform MVP

## Source

Tokens are derived from the V1 light-purple MealDynamics style guide, adjusted for a dual mobile/Chromebook operating product. The active implementation lives in:

- `opsflow-app/src/theme/colors.js`
- `opsflow-app/src/theme/typography.js`
- `opsflow-app/app.json`

## Philosophy

Light, operational, touch-first, and dense enough for restaurant work without feeling like a spreadsheet. Purple is the brand/action color; neutral iOS-style surfaces carry most of the interface; semantic colors communicate exceptions and completion.

## Light Palette

| Token | Value | Use |
| ----- | ----- | --- |
| background | `#F2F2F7` | Screen background |
| surface | `#FFFFFF` | Cards, rows, panels |
| surfaceElevated | `#F8F8FC` | Inputs, subtle panels |
| surfacePressed | `#EEF2FF` | Selected/pressed surfaces |
| accent | `#6B63D9` | Primary action, active nav, focus |
| accentLight | `#7B74E8` | Hover/pressed primary |
| accentDark | `#5148B8` | Active/deeper brand state |
| text | `#1C1C1E` | Main text |
| textSecondary | `#6B7280` | Labels/meta |
| textMuted | `#8E8E93` | Placeholder/inactive text |
| success | `#34C759` | Completed/signed |
| warning | `#FF9500` | Due soon/caution |
| danger | `#FF3B30` | Overdue/error |
| info | `#3B82F6` | In progress/info |
| border | `#E5E7EB` | Default borders |

## Dark Palette

Dark mode tokens are present for future support, but the MVP defaults to light mode because the selected V1 direction is light-purple.

## Typography

- Screen title: 22px / 700
- Section title: 17px / 700
- Body: 16px / 400
- Meta: 14px / 400-600
- Caption: 13px / 400
- Badge: 12px / 600
- Button: 17px / 600
- Mono: 16px / 600 for cash, inventory, and quantity values

## Spacing And Layout

- Screen margin: 16px
- Card padding: 16px
- Row gap: 8-12px
- Card gap: 12-20px
- Minimum row/touch target: 56px
- Primary action height: 52px
- Chromebook navigation rail: 248px
- Page max width: 1280px

## Shape And Elevation

- Small radius: 8px
- Medium radius: 10px
- Large radius: 14px
- Card radius: 16px
- Shadows are intentionally subtle. Borders should carry most structure.

## Implementation Notes

- Existing screens import `colors`, so `colors` now aliases `lightColors` for compatibility.
- Semantic aliases such as `successLight`, `dangerLight`, `infoLight`, and `surfacePressed` support badges, alert cards, and selected states.
- `app.json` now defaults the app to light mode and uses the light screen background for launch surfaces.
