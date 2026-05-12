# UnoPropertyGrid Design

UnoPropertyGrid should model the Visual Studio Properties window closely enough that a WinUI/Uno control author recognizes the workflow immediately: select a control, switch between properties and events, search, choose categorized or alphabetical arrangement, expand only the categories of interest, and edit values with type-appropriate editors.

The current implementation already has the first layer of this behavior: a selected-object source, categorized/alphabetical sorting, live search, category filtering, and editors for boolean, text, numeric, enum, and read-only values. The next design pass should refine those pieces into a Visual Studio-style property window rather than a generic form.

## Reference Behavior

Primary reference:

- Microsoft Visual Studio Properties window documentation: https://learn.microsoft.com/en-us/visualstudio/ide/properties-window?view=vs-2022

Supporting WinUI PropertyGrid references:

- Telerik WinUI PropertyGrid visual structure: https://www.telerik.com/winui/documentation/controls/radpropertygrid/visual-structure
- Telerik WinUI PropertyGrid filtering: https://www.telerik.com/winui/documentation/controls/radpropertygrid/filtering
- Telerik WinUI PropertyGrid editing: https://www.telerik.com/winui/documentation/controls/radpropertygrid/editing

Visual reference images:

- Properties view: https://user-images.githubusercontent.com/10389314/82217687-b5fded80-9912-11ea-91d9-dd12c9bc0fe7.png
- Events view: https://user-images.githubusercontent.com/10389314/82217690-b7c7b100-9912-11ea-89d6-f304ea2aefc8.png

Important reference points from Visual Studio:

- The window edits design-time properties and events for selected objects.
- Properties and events are separate modes.
- Categorized mode groups properties by category and allows each category to collapse or expand.
- Alphabetical mode shows a flat name-sorted list.
- Search filters properties and events as the user types.
- Rows use different editing fields depending on the property: edit boxes, drop-down lists, and custom editor dialogs.
- Read-only values are visibly disabled or muted.
- A description pane can show the selected property type and short description.

## Product Goals

UnoPropertyGrid is a developer/designer control for inspecting and editing Uno Platform / WinUI object state. It should support common .NET component metadata, while also allowing WinUI/Uno controls to provide richer editors for dependency properties, brushes, colors, templates, styles, layout values, and events.

The project exists because Windows Community Toolkit v8 moved to the `CommunityToolkit/Windows` repository and a single-codebase model for WinUI 2, WinUI 3, and Uno Platform components, while the archived v7-era toolkit repository remains historical guidance rather than a carried-forward generic property grid. UnoPropertyGrid fills that gap for this workspace without tying the implementation to UnoEdit.

Additional goals:

- Provide a reusable WinUI/Uno property grid that can inspect any object, including any WinUI/Uno control instance.
- Keep the component independent from UnoEdit and from temporary workspace-specific property grid projects.
- Use .NET component metadata conventions instead of hard-coded control knowledge.
- Make the reflection/editing core testable without a XAML runtime.
- Support a conservative built-in editor set first, with extension points for richer editors later.

Non-goals for the initial redesign:

- Full Visual Studio designer integration.
- A complete XAML designer event-handler generator.
- A dependency on a commercial control package.
- A WinForms `PropertyGrid` clone with WPF-only editor assumptions.
- A dependency on AvalonEdit or UnoEdit types.
- Control-specific adapters for normal public CLR properties.
- A full nested object graph editor in the first pass.

## Current Project Shape

Current package location:

```text
/Users/lextm/new-AvalonEdit/UnoEdit/external/propertygrid
```

Important files:

- `src/UnoPropertyGrid/UnoPropertyGrid.csproj`: standalone Uno library.
- `src/UnoPropertyGrid/PropertyGridControl.xaml`: public UI surface.
- `src/UnoPropertyGrid/Editors/`: built-in editor providers and shared editor helpers.
- `src/UnoPropertyGrid/DesignTools/Extensibility/Metadata/`: design-time-style metadata registration API.
- `src/UnoPropertyGrid/DesignTools/Extensibility/PropertyEditing/`: custom property editor API.
- `src/UnoPropertyGrid/TypeDescriptorPropertyProvider.cs`: default discovery provider.
- `src/UnoPropertyGrid/PropertyGridPropertyDescriptor.cs`: read/write wrapper over reflected property metadata.
- `src/UnoPropertyGrid/PropertyGridPropertyViewModel.cs`: bindable property row model.
- `src/UnoPropertyGrid.Sample.DesignTools/`: sample design-tools assembly with metadata-registered custom editors.
- `src/UnoPropertyGrid.Tests/UnoPropertyGrid.Tests.csproj`: plain .NET tests for the non-UI core.

