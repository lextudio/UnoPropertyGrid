# LeXtudio.UnoPropertyGrid.Generator

A Roslyn source generator companion to [LeXtudio.UnoPropertyGrid](https://www.nuget.org/packages/LeXtudio.UnoPropertyGrid) that emits AOT-safe property descriptors at compile time, so the property grid can display and edit object properties without using runtime reflection for property discovery.

## When to use this package

The default `UnoPropertyGrid` relies on `TypeDescriptor` and `PropertyInfo` to discover properties at runtime. That works well on desktop, but it is incompatible with:

- .NET NativeAOT publication
- Blazor WebAssembly trimmed builds
- Any target where unreferenced metadata is removed by the trimmer

Install this generator alongside the main library whenever you target one of those scenarios.

## Installation

```
dotnet add package LeXtudio.UnoPropertyGrid
dotnet add package LeXtudio.UnoPropertyGrid.Generator
```

The generator package ships its DLL under `analyzers/dotnet/cs/` and is automatically wired up by NuGet as a Roslyn analyser — no additional MSBuild configuration is required.

## Usage

### 1. Annotate your assembly

Place one attribute per type you want the generator to cover:

```csharp
[assembly: UnoPropertyGrid.GeneratePropertyGridDescriptors(typeof(MyApp.DeviceSettings))]
[assembly: UnoPropertyGrid.GeneratePropertyGridDescriptors(typeof(MyApp.NetworkConfig))]
```

### 2. Wire up the generated provider

Replace the default reflection-based provider before setting `SelectedObject`:

```csharp
// GeneratedPropertyGridDescriptors is emitted by the source generator.
PropertyGrid.PropertyProvider = GeneratedPropertyGridDescriptors.CreateProvider();
PropertyGrid.SelectedObject = myDeviceSettings;
```

### 3. Done

The generator reads `[Category]`, `[Description]`, `[DisplayName]`, `[ReadOnly]`, and `[Browsable]` attributes at compile time and folds them into typed lambda accessors. `[Browsable(false)]` properties are excluded. Read-only properties (no public setter, or `[ReadOnly(true)]`) are marked accordingly.

## What the generator emits

For each annotated type the generator produces a static registration method. For example, given:

```csharp
public sealed class DeviceSettings
{
    [Category("Network")]
    [Description("The hostname or IP address.")]
    public string HostName { get; set; } = "localhost";

    [Category("Network")]
    public int Port { get; set; } = 443;

    [ReadOnly(true)]
    public string SerialNumber { get; } = "SN-001";

    [Browsable(false)]
    public string InternalToken { get; set; } = "";
}
```

the generator emits (simplified):

```csharp
internal static class GeneratedPropertyGridDescriptors
{
    public static LambdaPropertyProvider CreateProvider() { ... }

    static void RegisterDeviceSettings(LambdaPropertyProvider provider)
    {
        provider.Register<DeviceSettings>(static c => [
            new PropertyGridPropertyDescriptor(c, "HostName", typeof(string),
                getter: static c2 => ((DeviceSettings)c2).HostName,
                setter: static (c2, v) => ((DeviceSettings)c2).HostName = (string)v!,
                category: "Network", description: "The hostname or IP address."),
            new PropertyGridPropertyDescriptor(c, "Port", typeof(int),
                getter: static c2 => (object?)((DeviceSettings)c2).Port,
                setter: static (c2, v) => ((DeviceSettings)c2).Port = (int)v!,
                category: "Network"),
            new PropertyGridPropertyDescriptor(c, "SerialNumber", typeof(string),
                getter: static c2 => ((DeviceSettings)c2).SerialNumber,
                isReadOnly: true),
            // InternalToken omitted — [Browsable(false)]
        ]);
    }
}
```

No reflection is involved in property discovery: the trimmer can see and preserve every accessor.

## Viewing the generated code

Set `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` in your project file to write the generated source to `obj/<config>/<tfm>/generated/UnoPropertyGrid.Generator/`.

## Supported platforms

Same as `LeXtudio.UnoPropertyGrid`: Windows 11, macOS, Ubuntu LTS (Uno Skia Desktop). WinUI 3 is included but not the primary focus.

## License

MIT — see [LICENSE](https://github.com/lextudio/UnoPropertyGrid/blob/master/LICENSE).

## Copyright

Copyright (c) 2026 LeXtudio, Inc. All rights reserved.
