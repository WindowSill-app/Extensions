---
description: 'Guidelines for localizing UI text in XAML views and C# code-behind using api:Uids and .resw resource files'
applyTo: '**/*.xaml, **/*.xaml.cs, **/*.resw'
---

# Localization

## General Instructions

- All user-visible text must be localized using `.resw` resource files
- Never hardcode displayed strings in XAML or code-behind
- Resource files are located at `Strings/en-US/{Category}.resw` within each extension project

## XAML: Use `api:Uids.Uid` (Preferred)

For static text on XAML elements, use the `api:Uids.Uid` attached property instead of code-behind properties with `.GetLocalizedString()`.

### Namespace Declaration

Add the `api` namespace to the root element:

```xml
xmlns:api="using:WindowSill.API"
```

### Uid Format

```
/WindowSill.{ExtensionName}/{ReswFileName}/{KeyName}
```

### XAML Usage

```xml
<!-- TextBlock: sets Text via TextProperty -->
<TextBlock api:Uids.Uid="/WindowSill.MyExtension/Settings/Title" />

<!-- Button: sets Content via ContentProperty -->
<Button api:Uids.Uid="/WindowSill.MyExtension/Settings/SaveButton" />

<!-- ComboBox: sets Header via HeaderProperty -->
<ComboBox api:Uids.Uid="/WindowSill.MyExtension/Settings/Language" />

<!-- MenuFlyoutItem: sets Text via TextProperty -->
<MenuFlyoutItem api:Uids.Uid="/WindowSill.MyExtension/Context/Open" />
```

### Corresponding `.resw` Keys

The `.resw` key name is the last segment of the Uid, suffixed with the dependency property name:

| Element Type | Suffix | Example Key |
| --- | --- | --- |
| `TextBlock` | `.TextProperty` | `Title.TextProperty` |
| `Button` | `.ContentProperty` | `SaveButton.ContentProperty` |
| `ComboBox` / controls with Header | `.HeaderProperty` | `Language.HeaderProperty` |
| `MenuFlyoutItem` | `.TextProperty` | `Open.TextProperty` |
| `SelectorBarItem` | `.TextProperty` | `PresetTab.TextProperty` |
| Tooltip | `.ToolTipService.ToolTipProperty` | `Edit.ToolTipService.ToolTipProperty` |

### `.resw` Entry Example

```xml
<data name="Title.TextProperty" xml:space="preserve">
  <value>Settings</value>
</data>
<data name="SaveButton.ContentProperty" xml:space="preserve">
  <value>Save</value>
</data>
```

## Code-Behind: Use `.GetLocalizedString()`

Use `.GetLocalizedString()` only when the string requires runtime processing such as `string.Format()`, conditional logic, or data binding with dynamic values.

```csharp
// Format string with parameters — must stay in code
SummaryText.Text = string.Format(
    "/WindowSill.MyExtension/Results/Summary".GetLocalizedString(),
    succeeded,
    total);
```

The corresponding `.resw` key does **not** use a property suffix:

```xml
<data name="Summary" xml:space="preserve">
  <value>{0} of {1} files processed successfully</value>
</data>
```

## Key Rules

- `.resw` keys used with `api:Uids.Uid` **must** have a dependency property suffix (e.g., `.TextProperty`)
- `.resw` keys used with `.GetLocalizedString()` must **not** have dots in the key name (dots become PRI subtree separators and break lookup)
- All locale `.resw` files must use identical key names — mismatches cause PRI175 build errors
- When renaming keys, update **all** locale files consistently