Tests should continue to keep the metadata, conversion, and view-model core usable without initializing a Uno UI runtime.

Basic usage:

```xml
xmlns:pg="using:UnoPropertyGrid"

<pg:PropertyGridControl SelectedObject="{x:Bind Editor}" />
```

Imperative usage:

```csharp
PropertyGrid.SelectedObject = myControl;
```

## Layout

The control should use a compact vertical layout:

1. Object selector/header
2. Toolbar
3. Search box
4. Property/event rows
5. Optional description pane

Suggested structure:

```text
+------------------------------------------------+
| Name        <No Name>    [properties] [events]  |
| Type        UserControl                         |
+------------------------------------------------+
|   [categorized] [A-Z] [...]|
+------------------------------------------------+
| search properties/events                        |
+------------------------------------------------+
| > Appearance                                    |
|   Background          [ brush editor        v ] |
|   Foreground          [ brush editor        v ] |
| v Layout                                        |
|   Width               [ number editor         ] |
|   HorizontalAlignment [ enum combo           v ]|
+------------------------------------------------+
| Width : Double                                  |
| Gets or sets the width of the element.          |
+------------------------------------------------+
```

The top toolbar should not look like a large settings form. It should be a dense tool strip using icon buttons or compact toggle buttons with tooltips.

The selected object header is part of the Visual Studio feel and should stay visible in both properties and events mode. It should show at least:

- `Name`: the editable component name, or `<No Name>`.
- `Type`: the selected component type, read-only.

If the selected object has no meaningful name service, the name row can be read-only. When a host supplies a name service, editing the `Name` field should route through that service rather than ordinary reflection.

## Modes

### Properties / Events Switch

Add a first-class `ViewMode`:

```csharp
public enum PropertyGridViewMode
{
    Properties,
    Events
}
```

The toolbar should expose a two-button mode switch:

- Properties: shows editable/read-only properties.
- Events: shows available events and their handler names.

Use familiar mode glyphs where available:

- Wrench/spanner for properties.
- Lightning bolt for events.

For XAML/WinUI use, events should be a separate mode rather than a category mixed into properties. This follows the Visual Studio behavior where properties and events are distinct toolbar views.

Initial event support can be metadata-only:

- Discover public events through reflection.
- Display event name, declaring type, and event handler type.
- Provide a text editor for the handler name.
- Do not generate code-behind until a host application supplies an event-handler service.

Future event integration should be host-provided:

```csharp
public interface IPropertyGridEventService
{
    string? GetHandlerName(object component, EventInfo eventInfo);
    Task SetHandlerNameAsync(object component, EventInfo eventInfo, string? handlerName);
    Task NavigateToHandlerAsync(object component, EventInfo eventInfo);
}
```

### Arrange By Switcher

Replace the current large radio buttons with toolbar-style toggles:

- Categorized
- Alphabetical
- Property source, optional future mode for XAML scenarios

The public API can keep `PropertyGridSortMode` and extend it later:

```csharp
public enum PropertyGridSortMode
{
    Categorized,
    Alphabetical,
    Source
}
```

`Source` should be deferred until dependency property metadata, local values, bindings, styles, inherited values, and default values are modeled.

### Search

Search is required and should be visible by default.

Behavior:

- Filters as the user types.
- Matches `DisplayName`, CLR name, category, description, and for events the handler type/name.
- Search should be case-insensitive and culture-aware.
- In categorized mode, categories with no matching children are hidden.
- Matching child rows should remain inside their original categories.
- When search is active, collapsed categories containing matches should either auto-expand or show a match count.

Public API:

```csharp
public string SearchText { get; set; }
public bool IsSearchBoxVisible { get; set; } = true;
public bool IsDeferredSearchEnabled { get; set; }
```

