# Session 2 — Icon and Button Foreground Fixes

## Goal

Fix remaining visual issues identified after session 1: icons and text appearing too dark
to be legible on the dark background.

## Issues Identified

### Issue 1 — Picker icon buttons invisible in dark mode

**Observed:** The calendar (Date), clock (Time), and location-pin (City) picker buttons
created by `EditorChrome.CreatePickerButton` rendered their `FontIcon` as nearly invisible
dark glyphs on the dark cell background.

**Root cause:** `FontIcon.Foreground` is set by FontIcon's **default style** (not inherited)
to `{ThemeResource SystemControlForegroundBaseHighBrush}`. On Uno macOS, this ThemeResource
resolves at the **application level** (Light theme), returning a dark color (`#000000` /
`#1E1E1E`). Even though the PropertyGrid's `RootControl.RequestedTheme = Dark`, the platform
resource for `SystemControlForegroundBaseHighBrush` is not scoped to the PropertyGrid.

**Fix:** Added an implicit `Style` for `FontIcon` in `PropertyGridControl.xaml`'s
`UserControl.Resources`:

```xml
<Style TargetType="FontIcon">
    <Setter Property="Foreground" Value="{ThemeResource PropertyGridForegroundBrush}" />
</Style>
```

`PropertyGridForegroundBrush` is defined in `PropertyGridThemeResources.xaml` with
ThemeDictionaries, and **does** resolve correctly from the PropertyGrid scope (confirmed in
session 1). The implicit style applies to all `FontIcon` instances inside the PropertyGrid
that have no explicitly-set `Foreground`, so named icons like `ObjectGlyph`, `SearchGlyph`,
and the category-template `FontIcon` (which already have explicit `Foreground` bindings)
are unaffected.

### Issue 2 — Button/ToggleButton platform foreground resolves from wrong theme

**Observed:** `ButtonForeground` and `ToggleButtonForegroundChecked` ThemeResources resolve
at app level (Light theme), potentially making button content dark on the dark background.

**Root cause:** Same Uno scoping limitation: platform ThemeResources for Button/ToggleButton
resolve from the application's theme, not the nearest ancestor with `RequestedTheme = Dark`.

**Fix:** Added `ButtonForeground`, `ToggleButtonForeground`, `ToggleButtonForegroundChecked`,
`ToggleButtonBackgroundChecked`, and related resources to all three ThemeDictionaries
(Light, Dark, Default) in `PropertyGridThemeResources.xaml`:

| Resource | Dark/Default | Light |
| --- | --- | --- |
| `ButtonForeground` | `#D4D4D4` | `#1E1E1E` |
| `ButtonBackground` | `Transparent` | `Transparent` |
| `ButtonBackgroundPointerOver` | `#2D2D30` | `#E8E8E8` |
| `ButtonBorderBrush` | `Transparent` | `Transparent` |
| `ToggleButtonForeground` | `#D4D4D4` | `#1E1E1E` |
| `ToggleButtonForegroundChecked` | `#D4D4D4` | `#1E1E1E` |
| `ToggleButtonBackground` | `Transparent` | `Transparent` |
| `ToggleButtonBackgroundChecked` | `#094771` | `#E1EEF9` |
| `ToggleButtonBackgroundPointerOver` | `#2D2D30` | `#E8E8E8` |
| `ToggleButtonBorderBrush` | `Transparent` | `Transparent` |
| `ToggleButtonBorderBrushChecked` | `Transparent` | `Transparent` |

These are added to the PropertyGrid's `UserControl.Resources` scope via
`PropertyGridThemeResources.xaml`, so they shadow the platform defaults for all buttons
and toggle buttons inside the PropertyGrid.

## Issue 3 — PropertiesButton checked state shows wrong background (post-session fix)

**Observed:** After adding `ToggleButtonBackgroundChecked` / `ToggleButtonForegroundChecked`
resources, the PropertiesButton (wrench, checked by default) showed a `#E1EEF9` background
with `#1E1E1E` foreground on its ContentPresenter — the Light theme values were being applied
even in dark mode.

