# Session 5 — Row Selection + Description Pane

## Goal

Match VS: a property/event row is **selectable**. Clicking a row highlights the entire row
(VS blue fill, white text) and shows the selected property's name + description in the
bottom description pane.

## Current state

- Rows are display-only; clicking does nothing, nothing is highlighted.
- The description pane exists in the control but was never populated (disabled in session 4).

## Plan

1. **Selection state.** Track the selected property VM and the visuals to toggle (row grid
   background, name TextBlock, and the value editor if it's a plain TextBlock).
2. **Click to select.** Each property/event row's outer border handles `Tapped` → `SelectRow`.
3. **VS blue-fill highlight.** Selected row grid background = accent blue (`#0078D4`); name
   (and plain-text value) foreground = white. Restore the previously selected row first.
4. **Description pane.** On selection, set `DescriptionTitle` = DisplayName and
   `DescriptionText` = Description, and show the pane. Re-enable `ShowDescriptionPane` in the
   sample. Pane brushes already follow the theme (session 4 fix).
5. **Rebuild safety.** Clear the stored selection visuals when rows are rebuilt so we never
   touch detached elements.

## Definition of Done

- [x] Clicking a property row highlights the whole row in VS blue with white text.
- [x] Selecting a row populates and shows the description pane (name + description).
- [x] Selecting another row clears the previous highlight.
- [x] Works in dark and light themes; selection cleared on view rebuild (no stale refs).
- [x] Event rows are selectable too (highlight + description), via the shared `SelectRow`.

## Implementation

- Selection state: `_selectedRowGrid`, `_selectedNameText`, `_selectedValueText`; theme-
  independent `_selectionBrush` (`#0078D4`) and `_selectionForegroundBrush` (white).
- `SelectRow(title, description, row, nameText, valueText)` restores the previous row, applies
  the blue fill + white text to the new one, and calls `UpdateDescription`. Wired from each
  property and event row's outer `Tapped`. A plain-text value editor is recolored white; editors
  with their own background (combo, date/time boxes, volume) are left as-is.
- `ClearRowSelection()` runs at the top of every Build*Rows so detached elements are never
  touched after a rebuild.
- Sample re-enables `ShowDescriptionPane="True"`; pane brushes follow the theme (session 4 fix).

## Screenshots

- `session5-before.png`
- `session5-after.png`