Live search should be the default. Deferred search can be useful for very large objects and should apply on Enter, Tab, or lost focus.

## Categories

The existing `PropertyGroupHeader` should become an expandable category view model:

```csharp
public sealed class PropertyGridCategoryViewModel
{
    public string Name { get; }
    public bool IsExpanded { get; set; }
    public IReadOnlyList<PropertyGridRowViewModel> Rows { get; }
}
```

Category requirements:

- Categories are sorted alphabetically in categorized mode.
- Category headers include an expander glyph.
- Expand/collapse state is preserved per selected object type and view mode.
- Search should not permanently alter stored expand/collapse state.
- A command should expand or collapse all categories.
- The default category name should be `Misc` when no category metadata exists.

The current category combo box should be removed from the primary UI. Visual Studio does not use a category drop-down for the core workflow; category reduction is handled by expanders and search.

## Rows

Each row should be a two-column grid:

- Name column: property/event display name.
- Value column: editor or read-only display.

The name column should be resizable. The splitter position should be a dependency property so host applications can persist it:

```csharp
public double NameColumnWidth { get; set; } = 180;
```

Rows should support:

- Selected/focused row state.
- Keyboard navigation.
- Read-only visual state.
- Validation error state.
- Tooltips for truncated names and values.
- Description pane updates on selection.

The current inline description under every property should be removed from the main row. It makes the grid less like Visual Studio and reduces scan density. Descriptions belong in the optional description pane.

## Metadata Model

Introduce a common descriptor abstraction that can represent CLR properties, dependency properties, attached properties, and events.

```csharp
public interface IPropertyGridMemberDescriptor
{
    string Name { get; }
    string DisplayName { get; }
    string Category { get; }
    string Description { get; }
    Type ValueType { get; }
    bool IsReadOnly { get; }
    object? GetValue(object component);
    void SetValue(object component, object? value);
}
```

Provider order:

1. Explicit host-provided descriptors.
2. Dependency property / attached property descriptors.
3. `TypeDescriptor` metadata when available.
4. Reflection fallback.

`TypeDescriptorPropertyProvider` currently uses reflection and only checks `BrowsableAttribute`. It should move closer to design-time .NET metadata:

- `BrowsableAttribute`
- `CategoryAttribute`
- `DescriptionAttribute`
- `DisplayNameAttribute`
- `ReadOnlyAttribute`
- `DefaultValueAttribute`
- `EditorAttribute`
- `TypeConverterAttribute`

The intended discovery model is `TypeDescriptor` first, not raw reflection first. `TypeDescriptor.GetProperties(component)` is useful because it participates in .NET component metadata and can surface descriptors that are not plain reflected CLR properties. Reflection remains a fallback for platforms or objects where `TypeDescriptor` metadata is incomplete.

For WinUI/Uno controls, dependency property metadata is important because many useful values are dependency properties rather than ordinary CLR properties. The provider should identify dependency-property-backed CLR wrappers and expose value source metadata later.

## Editor Architecture

The editor system is provider-based. Built-in editors live in `src/UnoPropertyGrid/Editors/`, custom editors use the same provider contract, and metadata can attach a provider to a specific component property.

```csharp
public interface IPropertyGridEditorProvider
{
    bool CanEdit(PropertyGridEditorContext context);
    FrameworkElement CreateEditor(PropertyGridEditorContext context);
}
```

```csharp
public sealed class PropertyGridEditorContext
{
    public required object Component { get; init; }
    public required PropertyGridPropertyDescriptor Descriptor { get; init; }
    public object? Value { get; set; }
    public BindingMode BindingMode { get; init; } = BindingMode.TwoWay;
    public IServiceProvider? Services { get; init; }
    public Action<object?>? SetValue { get; init; }
}
```

Editor resolution order:

1. Metadata-provided editor from `LeXtudio.UnoPropertyGrid.DesignTools.Extensibility.PropertyEditing.EditorAttribute`.
2. Host-registered providers in `PropertyGridControl.EditorProviders`.
3. Built-in editor for known primitive and WinUI types.
4. Type converter text editor.
5. Read-only display.

This follows the same general pattern used by property-grid controls: default editors for common types, plus a way to replace an editor through metadata or direct host registration.

