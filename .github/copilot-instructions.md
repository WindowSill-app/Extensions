# GitHub Copilot Instructions for WindowSill Extensions

## Project Overview

- **Framework**: WinUI 3 with Windows App SDK
- **Target**: Windows 10/11 desktop (x64, ARM64)
- **Architecture**: MVVM with MEF (Managed Extensibility Framework)
- **Language**: C# (latest preview via `LangVersion preview`)
- **Runtime**: .NET 10 (`net10.0-windows10.0.22621`)
- **Testing**: xUnit, FluentAssertions
- **Package Management**: Central Package Management (`Directory.Packages.props`)
- **Solution**: `Extensions.slnx`

## Project Structure

```
src/
├── Directory.Build.props          # Shared build properties
├── Directory.Build.targets        # Shared build targets
├── Directory.Packages.props       # Central package versions
├── WindowSill.{ExtensionName}/
│   ├── {ExtensionName}Sill.cs     # Entry point (ISill export)
│   ├── Assets/                    # Icons (SVG), images
│   ├── Core/                      # Business logic
│   ├── Views/                     # XAML UI components
│   ├── ViewModels/                # MVVM view models
│   ├── Settings/                  # Extension settings
│   ├── Strings/                   # Localization resources
│   └── WindowSill.{ExtensionName}.csproj
└── UnitTests/                     # Shared test project
```

## Extensions

Current extensions: AppLauncher, ClipboardHistory, DevToys, ImageHelper, MediaControl, PerfCounter, ShortTermReminder, Teams, TextFinder, UnitConverter, URLHelper, WebBrowser.

## Key Patterns

### Entry Point

Every extension has a `{ExtensionName}Sill.cs` class that:

- Exports `ISill` via MEF: `[Export(typeof(ISill))]`
- Uses metadata attributes: `[Name("...")]`, `[Priority(...)]`
- Optionally implements activation interfaces: `ISillListView`, `ISillActivatedByDefault`, `ISillActivatedByTextSelection`, `ISillActivatedByDragAndDrop`
- Uses `[ImportingConstructor]` for dependency injection

### Conventions

- Localized strings: `"/WindowSill.ExtensionName/Category/Key".GetLocalizedString()`
- SVG icons: `new SvgImageSource(new Uri("ms-appx:///WindowSill.ExtensionName/Assets/icon.svg"))`
- Async: prefer `ValueTask`, use `ForgetSafely()` for fire-and-forget
- MVVM: `ObservableObject` base class, `RelayCommand` for commands
- Nullable reference types are enabled globally

### Build

- Never build the full solution; build only the modified `.csproj`
- Use `msbuild -t:build <ProjectPath>` to build a specific project
- Platform is x64 and ARM64
