# Contributing to WindowSill Extensions

Thank you for your interest in contributing to WindowSill Extensions! This document provides guidelines and instructions for contributing.

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR_USERNAME/Extensions.git
   cd Extensions
   ```
3. **Create a branch** for your changes:
   ```bash
   git checkout -b feature/your-feature-name
   ```

## Development Setup

### Prerequisites

- Windows 10/11
- .NET 10.0 SDK
- Visual Studio 2026 or later
- WindowSill application for testing

### Building

Open `.src/Extensions.slnx` in Visual Studio and build the solution.

### Debugging

If WindowSill is correctly installed, then all you need to do is set the extension project of your choice as the startup project and run it (F5). WindowSill will launch with your extension loaded for testing.

More information can be found in the [WindowSill SDK documentation](https://getwindowsill.app/doc/articles/extension-development/getting-started/setup.html).

## Making Changes

### Code Style

- Follow the existing code style and conventions in the codebase
- Use meaningful variable and method names
- Keep methods focused and concise
- Add XML documentation for public APIs

### Commit Messages

Write clear, concise commit messages:
- Use the present tense ("Add feature" not "Added feature")
- Use the imperative mood ("Move cursor to..." not "Moves cursor to...")
- Limit the first line to 72 characters
- Reference issues and pull requests when relevant

Example:
```
Add currency favorites to Exchange converter

- Implement favorite currency selection dialog
- Persist favorites to settings
- Display favorites at top of currency list

Fixes #123
```

### Pull Requests

1. **Update your branch** with the latest changes from `main`:
   ```bash
   git fetch origin
   git rebase origin/main
   ```

2. **Push your branch** to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```

3. **Open a Pull Request** on GitHub with:
   - A clear title describing the change
   - A description explaining what and why
   - Reference to any related issues
   - Screenshots for UI changes

4. **Address review feedback** promptly

## Creating a New Extension

If you're creating a new extension:

1. Create a new project under `src/` following the naming convention `WindowSill.YourExtensionName`
2. Reference the WindowSill SDK
3. Implement the required interfaces (`ISill`, etc.)
4. Add unit tests under `src/UnitTests/`
5. Update the README.md to include your extension in the table

### Extension Structure

```
src/WindowSill.YourExtension/
??? YourExtensionSill.cs       # Main extension entry point
??? Settings/
?   ??? Settings.cs            # Settings model
?   ??? SettingsView.cs        # Settings UI
?   ??? SettingsViewModel.cs   # Settings logic
??? UI/
?   ??? ...                    # UI components
??? Core/
    ??? ...                    # Business logic
```

## Reporting Bugs

When reporting bugs, please include:

- A clear, descriptive title
- Steps to reproduce the issue
- Expected behavior
- Actual behavior
- Screenshots if applicable
- Your environment (Windows version, .NET version, WindowSill version)

## Suggesting Features

Feature suggestions are welcome! Please:

- Check existing issues first to avoid duplicates
- Clearly describe the feature and its use case
- Explain why this would be valuable to other users

## Questions?

If you have questions, feel free to:
- Open a discussion on GitHub
- Check existing issues for similar questions

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