## Built-In Editors

Initial built-in editors:

| Type | Editor |
| --- | --- |
| `bool`, `bool?` | `CheckBox` |
| enum | `ComboBox` |
| flags enum | checked combo/flyout, later |
| `string`, `char` | `TextBox` |
| numeric types | numeric text box, later `NumberBox` where available |
| `DateTime`, `DateTimeOffset`, `TimeSpan` | date/time editor |
| `Color` | color swatch + color picker flyout |
| `SolidColorBrush` | color swatch + color picker, converts to brush |
| `Brush` | brush summary + specialized brush editor or read-only fallback |
| `Thickness` | four-part numeric editor |
| `CornerRadius` | four-part numeric editor |
| `GridLength` | numeric/unit editor |
| `FontFamily` | font family combo |
| `Uri` | text box with validation |
| collections | collection dialog/editor placeholder |
| complex object | expandable nested object or dialog placeholder |

Brush support should be intentionally staged:

- `SolidColorBrush` can be edited safely with a color picker.
- `Brush` may represent image, acrylic, gradient, theme resource, or custom brush. It needs a pluggable editor and should not be flattened to a color unless the runtime value is a `SolidColorBrush`.
- Theme resources and bindings should be preserved where possible instead of overwritten by literal values.

Text support should also respect metadata:

- Single-line text by default.
- Multiline text when metadata opts in.
- Password/secret values should use a host-provided editor rather than a plain text box.

## Custom Editors

Host applications and control libraries must be able to provide editors without forking UnoPropertyGrid.

Public registration examples:

```csharp
propertyGrid.EditorProviders.Add(new BrushEditorProvider());
propertyGrid.EditorProviders.Add(new MyControlSpecificEditorProvider());
```

Template-based customization should also be available:

```csharp
public DataTemplate? EditorTemplate { get; set; }
public DataTemplateSelector? EditorTemplateSelector { get; set; }
```

Use cases:

- A control library provides a custom editor for a specific dependency property.
- A designer host provides a resource picker for `Brush`, `Style`, or `ControlTemplate`.
- A XAML editor host provides an event handler picker.
- A validation-heavy application replaces a numeric editor with a constrained slider/spinner.

## Design-Time Custom Editors

UnoPropertyGrid emulates the Visual Studio design-time metadata pattern for UWP/WPF controls. A control library can keep runtime controls clean and place property-grid metadata in a design-tools assembly or in the sample/application assembly.

The namespaces intentionally mirror the Visual Studio model while staying project-owned:

- `LeXtudio.UnoPropertyGrid.DesignTools.Extensibility.Metadata`
- `LeXtudio.UnoPropertyGrid.DesignTools.Extensibility.PropertyEditing`

Use `PropertyValueEditor` for the common case. It implements `IPropertyGridEditorProvider`, defaults `CanEdit` to writable properties, and leaves editor creation to the derived class.

```csharp
using LeXtudio.UnoPropertyGrid.DesignTools.Extensibility.PropertyEditing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UnoPropertyGrid;

public sealed class RatingValueEditor : PropertyValueEditor
{
    public override bool CanEdit(PropertyGridEditorContext context)
    {
        return context.Descriptor.PropertyType == typeof(double)
            && base.CanEdit(context);
    }

    public override FrameworkElement CreateEditor(PropertyGridEditorContext context)
    {
        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 5,
            Value = context.Value is double value ? value : 0
        };

        slider.ValueChanged += (_, args) => context.SetValue?.Invoke(args.NewValue);
        return slider;
    }
}
```

Register that editor through an attribute table:

```csharp
using System.ComponentModel;
using LeXtudio.UnoPropertyGrid.DesignTools.Extensibility.Metadata;
using LeXtudio.UnoPropertyGrid.DesignTools.Extensibility.PropertyEditing;
using EditorAttribute = LeXtudio.UnoPropertyGrid.DesignTools.Extensibility.PropertyEditing.EditorAttribute;

[assembly: ProvideMetadata(typeof(MyUwpControls.DesignTools.Metadata))]

namespace MyUwpControls.DesignTools;

public sealed class Metadata : IProvideAttributeTable
{
    public AttributeTable AttributeTable
    {
        get
        {
            var builder = new AttributeTableBuilder();

            builder.AddCustomAttributes(
                "MyUwpControls.RatingBox",
                "Value",
                new CategoryAttribute("Common"),
                new DescriptionAttribute("The current rating value."),
                new EditorAttribute(typeof(RatingValueEditor), typeof(IPropertyGridEditorProvider)));

            return builder.CreateTable();
        }
    }
}
```

