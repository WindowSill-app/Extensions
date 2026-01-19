---
description: 'XAML styling and UI design patterns for WinUI 3 applications'
applyTo: '**/*.xaml'
---

# XAML Styling Guidelines

## File Structure

### XAML File Organization

1. XML declaration and root element with namespaces
2. Resources section (if needed)
3. Main content

```xaml
<?xml version="1.0" encoding="utf-8" ?>
<Page
    x:Class="MyApp.Views.MyPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:local="using:MyApp.Views"
    mc:Ignorable="d">

    <Page.Resources>
        <!-- Page-specific resources -->
    </Page.Resources>

    <Grid>
        <!-- Content -->
    </Grid>
</Page>
```

## Namespace Conventions

- Order namespaces: default → x: → custom → d: → mc:
- Use consistent namespace prefixes across the project:
  - `local:` for current namespace
  - `controls:` for CommunityToolkit controls
  - `converters:` for converters
  - `ui:` for custom UI components
  - `vm:` for ViewModels

## Data Binding

### Prefer x:Bind

- Use `x:Bind` for compile-time type checking and better performance
- Always specify binding mode explicitly

```xaml
<!-- Preferred -->
<TextBlock Text="{x:Bind ViewModel.Title, Mode=OneWay}" />

<!-- Avoid unless necessary -->
<TextBlock Text="{Binding Title}" />
```

### Binding Modes

- `Mode=OneTime` - Static values that never change
- `Mode=OneWay` - View reads from ViewModel
- `Mode=TwoWay` - Bidirectional (input controls)

### Null Fallbacks

```xaml
<TextBlock Text="{x:Bind ViewModel.Name, Mode=OneWay, FallbackValue='Unknown'}" />
```

## Resource Definitions

### Converter Declarations

```xaml
<Page.Resources>
    <converters:BoolToVisibilityConverter x:Key="BoolToVisibilityConverter" />
    <converters:BoolToVisibilityConverter
        x:Key="InverseBoolToVisibilityConverter"
        FalseValue="Visible"
        TrueValue="Collapsed" />
</Page.Resources>
```

### Static Resources

- Use `{StaticResource}` for theme resources and styles
- Define app-wide resources in `App.xaml` or theme dictionaries

```xaml
<TextBlock Style="{StaticResource TitleTextBlockStyle}" />
```

## Layout Guidelines

### Grid

- Define rows and columns explicitly
- Use `Auto` for content-sized, `*` for proportional, fixed values sparingly

```xaml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
        <RowDefinition Height="48" />
    </Grid.RowDefinitions>
</Grid>
```

### StackPanel vs Grid

- Use `StackPanel` for simple linear layouts
- Use `Grid` for complex layouts requiring precise positioning
- Avoid deeply nested `StackPanel` elements

### Spacing

- Use `Spacing` property on `StackPanel` instead of individual margins
- Maintain consistent spacing values (8, 12, 16, 24)

```xaml
<StackPanel Spacing="12">
    <TextBlock Text="Item 1" />
    <TextBlock Text="Item 2" />
</StackPanel>
```

## Accessibility

- Always provide `AutomationProperties.Name` for interactive elements
- Use `x:Uid` for localized accessibility text
- Ensure proper tab order with `TabIndex`

```xaml
<Button
    x:Uid="/App/SaveButton"
    AutomationProperties.Name="Save changes"
    Content="Save" />
```

## Fluent Design

### Use WinUI 3 Controls

- Prefer built-in WinUI 3 controls over custom implementations
- Use `InfoBadge`, `InfoBar`, `NavigationView` for modern UI

### Acrylic and Mica

```xaml
<Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
```

### Icons

- Use Segoe Fluent Icons font
- Reference via `FontIcon` with glyph codes

```xaml
<FontIcon FontFamily="{StaticResource SegoeFluentIcons}" Glyph="&#xE896;" />
```

## Menu and Flyout Patterns

```xaml
<MenuFlyout x:Key="ContextMenu">
    <MenuFlyoutItem
        x:Uid="/App/EditMenuItem"
        Click="Edit_Click"
        Icon="Edit" />
    <MenuFlyoutSeparator />
    <MenuFlyoutItem
        x:Uid="/App/DeleteMenuItem"
        Click="Delete_Click"
        Icon="Delete" />
</MenuFlyout>
```

## Animation

- Use CommunityToolkit animations for common scenarios
- Prefer implicit animations over explicit storyboards

```xaml
<animations:Implicit.ShowAnimations>
    <animations:OpacityAnimation Duration="0:0:0.3" To="1" />
</animations:Implicit.ShowAnimations>
```

## Design-Time Data

- Use `d:` prefix for design-time only attributes
- Provide design-time DataContext for XAML preview

```xaml
<Page
    d:DataContext="{d:DesignInstance Type=vm:MyViewModel, IsDesignTimeCreatable=False}">
```

## Performance

- Use `x:Load` for conditional loading
- Avoid complex bindings in frequently updated elements
- Use `ItemsRepeater` with virtualization for large lists

```xaml
<Border x:Load="{x:Bind ViewModel.ShowAdvanced, Mode=OneWay}">
    <!-- Expensive content -->
</Border>
```

## Naming Conventions

- Use descriptive `x:Name` values in PascalCase
- Prefix with element type for clarity: `SaveButton`, `UserNameTextBox`
- Only name elements that need code-behind access
