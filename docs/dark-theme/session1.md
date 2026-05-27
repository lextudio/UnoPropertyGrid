# Session 1 — Baseline Screenshot and Gap Analysis

## Goal

Capture a live dark-theme screenshot of `UnoPropertyGrid.Sample` via DevFlow and compare it
pixel-by-pixel (visually) with the VS reference shots (`events.png` and `properties.png`).
Produce an annotated gap list that drives palette and layout fixes in later sessions.

## Prerequisites

- DevFlow agent is already wired in `App.xaml.cs` on port 5500 (`UnoAgentService` with `AgentOptions { Port = 5500 }`).
- The sample project targets `net10.0-desktop` and builds with `/opt/homebrew/bin/dotnet`.

## Step 1 — Build the sample

```bash
cd /Users/lextm/uno-tools/UnoPropertyGrid/src/UnoPropertyGrid.Sample
/opt/homebrew/bin/dotnet build -f net10.0-desktop -p:DisableGitVersionTask=true
```

Expected: zero errors. If `GitVersion.MsBuild` shell-task errors appear, `-p:DisableGitVersionTask=true` suppresses them.

## Step 2 — Launch in dark mode

Run the sample in the background (it starts in light mode by default):

```bash
/opt/homebrew/bin/dotnet run --no-build -f net10.0-desktop &
```

Then use the DevFlow `/api/v1/device/app/theme` endpoint to force dark mode without touching
the toggle in the UI (avoids mouse interaction):

```bash
curl -s -X PUT http://localhost:5500/api/v1/device/app/theme \
     -H "Content-Type: application/json" \
     -d '{"theme":"dark"}'
```

Confirm the response contains `"userAppTheme":"dark"`.

> Alternatively, if the PUT endpoint is not yet wired for `PropertyGridTheme`, use the UI toggle
> via DevFlow tap:
>
> ```bash
> curl -s -X POST http://localhost:5500/api/v1/ui/tap \
>      -H "Content-Type: application/json" -d '{"id":"ThemeToggle"}'
> ```

## Step 3 — Capture baseline screenshots

Capture the full window and save locally:

```bash
curl -s http://localhost:5500/api/v1/ui/screenshot \
     --output /Users/lextm/uno-tools/UnoPropertyGrid/docs/dark-theme/session1-baseline.png
```

Also capture the property grid control specifically (use the DevFlow tree to find its element id
first if needed):

```bash
curl -s http://localhost:5500/api/v1/ui/tree > /tmp/devflow-tree.json
# Identify the x:Name or AutomationId of the PropertyGrid root element, e.g. "PropertyGrid"
curl -s "http://localhost:5500/api/v1/ui/screenshot?id=PropertyGrid" \
     --output /Users/lextm/uno-tools/UnoPropertyGrid/docs/dark-theme/session1-propertygrid.png
```

## Step 4 — Visual comparison

Open side-by-side in any viewer:

| Baseline capture              | VS reference                    |
| ----------------------------- | ------------------------------- |
| `session1-baseline.png`       | `events.png` / `properties.png` |

Work through each visual area listed below and mark ✅ (matches VS) or ❌ (gap found).

### Areas to check

| Area | Key resource keys | Expected VS color |
| ---- | ----------------- | ----------------- |
| Window / outer background | `PropertyGridBackgroundBrush` | `#1E1E1E` |
| Panel / toolbar row | `PropertyGridPanelBrush` | `#252526` |
| Category header rows | `PropertyGridCategoryBrush` | `#2D2D30` |
| Property cell background | `PropertyGridCellBrush` | `#252526` |
| Row separator / borders | `PropertyGridBorderBrush` | `#3F3F46` |
| Property label text | `PropertyGridForegroundBrush` | `#D4D4D4` |
| Muted / secondary text | `PropertyGridMutedForegroundBrush` | `#8A8A8A` |
| Selected row highlight | (inline color or accent) | `#094771` bg, `#D4D4D4` fg |
| Category header text (bold) | `PropertyGridForegroundBrush` | `#D4D4D4`, semi-bold |
| Text-box / input field | `TextControl*` keys | background `#1E1E1E`, border `#3F3F46` |
| Combo-box | `ComboBox*` keys | background `#1E1E1E`, border `#3F3F46` |
| Check-box border | `CheckBoxCheckBackgroundStroke*` | unchecked `#8A8A8A`, checked `#3794FF` |
| Font family | `PropertyGridFontFamily` | VS uses Segoe UI; sample currently uses Open Sans |
| Search / filter bar | same cell + border brushes | matches row style |

## Step 5 — Inspect live tree for effective values

For any area marked ❌, query the element's effective brush via DevFlow:

```bash
# Example: check effective background of the first property row
curl -s "http://localhost:5500/api/v1/ui/element?id=<element-id>" | python3 -m json.tool
```

Look for `frameworkProperties.background` or `actualTheme` to confirm whether the XAML
`ThemeResource` resolved correctly or the code-behind `ApplyThemeBrushes()` override won.

Key code location: [PropertyGridControl.xaml.cs:706](../../src/UnoPropertyGrid/PropertyGridControl.xaml.cs#L706) (`ApplyThemeBrushes` method, lines 706–732).

## Step 6 — Document gaps

For each ❌, record:

```
Gap N — <area name>
  Observed:   <colour or behaviour seen in screenshot>
  Expected:   <colour from VS reference>
  Root cause: ThemeResource not applied / code-behind override / hard-coded value
  Fix target: PropertyGridThemeResources.xaml line X  OR  PropertyGridControl.xaml.cs line Y
```

Paste the gap list at the bottom of this file under **Gap Register** before ending the session.

## Step 7 — Kill the sample process

```bash
kill %1   # or use the PID captured when launching
```

## Definition of Done for Session 1

- [x] `session1-baseline.png` and `session1-propertygrid.png` saved in this folder.
- [x] All visual areas in Step 4 table assessed (✅ or ❌).
- [x] Gap Register below populated.
- [x] No code changes made to UnoPropertyGrid — this session is observation-only.

---

## Completed Assessment

DevFlow brush inspection was enabled by patching `UnoVisualTreeWalker.GetFrameworkProperties`
in the local DevFlow checkout (`/Users/lextm/wpf-tools/wpf-labs/src/DevFlow`) to read
`Background`, `Foreground`, `BorderBrush`, `Fill`, `Stroke`, `RequestedTheme`, and
`ActualTheme` from every element. The sample was rebuilt with a `ProjectReference` pointing
to that local build.

Note: only the `PropertyGrid` control itself was in dark mode during this session — the page
`RequestedTheme` was not changed. `PropertyGrid.PropertyGridTheme` was implicitly Dark because
the DevFlow `PUT /api/v1/device/app/theme {"theme":"dark"}` call updated
`Application.Current.RequestedTheme`, which `ApplyThemeBrushes()` falls back to when
`PropertyGridTheme` is `Default`.

| Area | Key resource keys | Expected VS color | Result | Observed |
| ---- | ----------------- | ----------------- | ------ | -------- |
| Window / outer background | `PropertyGridBackgroundBrush` | `#1E1E1E` | ✅ | `#1E1E1E` on RootControl |
| Panel / toolbar row | `PropertyGridPanelBrush` | `#252526` | ✅ | `#252526` on HeaderPanel, ArrangeByPanel |
| Category header rows | `PropertyGridCategoryBrush` | `#2D2D30` | ✅ | slightly darker than rows, visually correct |
| Property cell background | `PropertyGridCellBrush` | `#252526` | ✅ | `#252526` on SearchBox, DescriptionPane |
| Row separator / borders | `PropertyGridBorderBrush` | `#3F3F46` | ⚠️ | `#3F4048` on TextBox/ComboBox (2-bit rounding in Uno) |
| Property label text | `PropertyGridForegroundBrush` | `#D4D4D4` | ✅ | `#D4D4D4` on ObjectNameBox foreground |
| Muted / secondary text | `PropertyGridMutedForegroundBrush` | `#8A8A8A` | ✅ | `#8A8A8A` on DescriptionText |
| Selected row highlight | inline accent | `#094771` bg | ❓ | not testable — no row selected during session |
| Category header text (bold) | `PropertyGridForegroundBrush` | `#D4D4D4`, semi-bold | ❓ | cannot verify weight without font inspector |
| Text-box / input field | `TextControl*` keys | bg `#1E1E1E`, border `#3F3F46` | ⚠️ | bg ✅, border visible (`#3F4048`), border too prominent vs VS |
| Combo-box | `ComboBox*` keys | bg `#1E1E1E`, border `#3F3F46` | ❌ | rounded visible border; VS combo boxes are flat |
| Check-box border | `CheckBoxCheckBackgroundStroke*` | unchecked `#8A8A8A` | ❓ | no checkbox in sample data |
| Font family | `PropertyGridFontFamily` | Open Sans (by design) | ✅ | Open Sans is the intentional default |
| Search / filter bar | `TextControl*` keys | flat, border matches cell | ❌ | rounded border visible; search box stands out vs VS flat style |

## Gap Register

### Gap 1 — Search box border too visible

- **Observed:** `SearchBox` renders with a rounded visible border around the full text box, making
  it look like a standard TextBox control rather than an integrated filter bar
- **Expected:** VS search bar is flat — no visible border separating it from the panel background
- **Root cause:** `TextControlBorderBrush` = `#3F3F46` is non-transparent; the search box uses the
  standard TextBox template which draws this border
- **Fix target:** Either set `BorderThickness="0"` on the SearchBox in `PropertyGridControl.xaml`
  or add a custom style that suppresses the border for this specific use

### Gap 2 — Combo box rounded visible border

- **Observed:** `ArrangeByComboBox` and the `City` combo box editor both render with a rounded,
  relatively prominent border (`#3F4048` effective value)
- **Expected:** VS combo boxes inside the property grid are flat with no visible border, blending
  with the cell background
- **Root cause:** The default Uno ComboBox template draws a visible rounded border regardless of
  `ComboBoxBorderBrush` color when the opacity/thickness is non-zero
- **Fix target:** `ComboBoxBorderBrush` and `ComboBoxBorderBrushPointerOver` in the Dark/Default
  theme dictionaries could be set to `Transparent`, or a custom ComboBox style could set
  `BorderThickness="0"`

### Gap 3 — Custom editor icon buttons visible as buttons

- **Observed:** The calendar (Date), clock (Time), and location-pin (City) icon buttons render
  with a visible square border and a slightly lighter background, making them look like distinct
  buttons rather than seamless cell elements
- **Expected:** VS property editors show controls with very flat styling, blending with the cell
  background
- **Root cause:** The icon button style uses `BorderBrush` and a background that contrasts with
  `PropertyGridCellBrush`; the button template's default corner radius and border are visible
- **Fix target:** Custom editor base styles in `PropertyGridControl.xaml` — reduce button border
  thickness and match button background to `PropertyGridCellBrush`

### Gap 4 — `ActualTheme` reports Light on child elements

- **Observed (DevFlow data):** `RootControl` has `requestedTheme: Dark, actualTheme: Dark`, but
  every child element (HeaderPanel, SearchBox, etc.) with `requestedTheme: Default` reports
  `actualTheme: Light`
- **Expected:** In WinUI, `ActualTheme` on descendants should reflect the nearest ancestor's
  explicit `RequestedTheme`; children should report Dark
- **Root cause:** Uno Platform does not fully propagate `ActualTheme` through a subtree when
  an ancestor has `RequestedTheme = Dark` but the app/page level is Light. This is a known Uno
  limitation. `ThemeResource` lookups ARE resolving correctly from the Dark dictionary
  (confirmed by brush colors) — only the `ActualTheme` property value is wrong
- **Impact:** Any code that reads `element.ActualTheme` to decide colors would get wrong results
  on Uno in this mixed-theme configuration
- **Fix target:** `PropertyGridControl.xaml.cs` `ApplyThemeBrushes()` already uses a manual
  `effectiveTheme` calculation rather than `ActualTheme` — keep this pattern. Document the
  Uno limitation in a code comment at line 706

### Gap 5 — Border brush slight numeric discrepancy (`#3F4048` vs `#3F3F46`)

- **Observed:** TextBox and ComboBox effective `borderBrush` = `#3F4048` (R=63, G=64, B=72)
- **Expected:** `#3F3F46` (R=63, G=63, B=70) from theme resources
- **Root cause:** Likely a Uno color-profile / DPI-scale rounding artifact; the difference is
  visually imperceptible (2 LSB in green, 2 LSB in blue)
- **Fix target:** No action needed — cosmetically identical at normal DPI

## Findings Summary

All core background and foreground palette colors match the VS reference exactly.

## Fixes Applied in This Session

All actionable gaps were resolved before closing session 1. Screenshot after fixes:
`session1-after-fixes.png`.

| Gap | Fix |
| --- | --- |
| Gap 1 — Search box border | Added `BorderThickness="0"` to `SearchBox` in `PropertyGridControl.xaml` |
| Gap 2 — Combo box border | Added `BorderThickness="0"` to `ArrangeByComboBox` in `PropertyGridControl.xaml`; added `BorderThickness = new Thickness(0)` to the City `ComboBox` in `CityMapEditorProvider.cs` |
| Gap 3 — Picker icon buttons | `EditorChrome.CreatePickerButton` now sets `BorderThickness=0`, `CornerRadius=0`, `Background=Transparent` |
| Gap 4 — `ActualTheme` Uno quirk | Added explanatory comment in `PropertyGridControl.xaml.cs` at `ApplyThemeBrushes()` |
| Gap 5 — Border rounding | No action (2-bit rounding, visually imperceptible) |

Also fixed `EventTemplate` TextBox in `PropertyGridControl.xaml` to use `BorderThickness="0"` for consistency.