**Root cause:** The ToggleButton's internal visual state machine resolves `ToggleButtonBackgroundChecked`
and `ToggleButtonForegroundChecked` using the app-level theme (Light), not the PropertyGrid's
`RootControl.RequestedTheme = Dark` scope on Uno macOS. So the Light dictionary values
(`#E1EEF9`, `#1E1E1E`) were applied to the ContentPresenter instead of the Dark ones.

**Fix:** Removed all ToggleButton* resources from `PropertyGridThemeResources.xaml`. The
wrench/bolt FontIcons already have explicit `Foreground="{ThemeResource PropertyGridForegroundBrush}"`
in XAML and are unaffected by the ToggleButton checked visual state.

## Issue 4 — Volume percentage, placeholder texts not updated on theme switch (post-session fix)

**Observed:** After DevFlow switches the app to dark theme, the volume `42%` TextBlock and
`Search properties` / `City` placeholder texts remained dark (Light-theme colors).

**Root causes:**

- `ApplyThemeBrushes()` is only called at startup (Light theme) and on `PropertyGridTheme`
  changes. It was never re-called after the DevFlow-driven app theme change.
- The Volume percentage `TextBlock` was created in `VolumeEditorProvider` without explicit
  `Foreground`, so it used the platform default `SystemControlForegroundBaseHighBrush`
  resolved at app level (dark on Light).
- `TextControlPlaceholderForeground` resource put into `control.Resources` by
  `ApplyTextControlResources` was ignored by the TextBox template's internal
  `{ThemeResource}` lookup (Uno limitation); the `PlaceholderTextContentPresenter`
  TextBlock retained the platform Light default `#9E000000`.

**Fixes applied:**

1. Added `RootControl.ActualThemeChanged += (_, _) => ApplyThemeBrushes()` in
   `PropertyGridControl` constructor so brushes re-apply whenever the app theme changes.
2. Added implicit `<Style TargetType="TextBlock">` in `PropertyGridControl.xaml` with
   `Foreground="{ThemeResource PropertyGridForegroundBrush}"` to fix volume percentage
   and any other code-created TextBlocks.
3. Added `SetPlaceholderForeground` visual tree walk to `ApplyTextControlResources` so
   `PlaceholderTextContentPresenter` is set directly (bypasses ThemeResource scoping).
4. Added `ApplyComboBoxTheme` + `SetPlaceholderForeground` in `CityMapEditorProvider`
   (called from `Loaded` and `ActualThemeChanged`) to fix the City ComboBox's internal
   editable TextBox colors.

## Fixes Applied

| Fix | File | Change |
| --- | --- | --- |
| Implicit FontIcon style | `PropertyGridControl.xaml` | Added `<Style TargetType="FontIcon">` |
| Implicit TextBlock style | `PropertyGridControl.xaml` | Added `<Style TargetType="TextBlock">` |
| Button theme resources | `PropertyGridThemeResources.xaml` | Added Button* resources (Light, Dark, Default) |
| Re-apply on theme change | `PropertyGridControl.xaml.cs` | `RootControl.ActualThemeChanged → ApplyThemeBrushes()` |
| Placeholder foreground walk | `PropertyGridControl.xaml.cs` | `SetPlaceholderForeground` in `ApplyTextControlResources` |
| City ComboBox theming | `CityMapEditorProvider.cs` | `ApplyComboBoxTheme` + `SetPlaceholderForeground` in `Loaded`/`ActualThemeChanged` |

## Screenshots

- `session2-after-fixes.png` — final dark mode after all fixes

## Definition of Done

- [x] Calendar, clock, and location-pin picker icons visible in dark mode
- [x] PropertiesButton/EventsButton wrench/bolt icons visible with correct foreground
- [x] PropertiesButton checked state shows no unwanted highlight background
- [x] Volume percentage `42%` clearly visible in light gray
- [x] Search box placeholder text visible (muted gray, not near-black)
- [x] City combobox editable text colors correct in dark mode
- [x] `ApplyThemeBrushes()` re-triggered on app theme changes via `ActualThemeChanged`
- [x] All explicitly-set `Foreground` bindings in XAML take precedence over implicit styles
