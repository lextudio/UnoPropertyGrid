# UnoPropertyGrid Dark Theme Research Notes

## Goal
- Make the `UnoPropertyGrid.Sample` dark theme closely match the Visual Studio Dark Theme reference in /Users/lextm/uno-tools/UnoPropertyGrid/events.png and /Users/lextm/uno-tools/UnoPropertyGrid/properties.png
- Use DevFlow runtime inspection to verify that style changes take effect.
- Record the exact research steps and methods for later reuse.

## Research Steps

1. Locate the sample project and theme resources
   - Open `UnoPropertyGrid/src/UnoPropertyGrid.Sample`.
   - Confirm where `App.xaml.cs` configures the DevFlow agent.
   - Identify `UnoPropertyGrid/src/UnoPropertyGrid/PropertyGridThemeResources.xaml` and `UnoPropertyGrid/src/UnoPropertyGrid/PropertyGridControl.xaml`.

2. Fix DevFlow port issues and lock the port
   - The default DevFlow port `9223` was occupied.
   - Set `UnoAgentService` in `UnoPropertyGrid.Sample/App.xaml.cs` to use `new AgentOptions { Port = 5500 }`.
   - Use `/opt/homebrew/bin/dotnet` consistently for all runs.

3. Review theme resources and template bindings
   - `PropertyGridThemeResources.xaml` contains base brushes and `ResourceDictionary.ThemeDictionaries` for `Light`, `Dark`, and `Default` themes.
   - `PropertyGridControl.xaml` binds UI elements to these brushes with `ThemeResource`.
   - Pay special attention to `PropertyGridBackgroundBrush`, `PropertyGridPanelBrush`, `PropertyGridCellBrush`, `PropertyGridBorderBrush`, `PropertyGridForegroundBrush`, `TextControl*`, and `ComboBox*` resources.

4. Search for hard-coded colors and remove them
   - Confirm `PropertyGridControl.xaml` has no explicit `#xxxxxx` color literals.
   - Ensure all controls use replaceable `ThemeResource` values so dark theme and runtime theme stay in sync.

5. Update the dark theme palette
   - Adjust the dark theme base colors to match Visual Studio Dark’s deep gray / charcoal tone.
   - Use a softer light-gray foreground, a medium gray muted foreground, and a VS blue accent `#3794FF`.
   - Update text box, combo box, and checkbox border/background colors to be saturated dark tones with better contrast.

6. Sync runtime theme application in code
   - Inspect `PropertyGridControl.xaml.cs` in `ApplyThemeBrushes()`.
   - Ensure when `PropertyGridTheme` is `Dark`, runtime code applies the same dark palette as the resource dictionary.
   - Update the static `SolidColorBrush` values to match XAML resource styles.

7. Run and verify
   - Use `/opt/homebrew/bin/dotnet restore` and `/opt/homebrew/bin/dotnet build -f net10.0-desktop` to validate compilation.
   - If `GitVersion.MsBuild` shell task errors appear, temporarily skip it with `-p:DisableGitVersionTask=true`.
   - Use DevFlow on port `5500` to inspect the live visual tree, screenshot, and effective theme state.

## Reference Visual Studio Screenshot
The standard reference is the attached Visual Studio Dark Theme Properties panel screenshot.

### Exact details to follow
- The panel background should be a very dark gray, not pure black, with a slightly lighter content area.
- The header bar is a strong dark blue accent with white text/icons.
- Property row backgrounds are dark gray and consistent, with subtle separators.
- Labels use high-contrast light gray text.
- Field values use a bright gray that is easy to read but not harsh white.
- Secondary or less important text is medium gray to establish visual hierarchy.
- Active or selected fields should use a darker inner background with a blue highlight for focus.
- Borders and separators should be subtle mid-gray strokes, not sharp black or white lines.
- Font should be Segoe UI 12px, with regular text for fields and semi-bold for section headings.

This screenshot is the design standard for the `UnoPropertyGrid` dark theme.

## Core Methods

- `list_dir` / `grep_search`: quickly locate project structure and key files.
- `read_file`: inspect XAML/C# content to identify the changes needed.
- `multi_replace_string_in_file`: apply palette updates consistently across files.
- `dotnet build`: verify changes with the Homebrew `dotnet` executable.
- DevFlow runtime inspection: use `UnoAgentService` on port `5500` to confirm live UI state.

## Results and Conclusions
- `PropertyGridThemeResources.xaml` is the core entry point for dark theme colors.
- `PropertyGridControl.xaml` templates must fully rely on `ThemeResource`, otherwise theme switching is unstable.
- `PropertyGridControl.xaml.cs`'s `ApplyThemeBrushes()` must keep runtime colors consistent with the resource dictionary, especially text box, combo box, header, and border colors.
- For future optimization, prioritize `PropertyGridBackgroundBrush`, `PropertyGridCellBrush`, `PropertyGridBorderBrush`, `TextControl*`, and `ComboBox*` resources.
