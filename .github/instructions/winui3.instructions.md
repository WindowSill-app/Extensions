---
description: 'Guidelines for WinUI 3 desktop application development with Windows App SDK'
applyTo: '**/app/**/*.xaml, **/app/**/*.xaml.cs, **/app/**/*.cs'
---

# WinUI 3 Development Guidelines

## Project Context

- Framework: WinUI 3 with Windows App SDK
- Target: Windows 10/11 desktop applications
- Architecture: MVVM with MEF (Managed Extensibility Framework) for plugin support
- DI Container: Microsoft.Extensions.DependencyInjection

## MVVM Architecture

### ViewModel Requirements

- Inherit from `ObservableObject` (CommunityToolkit.Mvvm) for property change notifications
- Use `[ObservableProperty]` attribute for bindable properties instead of manual `OnPropertyChanged`
- Use `[RelayCommand]` attribute for commands instead of manual `ICommand` implementations
- Keep ViewModels in dedicated `ViewModels/` folder matching View structure

```csharp
public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _userName = string.Empty;

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        // Implementation
    }
}
```

### View-ViewModel Binding

- Set `DataContext` in XAML or code-behind, never instantiate ViewModels directly in Views
- Use `x:Bind` over `{Binding}` for better performance and compile-time checking
- Specify `Mode=OneWay` or `Mode=TwoWay` explicitly for clarity

```xaml
<TextBox Text="{x:Bind ViewModel.UserName, Mode=TwoWay}" />
```

## Dependency Properties

- Use `DependencyProperty` for custom control properties that need data binding
- Follow the naming convention: `PropertyNameProperty` for the static field
- Include property changed callbacks when side effects are needed

```csharp
public static readonly DependencyProperty ViewModelProperty =
    DependencyProperty.Register(
        nameof(ViewModel),
        typeof(MyViewModel),
        typeof(MyControl),
        new PropertyMetadata(null, OnViewModelChanged));

public MyViewModel? ViewModel
{
    get => (MyViewModel?)GetValue(ViewModelProperty);
    set => SetValue(ViewModelProperty, value);
}
```

## UI Threading

- Use `DispatcherQueue.GetForCurrentThread()` for UI thread operations
- Never perform long-running operations on the UI thread
- Use `async/await` for asynchronous operations

```csharp
await ThreadHelper.UiDispatcher.EnqueueAsync(() =>
{
    // UI update code
});
```

## Resource Management

### Resource Dictionaries

- Place generic styles in `Themes/Generic.xaml`
- Use merged dictionaries for theme-specific resources
- Reference with `ms-appx:///` URI scheme

```xaml
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="ms-appx:///Themes/Generic.xaml" />
</ResourceDictionary.MergedDictionaries>
```

### Localization

- Use resource files (`.resw`) in `Strings/` folder
- Reference with `x:Uid` for XAML elements
- Use custom `Uids.Uid` attached property for complex scenarios

```xaml
<TextBlock x:Uid="/App/WelcomeMessage" />
```

## Window Management

- Use `WinUIEx` library for extended window functionality
- Implement proper window lifecycle handling
- Handle `Closing` event for cleanup

## MEF Plugin System

- Export components using `[Export]` attribute
- Import dependencies using `[Import]` attribute
- Use `ISettingsProvider` for persistent settings
- Use `ILogger` for logging

```csharp
[Export(typeof(IMyService))]
public class MyService : IMyService
{
    [Import]
    private ISettingsProvider _settingsProvider = null!;
}
```

## Error Handling

- Register global exception handlers in `App.xaml.cs`
- Use Sentry for error tracking in production
- Log exceptions before showing user-friendly messages

```csharp
UnhandledException += (sender, e) => HandleException(e.Exception);
TaskScheduler.UnobservedTaskException += (sender, e) => HandleException(e.Exception);
```

## Performance

- Use `x:Load` for deferred loading of UI elements
- Implement virtualization for large lists with `ItemsRepeater`
- Minimize visual tree complexity
- Use `ShouldRender()` pattern to prevent unnecessary renders

## CommunityToolkit Usage

- Use CommunityToolkit.WinUI for converters, behaviors, and controls
- Use CommunityToolkit.Mvvm for MVVM infrastructure
- Prefer toolkit converters over custom implementations

### Common Converters

```xaml
<converters:BoolToVisibilityConverter x:Key="BoolToVisibilityConverter" />
<converters:BoolNegationConverter x:Key="BoolNegationConverter" />
```

## Testing Considerations

- Design ViewModels to be testable without UI
- Use interface abstractions for platform services
- Avoid static dependencies where possible

## Packaged vs Unpackaged

- Support both MSIX (packaged) and standalone (unpackaged) deployments
- Use conditional compilation with `UNPACKAGED` constant
- Handle different storage locations for each mode

```csharp
#if UNPACKAGED
    // Unpackaged-specific code
#else
    // MSIX-specific code
#endif
```
