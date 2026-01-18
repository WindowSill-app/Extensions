# GitHub Copilot Instructions for WindowSill Extensions

## Project Overview

- **Framework**: WinUI 3 with Windows App SDK
- **Target**: Windows 10/11 desktop extensions
- **Architecture**: MVVM with MEF (Managed Extensibility Framework)
- **Language**: C# 14
- **Testing**: xUnit, FluentAssertions, Moq

## Detailed Instructions

Context-specific guidelines are in `.github/instructions/`. These are auto-applied based on file patterns:

| Instruction File | Applies To | Description |
|------------------|------------|-------------|
| `csharp.instructions.md` | `**/*.cs` | C# conventions, formatting, nullable types |
| `winui3.instructions.md` | `**/app/**/*.xaml, **/app/**/*.cs` | MVVM, dependency properties, MEF |
| `xaml-styling.instructions.md` | `**/*.xaml` | XAML layout, binding, accessibility |
| `testing.instructions.md` | `**/*Tests*/**/*.cs` | xUnit, FluentAssertions, Moq patterns |
| `github-actions-ci-cd-best-practices.instructions.md` | `.github/workflows/*.yml` | CI/CD security and optimization |
| `self-explanatory-code-commenting.instructions.md` | `**` | Comment only WHY, not WHAT |

## Project Structure

```
src/
├── WindowSill.{ExtensionName}/
│   ├── Assets/
│   ├── Settings/
│   ├── Strings/
│   ├── ViewModels/
│   ├── {ExtensionName}Sill.cs    # Entry point
│   └── WindowSill.{ExtensionName}.csproj
└── UnitTests/
```

## Quick Reference

### Creating Extensions

1. Create `{ExtensionName}Sill.cs` as entry point
2. Use `[Export]`/`[Import]` for MEF
3. Add localization in `Strings/`