`AttributeTableBuilder.AddCustomAttributes` also accepts a `Type`, which is useful when the target control type is referenced directly:

```csharp
builder.AddCustomAttributes(
    typeof(RatingBox),
    nameof(RatingBox.Value),
    new EditorAttribute(typeof(RatingValueEditor), typeof(IPropertyGridEditorProvider)));
```

Discovery is automatic for loaded assemblies:

- `AttributeTableStore` scans loaded assemblies for `[assembly: ProvideMetadata(...)]`.
- Each metadata provider returns one `AttributeTable`.
- `PropertyGridPropertyDescriptor.Attributes` merges reflected/`TypeDescriptor` attributes with metadata attributes.
- `PropertyGridControl` reads the project-owned `EditorAttribute`, creates the editor provider, and uses it before host-registered and built-in providers.

`System.ComponentModel.EditorAttribute` is intentionally not part of the editor-resolution path. UnoPropertyGrid uses its own `EditorAttribute` to avoid two competing metadata channels and to keep the editor contract tied to `IPropertyGridEditorProvider`.

Metadata can also provide normal component attributes such as `CategoryAttribute`, `DescriptionAttribute`, `DisplayNameAttribute`, `BrowsableAttribute`, `ReadOnlyAttribute`, and `DefaultValueAttribute`. These attributes participate in property grouping, row text, visibility, read-only state, and default-value comparison.

Editor implementations should commit through `context.SetValue?.Invoke(value)` rather than setting the target object directly. The context path keeps conversion, refresh, and default-value indicators synchronized with the grid.

Use `PropertyGridControl.EditorProviders` for host-level overrides that are broader than one property, such as replacing every `Thickness` editor in an application or registering a resource picker supplied by a designer host. Use design-time metadata for control/property-specific editors that should travel with a control library.

## Value Conversion

Editing should not pass raw strings directly into numeric or complex properties. The current `NumberValue` path sets string values and relies on setter behavior. Replace that with explicit conversion.

Conversion order:

1. Editor emits the correct runtime type.
2. Nullable unwrap/rewrap handling.
3. Enum parse.
4. Known WinUI type parser.
5. `TypeConverter`, when available.
6. `Parse(string)` / `TryParse(string, ...)` methods.
7. Validation error without setting the value.

Failed conversion should keep the typed text visible, show an error state, and avoid mutating the target object.

## Dependency Properties And Value Sources

For WinUI/Uno controls, dependency properties need extra metadata:

- CLR wrapper property, if one exists.
- Dependency property identifier.
- Default value.
- Local value.
- Binding/expression value, where inspectable.
- Style/template/inherited value, where inspectable.
- Attached property owner type.

The first redesign can expose dependency properties as normal rows. A later pass can add a source indicator or `Sort by Source` mode.

## Events

Event mode should match the Visual Studio events screenshot: the same selected-object header remains at the top, the toolbar switches to the lightning-bolt events mode, and the main list becomes event-name rows with handler-name editors.

Suggested events layout:

```text
+------------------------------------------------+
| Name        <No Name>    [properties] [events]  |
| Type        UserControl                         |
+------------------------------------------------+
| ContextMenuClosing       [                    ] |
| ContextMenuOpening       [                    ] |
| DataContextChanged       [                    ] |
| DragEnter                [                    ] |
| DragLeave                [                    ] |
| Drop                     [                    ] |
| GotFocus                 [                    ] |
| Initialized              [                    ] |
+------------------------------------------------+
```

Columns:

- Event name.
- Handler name editor, usually an empty `TextBox` until a handler is assigned.

Event row behavior:

