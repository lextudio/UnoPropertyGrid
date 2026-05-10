# Contributing to UnoPropertyGrid

Repository layout:

- `src/UnoPropertyGrid`: core property grid library
- `src/UnoPropertyGrid.Tests`: unit and integration tests for the component
- `UnoPropertyGrid.slnx`: solution file for building the component and tests

Current status:

- Core library exists in `src/UnoPropertyGrid` and targets `net9.0-desktop`. Windows-specific targeting is provided via `net9.0-windows10.0.19041.0` where applicable.
- Unit tests are present in `src/UnoPropertyGrid.Tests` (run with `dotnet test`).
- Packaging metadata (PackageId, PackageIcon) and packaging scripts are present (`pack.ps1`, `dist.*.bat`).
- Build scripts have been adjusted to target `UnoPropertyGrid.slnx` for Windows builds.
- Documentation (`README.md`, `docs/`) is present but contains leftover references to `UnoEdit` that should be removed.
- CI is not configured for this component in the workspace yet.

Next steps:

1. Remove remaining references to `UnoEdit` from docs and metadata across `external/propertygrid`.
2. Finalize `README.md` with sample usage and API notes for `UnoPropertyGrid`.
3. Verify packaging locally with `pwsh ./pack.ps1` and prepare a publishing workflow for NuGet.
4. Add CI (GitHub Actions / Azure Pipelines) to run `dotnet build` and `dotnet test` on PRs.
5. Prioritize accessibility improvements, dark theme polish, and IME behavior fixes.
