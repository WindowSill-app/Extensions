# Localization

WinUI 3 / Windows App SDK localization using `.resw` resource files, `x:Uid` XAML binding, and the Windows MRT (Modern Resource Technology) resource system. Covers resource file structure, XAML and code-behind access, date/number/currency formatting with CultureInfo, RTL layout support, and pluralization.

**Version assumptions:** Windows App SDK 1.4+. MRT resource system stable since UWP; carried forward into WinUI 3.

## .resw Resource Files

### Overview

WinUI 3 uses `.resw` files (Windows resource format), **not** `.resx` files. The `.resw` format is XML-based (same schema as `.resx`) but is processed by the Windows MRT (Modern Resource Technology) system at runtime instead of .NET `ResourceManager`. MRT provides automatic culture fallback, qualifier-based resource selection (language, scale, contrast), and integration with `x:Uid` for declarative XAML localization.

**Why not `.resx`?** The `.resx` / `ResourceManager` / `IStringLocalizer` pipeline is designed for ASP.NET Core and general .NET. WinUI 3 does not use `IStringLocalizer` or satellite assemblies. Using `.resx` in a WinUI 3 project will not integrate with `x:Uid` binding or MRT resource lookup.

### Culture Fallback Chain

MRT resolves resources by language qualifier, falling back until a match is found:

```
Strings/sr-Cyrl-RS/Resources.resw -> Strings/sr-Cyrl/Resources.resw -> Strings/sr/Resources.resw -> Strings/en-US/Resources.resw (default)
```

The default language folder contains the neutral/fallback resources. The default language is set in `Package.appxmanifest` or the project file.

### Project Setup

```
Strings/
  en-US/
    Resources.resw          # Default (neutral) culture
  fr-FR/
    Resources.resw          # French
  de-DE/
    Resources.resw          # German
  ja-JP/
    Resources.resw          # Japanese
```

```xml
<!-- MyApp.csproj -->
<PropertyGroup>
  <DefaultLanguage>en-US</DefaultLanguage>
</PropertyGroup>
```

For MSIX-packaged apps, also set the default language in `Package.appxmanifest`:

```xml
<Resources>
  <Resource Language="en-US" />
  <Resource Language="fr-FR" />
  <Resource Language="de-DE" />
  <Resource Language="ja-JP" />
</Resources>
```

### Resource File Structure

```xml
<!-- Strings/en-US/Resources.resw -->
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="AppTitle" xml:space="preserve">
    <value>My Application</value>
    <comment>Application title shown in title bar</comment>
  </data>
  <data name="WelcomeMessage.Text" xml:space="preserve">
    <value>Welcome to the application</value>
    <comment>TextBlock on the home page</comment>
  </data>
  <data name="LoginButton.Content" xml:space="preserve">
    <value>Log In</value>
    <comment>Login button content</comment>
  </data>
  <data name="ItemCount" xml:space="preserve">
    <value>You have {0} item(s)</value>
    <comment>{0} = number of items. Used from code-behind.</comment>
  </data>
</root>
```

### XAML Binding with x:Uid

The `x:Uid` directive maps a control to resource keys by convention. The resource key format is `{x:Uid value}.{Property}`.

```xml
<!-- x:Uid="WelcomeMessage" maps to "WelcomeMessage.Text" in Resources.resw -->
<TextBlock x:Uid="WelcomeMessage" />

<!-- x:Uid="LoginButton" maps to "LoginButton.Content" in Resources.resw -->
<Button x:Uid="LoginButton" />

<!-- Multiple properties: "SearchBox.Header", "SearchBox.PlaceholderText" -->
<TextBox x:Uid="SearchBox" />
```

**Supported property mappings:**

| Resource Key | Maps To |
|---|---|
| `MyText.Text` | `TextBlock.Text` |
| `MyButton.Content` | `Button.Content` |
| `MyInput.Header` | `TextBox.Header` |
| `MyInput.PlaceholderText` | `TextBox.PlaceholderText` |
| `MyImage.[using:Microsoft.UI.Xaml.Automation]AutomationProperties.Name` | Attached property |

### Accessing Resources from Code

```csharp
// Using Windows.ApplicationModel.Resources
var resourceLoader = new Microsoft.Windows.ApplicationModel.Resources.ResourceLoader();
string welcome = resourceLoader.GetString("WelcomeMessage/Text");
string itemCount = string.Format(resourceLoader.GetString("ItemCount"), count);

// For unpackaged apps, use ResourceLoader with explicit resource map
var resourceLoader = new ResourceLoader("Resources");
string title = resourceLoader.GetString("AppTitle");
```

### Unpackaged App Considerations

For unpackaged (non-MSIX) WinUI 3 apps, MRT resource loading requires the Windows App SDK runtime. Ensure the project references `WindowsAppSDK` and that the `.pri` resource index file is generated at build time.

```xml
<!-- Ensure PRI generation for unpackaged apps -->
<PropertyGroup>
  <WindowsPackageType>None</WindowsPackageType>
  <GeneratePriFileForUnpackagedApp>true</GeneratePriFileForUnpackagedApp>
</PropertyGroup>
```


## Date, Number, and Currency Formatting

### CultureInfo