- The default event mode should be flat alphabetical order, matching Visual Studio's event list.
- Categorized event grouping can be added later only if it proves useful; it should not be the default.
- The value column should accept a handler name and validate it using host rules.
- Double-clicking an empty handler cell may ask the host event service to create the default handler.
- Double-clicking a populated handler cell may ask the host event service to navigate to the handler.
- Read-only or unsupported events should keep the event visible but disable the handler editor.
- Long event names should trim with tooltip, as seen in the reference where long names are ellipsized.

Metadata shown in the description pane:

- Event handler type.
- Declaring type.
- Description, if metadata exists.

Event editing requires host services. UnoPropertyGrid should not assume a code-behind model or write files directly.

Search in events mode should filter event names and existing handler names. If the final visual design keeps the search box hidden in events mode to mirror older Visual Studio screenshots, the search API should still work and hosts should be able to keep it visible.

## Public API Sketch

```csharp
public sealed class PropertyGridControl : Control
{
    public object? SelectedObject { get; set; }
    public IReadOnlyList<object>? SelectedObjects { get; set; }
    public PropertyGridViewMode ViewMode { get; set; }
    public PropertyGridSortMode SortMode { get; set; }
    public string SearchText { get; set; }
    public bool ShowReadOnlyProperties { get; set; }
    public bool ShowDescriptionPane { get; set; }
    public double NameColumnWidth { get; set; }

    public IList<IPropertyGridMemberProvider> MemberProviders { get; }
    public IList<IPropertyGridEditorProvider> EditorProviders { get; }

    public IPropertyGridEventService? EventService { get; set; }
}
```

Multi-select should be designed into the model even if it is not implemented immediately. Visual Studio shows common properties across selected objects, which requires merged descriptors and mixed-value display.

## Implementation Plan

Recommended order:

1. Replace `UserControl` layout with a template-friendly `Control` or keep `UserControl` only until the API settles.
2. Add `ViewMode` and split property/event descriptor models.
3. Replace category combo filtering with expandable category groups.
4. Move inline descriptions to a selected-row description pane.
5. Convert sort controls to toolbar-style arrange buttons.
6. Add search API and preserve existing live filtering behavior.
7. Introduce editor provider registry while keeping current primitive editors as built-ins.
8. Add explicit conversion/validation layer.
9. Add built-in WinUI editors for `Color`, `SolidColorBrush`, `Thickness`, `CornerRadius`, and `FontFamily`.
10. Add dependency property metadata provider.
11. Add host-provided event service hooks.
12. Add keyboard navigation and accessibility pass.

## Testing

Unit tests should cover:

- Metadata discovery for attributes and reflection fallback.
- Properties/events mode switching.
- Categorized and alphabetical ordering.
- Search matching and category visibility.
- Category expand/collapse state preservation.
- Editor provider precedence.
- Conversion success and failure.
- Read-only properties.
- Dependency-property-backed controls.

UI tests should cover:

- Toolbar mode switches.
- Search-as-you-type behavior.
- Category expansion and collapse.
- Editor focus and commit/cancel behavior.
- Validation visual state.
- Keyboard navigation.

Current verification commands from the previous implementation notes should be refreshed for the new external location:

```bash
dotnet build UnoEdit/external/propertygrid/src/UnoPropertyGrid/UnoPropertyGrid.csproj
dotnet test UnoEdit/external/propertygrid/src/UnoPropertyGrid.Tests/UnoPropertyGrid.Tests.csproj --verbosity minimal
```

Previous test coverage included:

- metadata discovery through component metadata
- `[Browsable(false)]` filtering
- category/display-name/description metadata
- numeric conversion and writes
- enum values and writes
- read-only setter failure reporting

Known current limitations to track during redesign:

- The current UI is simple and does not virtualize rows yet.
- Setter exceptions are captured as row errors in the view model, but the XAML does not surface error text visually yet.
- Complex types are read-only until custom editor factories and nested expansion are implemented.
- The current category combo box is useful for filtering but should be replaced by Visual Studio-style category expanders.

## Open Questions

- Which Uno targets must support the full editor set: Windows only, WebAssembly, Skia, mobile, or all?
- Should `ColorPicker` be a mandatory dependency or an optional editor provider?
- How much dependency property value-source information can Uno expose consistently across targets?
- Should event handler editing be included in the base package, or only through a designer-host extension?
- Should complex object editing use nested expansion, modal dialogs, or both?
