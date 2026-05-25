# UnoPropertyGrid

UnoPropertyGrid is a desktop-first property grid for [Uno Platform](https://platform.uno) and [WinUI 3](https://learn.microsoft.com/windows/apps/winui/winui3/).

Current scope:

- Target Uno Skia Desktop (WinUI 3 port is included but not the primary focus).
- Do not target mobile during the bootstrap phase (v0.x.x).

## Screenshot & Video

![UnoPropertyGrid on macOS](https://raw.githubusercontent.com/lextudio/UnoPropertyGrid/master/images/macos.png)

<video controls width="720" src="images/property-grid.mp4">
	Your browser does not support the video tag. Download the video: [property-grid.mp4](images/property-grid.mp4)
</video>


## Supported Platforms

- Windows 11 (Windows 10 may work but is not a primary target)
- macOS, 3 most recent versions from 2023-2025
- Ubuntu latest LTS (other Linux distributions may work but are not primary targets)

> If you are looking for support of a specific platform, business sponsorship is the way to accelerate that work. Please reach out to us at [homepage](https://lextudio.com).

## Get Started

Two NuGet packages are available:

- [![NuGet](https://img.shields.io/nuget/v/LeXtudio.UnoPropertyGrid.svg?label=LeXtudio.UnoPropertyGrid)](https://www.nuget.org/packages/LeXtudio.UnoPropertyGrid) The core property grid component.
- [![NuGet](https://img.shields.io/nuget/v/LeXtudio.UnoPropertyGrid.Generator.svg?label=LeXtudio.UnoPropertyGrid.Generator)](https://www.nuget.org/packages/LeXtudio.UnoPropertyGrid.Generator) Optional Roslyn source generator for AOT-safe property discovery (see [AOT support](#aot-support) below).

### Default usage (reflection-based)

```xml
xmlns:pg="using:UnoPropertyGrid"

<pg:PropertyGridControl SelectedObject="{x:Bind MyObject}" />
```

The property grid discovers properties automatically via `TypeDescriptor` and reflection. No extra setup is required for desktop targets.

Study [the sample project](https://github.com/lextudio/UnoPropertyGrid/tree/master/src/UnoPropertyGrid.Sample) to see custom editors and design-time metadata in action.

There are several built-in editors for common types (string, numeric types, bool, enum, etc.) and you can also create custom editors for your own types.

### AOT support

For NativeAOT, trimmed Blazor WebAssembly, or any target where the trimmer removes unreferenced metadata, add the companion source generator:

```
dotnet add package LeXtudio.UnoPropertyGrid
dotnet add package LeXtudio.UnoPropertyGrid.Generator
```

Annotate each type you want to inspect at the assembly level (typically in the same file that declares the type, or in a dedicated `AssemblyInfo.cs`):

```csharp
[assembly: UnoPropertyGrid.GeneratePropertyGridDescriptors(typeof(MyApp.DeviceSettings))]
[assembly: UnoPropertyGrid.GeneratePropertyGridDescriptors(typeof(MyApp.NetworkConfig))]
```

Then swap the default provider before assigning `SelectedObject`:

```csharp
// GeneratedPropertyGridDescriptors is emitted by the source generator at compile time.
PropertyGrid.PropertyProvider = GeneratedPropertyGridDescriptors.CreateProvider();
PropertyGrid.SelectedObject = myDeviceSettings;
```

The generator reads `[Category]`, `[Description]`, `[DisplayName]`, `[ReadOnly]`, and `[Browsable]` attributes at compile time and emits typed lambda accessors — no `PropertyInfo.GetValue` or `TypeDescriptor` is involved in property discovery at runtime.

Study [the AOT sample project](https://github.com/lextudio/UnoPropertyGrid/tree/master/src/UnoPropertyGrid.Sample.Aot) for a complete working example.

## Current Status

Early preview (v0.x.y) releases are available on NuGet.

The API is not yet stable and may change without a major version bump. Feedback is welcome to help shape the future of UnoPropertyGrid. 

## TODO Items Before v1.0.0

- [ ] More built-in editors (currently only a few basic types are supported)
- [ ] Finish dark theme support (currently functional but not fully polished)
- [ ] IME support improvements (currently functional but not fully polished)
- [ ] Accessibility support (screen readers, keyboard navigation, etc.)

## License

UnoPropertyGrid is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## Copyright

Copyright (c) 2026 LeXtudio, Inc. All rights reserved.
