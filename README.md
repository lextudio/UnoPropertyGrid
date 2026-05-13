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

The main deliverable is a NuGet package:

- [![NuGet](https://img.shields.io/nuget/v/LeXtudio.UnoPropertyGrid.svg?label=LeXtudio.UnoPropertyGrid)](https://www.nuget.org/packages/LeXtudio.UnoPropertyGrid) The core property grid component.

Study [the sample project](https://github.com/lextudio/UnoPropertyGrid/tree/master/src/UnoPropertyGrid.Sample) to see how to use UnoPropertyGrid in your own applications (Uno Platform and WinUI 3).

There are several built-in editors for common types (string, numeric types, bool, enum, etc.) and you can also create custom editors for your own types.

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
