# Session 3 — Sizing, Padding & Alignment to Match VS Property Grid

## Goal

Match the Visual Studio Properties window's compact, uniform layout. Reference:
`UnoPropertyGrid/properties.png` (VS dark theme Properties window).

## Reference observations (VS)

- **Uniform rows.** Every property row is the same compact height (~22px). Editors
  (text, combo, etc.) fit *within* the row — they never make a row taller than its
  neighbours.
- **Category headers** are the same compact height as rows, use **regular** font weight
  (not bold), an expander triangle at the far left, and a slightly distinct background.
  Categories are **contiguous** — no vertical gap between blocks.
- **Name column**: small left indent, vertically centered, regular weight.
- **Value column**: left-aligned content with small left padding, vertically centered.
- **Indicator**: a small (~8px) hollow square at the right edge of each row.
- Font is 12px Open Sans throughout.

## Current state (problems)

| Area | Current | VS target |
| --- | --- | --- |
| Property row height | `MinHeight=24` but editors override | uniform ~22 |
| Date/Time editor box | `MinHeight=30` | 22 |
| City combo | `MinHeight=30` | 22 |
| Volume control | `Height=46` | 46 |
| Brush swatch | `Height=22` | 20 |
| Picker buttons | `30x30` | 22x22 |
| Category header | `MinHeight=24`, weight 600 | 22, weight 400 |
| Name cell padding | `20,2,4,2` | `12,2,4,2` |
| Indicator box | `6x6` | `8x8` |
| Header object glyph | `FontSize=18` | 16 |
| Header toggle buttons | `24x22` | 22x22 |
| Search box | `MinHeight=24` | 22 |

## Plan

1. **Uniform editor heights.** Reduce interactive editors to a shared compact
   height (~22px) so rows stay uniform: Date/Time TextBoxes, City combo, picker
   buttons, brush swatch. Introduce a single `EditorChrome.RowControlHeight` constant
   so future editors stay consistent. **The volume control is intentionally left at its
   original size** (it is a deliberately taller, decorative bar-chart editor).
2. **Property/event rows.** Tighten `CreateRowGrid` to `MinHeight=22`; keep editor
   cell padding minimal; ensure editors vertically center.
3. **Name column rhythm.** Reduce name-cell left indent `20 → 12`.
4. **Category header.** `MinHeight 24 → 22`, font weight `600 → 400`, verify contiguous
   (no gap). Keep distinct background + bottom separator.
5. **Indicator cell.** Grow the hollow square `6 → 8`, keep centered in the 14px column.
6. **Header panel.** Object glyph `18 → 16`, toggle buttons `24x22 → 22x22`, keep the
   Name/Type label rhythm; ensure the Name box matches the new control height.
7. **Search & Arrange-by.** Search box `MinHeight 24 → 22`; keep the arrange-by row
   compact and vertically aligned.
8. **Description pane.** Keep compact padding; verify it doesn't dominate.

## Definition of Done

- [x] All property/event rows render at a uniform ~22px height (no tall outliers).
- [x] Date, Time, City, Brush editors fit within the row height (~21–24px measured).
      Volume intentionally retains its taller decorative bar-chart.
- [x] Category headers are regular weight, compact, and contiguous.
- [x] Name/value content is vertically centered with VS-like horizontal padding.
- [x] Header (glyph, labels, name box, toggle buttons) is compact and aligned.
- [x] Search and Arrange-by rows are compact and aligned.
- [x] Verified against `properties.png` (dark). Sizing is theme-independent so light matches.

## Implementation notes

- **Shared height constant.** `EditorChrome.RowControlHeight = 22` is the single source of
  truth for editor heights (Date/Time/City boxes, picker buttons, brush swatch).
- **lextudio TextBox sizing.** The wrapper now forwards `MinHeight`/`Height` to the inner
  platform TextBox (the inner control otherwise imposes its own ~32px default and ignores a
  smaller wrapper height). Like the box-frame forwarding, it uses a `Loaded` seed guarded by
  `ReadLocalValue` so an explicit-but-default value still propagates. Compact boxes also need
  tight `Padding` (e.g. `4,1`) so inner content doesn't grow past the height.
- **Measured result.** Date 21px, Time 24px, Name 24px, Search 23px — uniform, matching VS.
- **Uniform row spacing.** Symmetric per-row vertical padding makes a box→box gap
  (`bottom + top`) double a box→section gap (one side only). Instead, rows fit their
  content tightly (no internal vertical padding) and a single `RowGap` (6px) is applied
  as each row's bottom margin plus the category row-container's top margin. Result:
  band→box, box→box and box→section gaps are all equal. `RowGap` is `8px`, matching the
  header panel's `RowSpacing` so the spacing is consistent across the whole control.

## Screenshots

- `session3-before.png` — before
- `session3-after.png` — final
