# Session 4 — Apply the Property-View Layout Pattern to the Event View

## Goal

Session 3 made the **property** view match the VS Properties window (uniform compact rows,
consistent `RowGap`, framed square editors, right-edge alignment). The **event** view was
left untouched and still looks inconsistent. Session 4 brings the event view in line.

## Current state (problems)

Reference: `session4-before.png`.

- **No row spacing.** Event rows sit flush under the search box and against each other —
  the uniform `RowGap` (8px) used in the property view is not applied.
- **No right-edge alignment.** The handler TextBox runs all the way to the right border,
  whereas property editors stop short, leaving the 20px indicator column. The event value
  box should end at the same x as property editors.
- **Editor not framed like property editors.** The handler TextBox doesn't use the VS box
  pattern (square corners, 1px light border, compact 22px height, tight padding) and isn't
  wrapped in the standard editor cell padding (`4,0,4,0`).
- **Background.** Uses the window background instead of the cell brush used by the framed
  editors elsewhere.

## Plan

1. **Row structure parity.** Build event rows through the same 3-column grid as property
   rows (name / value* / 20px trailing column) so the value box right edge aligns with
   property editors. Wrap the handler TextBox in the standard editor cell (`CreateCellBorder`
   with `4,0,4,0` padding) instead of placing it directly in the value column.
2. **Framed editor.** Apply the VS box pattern to the handler TextBox: `CornerRadius=0`,
   `BorderThickness=1`, light `BorderBrush`, cell-brush background, `MinHeight=22` and tight
   `Padding (4,1)` so it renders at the uniform row height.
3. **Uniform RowGap.** Give each event row the same bottom-margin `RowGap`, plus a matching
   top gap on `EventRowsPanel`, so band/section→box, box→box and box→edge gaps all equal the
   property view's 8px.
4. **Vertical centering.** Ensure the name text and handler box vertically center within the
   row, matching property rows.

## Definition of Done

- [x] Event rows use uniform 8px `RowGap` spacing (top via `EventRowsPanel` margin, between
      rows via each row's bottom margin).
- [x] Handler TextBox is framed (square corners, 1px light border, cell-brush background) and
      compact (~23px measured).
- [x] Handler TextBox right edge aligns with property editors — event rows now use the same
      3-column grid (name / value* / 20px) and `4,0,4,0` editor cell, so alignment is structural.
- [x] Event name + value vertically centered, matching property rows.
- [x] Verified in dark; sizing/layout is theme-independent so light matches.

## Implementation

`CreateEventRow` now mirrors `CreatePropertyRow`:

- Uses `CreateRowGrid()` (3 columns incl. the 20px trailing column; left empty since events
  have no override indicator) instead of the 2-column variant.
- Wraps the handler TextBox in `CreateCellBorder(1)` with `4,0,4,0` padding.
- Handler TextBox: `CornerRadius=0`, `BorderThickness=1`, cell-brush background, `MinHeight=22`,
  `Padding(4,1)`.
- Outer row gets `Margin(0,0,0,RowGap)`; `EventRowsPanel` gets `Margin(0,8,0,0)` for the top gap.

## Screenshots

- `session4-before.png` — event view before
- `session4-after.png` — event view after