`CultureInfo` is the central class for culture-specific formatting. Two distinct properties control behavior:

- `CultureInfo.CurrentCulture` -- controls **formatting** (dates, numbers, currency)
- `CultureInfo.CurrentUICulture` -- controls **resource lookup** (which language folder MRT selects)

```csharp
var date = DateTime.Now.ToString("D", new CultureInfo("fr-FR"));
// "vendredi 14 février 2026"

var price = 1234.56m.ToString("C", new CultureInfo("de-DE"));
// "1.234,56 €"

var number = 1234567.89.ToString("N2", new CultureInfo("ja-JP"));
// "1,234,567.89"
```

### Format Specifiers

| Specifier | Type | Example (en-US) | Example (de-DE) |
|-----------|------|-----------------|-----------------|
| `"d"` | Short date | 2/14/2026 | 14.02.2026 |
| `"D"` | Long date | Friday, February 14, 2026 | Freitag, 14. Februar 2026 |
| `"C"` | Currency | $1,234.56 | 1.234,56 € |
| `"N2"` | Number | 1,234.57 | 1.234,57 |
| `"P1"` | Percent | 85.5% | 85,5 % |


## RTL Support

### Detecting RTL Cultures

```csharp
bool isRtl = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;
// true for: ar-*, he-*, fa-*, ur-*, etc.
```

### WinUI FlowDirection

WinUI 3 uses the `FlowDirection` property on `FrameworkElement`. Set it at the root level to cascade to all children:

```xml
<Page FlowDirection="RightToLeft">
  <!-- All children inherit RTL layout -->
</Page>
```

```csharp
// Set programmatically based on current culture
rootPage.FlowDirection = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft
    ? FlowDirection.RightToLeft
    : FlowDirection.LeftToRight;
```

You can also set `FlowDirection` via `x:Uid` in `.resw` files:

```xml
<!-- In Strings/ar-SA/Resources.resw -->
<data name="RootPage.FlowDirection" xml:space="preserve">
  <value>RightToLeft</value>
</data>
```


## Pluralization

### The Problem

Simple string interpolation fails for pluralization across languages:

```csharp
// WRONG: English-only, breaks in languages with complex plural rules
$"You have {count} item{(count != 1 ? "s" : "")}"
```

Languages like Arabic have six plural forms (zero, one, two, few, many, other). Polish distinguishes "few" from "many" based on number ranges.

### ICU MessageFormat (MessageFormat.NET)

CLDR-compliant pluralization using ICU plural categories. Recommended for internationalization-first projects.

```csharp
// Package: jeffijoe/messageformat.net (v5.0+, ships CLDR pluralizers)
var formatter = new MessageFormatter();

string pattern = "{count, plural, " +
    "=0 {No items}" +
    "one {# item}" +
    "other {# items}}";

formatter.Format(pattern, new { count = 0 });  // "No items"
formatter.Format(pattern, new { count = 1 });  // "1 item"
formatter.Format(pattern, new { count = 42 }); // "42 items"
```

### SmartFormat.NET

Flexible text templating with built-in pluralization. Good for projects wanting maximum flexibility.

```csharp
// Package: axuno/SmartFormat (v3.6.1+)
using SmartFormat;

Smart.Format("{count:plural:No items|# item|# items}",
    new { count = 0 });  // "No items"
Smart.Format("{count:plural:No items|# item|# items}",
    new { count = 1 });  // "1 item"
Smart.Format("{count:plural:No items|# item|# items}",
    new { count = 5 });  // "5 items"
```

### Choosing a Pluralization Engine

| Engine | CLDR Compliance | API Style | Best For |
|--------|-----------------|-----------|----------|
| MessageFormat.NET | Full (CLDR categories) | ICU pattern strings | Multi-locale apps needing standard compliance |
| SmartFormat.NET | Partial (extensible) | .NET format string extension | Flexible templating with pluralization |
| Manual conditional | None | `string.Format` + branching | Simple English-only dual forms |


## Agent Gotchas

1. **Do not use `.resx` files or `IStringLocalizer` in WinUI 3 projects.** WinUI 3 uses `.resw` files with the MRT resource system. `.resx` / `ResourceManager` / `IStringLocalizer` do not integrate with `x:Uid` or the Windows resource pipeline.
2. **Do not forget the `.Property` suffix in `.resw` resource keys.** Keys used with `x:Uid` must include the target property (e.g., `LoginButton.Content`, not `LoginButton`). Keys without a property suffix are only accessible from code via `ResourceLoader.GetString()`.
3. **Do not hardcode plural forms.** English "singular/plural" does not work for Arabic (6 forms), Polish, or other languages. Use MessageFormat.NET or SmartFormat.NET for proper CLDR pluralization.
4. **Do not add translation keys absent from the default language `.resw` file.** The default language resource is the single source of truth; other language folders must be a subset.
5. **Do not forget `<Resource Language>` entries in `Package.appxmanifest`.** Without declaring supported languages, MRT will not load satellite resource files for those cultures.
6. **Do not forget `GeneratePriFileForUnpackagedApp` for unpackaged apps.** Without it, the `.pri` resource index is not generated and `ResourceLoader` calls will fail at runtime.
