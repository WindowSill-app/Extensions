# Accessibility

WinUI 3 / Windows App SDK accessibility patterns: AutomationProperties, keyboard navigation, focus management, color contrast, high contrast themes, custom AutomationPeer implementation, and screen reader integration via Microsoft UI Automation.

## Core Principles

### Semantic Markup

Provide meaningful names and descriptions for all interactive and informational elements. Screen readers rely on semantic metadata -- not visual appearance -- to convey UI structure.

- Every interactive control must have an accessible name (`AutomationProperties.Name`, visible text content, or `AutomationProperties.LabeledBy`)
- Images and icons must have text alternatives describing their purpose
- Decorative elements should be hidden from the accessibility tree (`AutomationProperties.AccessibilityView="Raw"`)
- Group related controls logically so screen readers announce them in context

### Keyboard Navigation

All functionality must be operable via keyboard alone. Users who cannot use a mouse, pointer, or touch depend entirely on keyboard interaction.

- Maintain a logical tab order that follows the visual reading flow
- Provide visible focus indicators on all interactive elements
- Support standard keyboard patterns: Tab/Shift+Tab for navigation, Enter/Space for activation, Escape to dismiss, arrow keys within composite controls
- Avoid keyboard traps -- users must be able to navigate away from every control

### Focus Management

Programmatic focus management ensures screen readers announce context changes correctly.

- Move focus to newly revealed content (dialogs, expanded panels, inline notifications)
- Return focus to the triggering element when dismissing overlays
- Avoid stealing focus unexpectedly during background updates
- Set initial focus on the primary action when a page or dialog loads

### Color Contrast

Ensure text and interactive elements meet WCAG contrast ratios.

| Element Type | Minimum Ratio (WCAG AA) | Enhanced Ratio (WCAG AAA) |
|---|---|---|
| Normal text (< 18pt) | 4.5:1 | 7:1 |
| Large text (>= 18pt or 14pt bold) | 3:1 | 4.5:1 |
| UI components and graphical objects | 3:1 | 3:1 |

- Do not rely on color alone to convey information (use icons, patterns, or text labels as supplements)
- Support high-contrast themes and system color overrides
- Test with color blindness simulation tools


## AutomationProperties

WinUI 3 builds on the Microsoft UI Automation framework. Built-in controls include automation support by default. Use `AutomationProperties` attached properties to provide accessibility metadata.

```xml
<!-- Name: primary accessible name for screen readers -->
<Image Source="ms-appx:///Assets/product.png"
       AutomationProperties.Name="Product photo showing a blue widget" />

<!-- HelpText: supplementary description -->
<Button Content="Add to Cart"
        AutomationProperties.HelpText="Adds the current product to your shopping cart" />

<!-- LabeledBy: associates a label with a control -->
<TextBlock x:Name="QuantityLabel" Text="Quantity:" />
<NumberBox AutomationProperties.LabeledBy="{x:Bind QuantityLabel}"
           Value="{x:Bind ViewModel.Quantity, Mode=TwoWay}" />

<!-- Hide decorative elements from accessibility tree -->
<Image Source="ms-appx:///Assets/divider.png"
       AutomationProperties.AccessibilityView="Raw" />

<!-- Headings for navigation structure -->
<TextBlock Text="Order Summary"
           AutomationProperties.HeadingLevel="Level1" />
<TextBlock Text="Items"
           AutomationProperties.HeadingLevel="Level2" />
```

### AutomationProperties Reference

| Property | Purpose | When to Use |
|---|---|---|
| `Name` | Primary accessible name | Icon buttons, images, controls without visible text |
| `HelpText` | Supplementary description | Additional context about a control's purpose or behavior |
| `LabeledBy` | Associates a label element | Input controls with separate label TextBlocks |
| `AccessibilityView` | Controls tree inclusion | `Raw` to hide decorative elements; `Content` (default) for meaningful elements |
| `HeadingLevel` | Heading-based navigation | Section titles (Level1 through Level9) |
| `LiveSetting` | Live region announcements | Status areas that update dynamically (`Polite` or `Assertive`) |
| `AutomationId` | Test automation identifier | UI test automation (does not affect screen readers) |

### Live Regions

Announce dynamic content changes to screen readers without moving focus:

```xml
<!-- Polite: announced after current speech finishes -->
<TextBlock x:Name="StatusText"
           AutomationProperties.LiveSetting="Polite"
           Text="{x:Bind ViewModel.StatusMessage, Mode=OneWay}" />

<!-- Assertive: interrupts current speech (use sparingly) -->
<TextBlock x:Name="ErrorText"
           AutomationProperties.LiveSetting="Assertive"
           Text="{x:Bind ViewModel.ErrorMessage, Mode=OneWay}" />
```


## Custom Automation Peers

For custom controls, implement an `AutomationPeer` to expose the control to UI Automation clients:

```csharp
/// <summary>
/// A custom star rating control.
/// </summary>
public sealed class StarRating : Control
{
    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(int),
            typeof(StarRating), new PropertyMetadata(0, OnValueChanged));

    private static void OnValueChanged(DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (FrameworkElementAutomationPeer
                .FromElement((StarRating)d) is StarRatingAutomationPeer peer)
        {
            peer.RaiseValueChanged((int)e.OldValue, (int)e.NewValue);
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer()
        => new StarRatingAutomationPeer(this);
}

/// <summary>
/// Automation peer for <see cref="StarRating"/> that exposes the control
/// to UI Automation clients as a range value provider.
/// </summary>
public sealed class StarRatingAutomationPeer
    : FrameworkElementAutomationPeer, IRangeValueProvider
{
    private StarRating Owner => (StarRating)base.Owner;

    public StarRatingAutomationPeer(StarRating owner) : base(owner) { }

    protected override string GetClassNameCore() => nameof(StarRating);
    protected override string GetNameCore()
        => $"Rating: {Owner.Value} out of 5 stars";
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Slider;

    // IRangeValueProvider
    public double Value => Owner.Value;
    public double Minimum => 0;
    public double Maximum => 5;
    public double SmallChange => 1;
    public double LargeChange => 1;
    public bool IsReadOnly => false;

    public void SetValue(double value)
        => Owner.Value = (int)Math.Clamp(value, Minimum, Maximum);

    public void RaiseValueChanged(int oldValue, int newValue)
    {
        RaisePropertyChangedEvent(
            RangeValuePatternIdentifiers.ValueProperty,
            (double)oldValue, (double)newValue);
    }
}
```

### AutomationPeer Guidelines

- Override `GetNameCore()` to return a human-readable description of the control's current state
- Override `GetAutomationControlTypeCore()` to return the most appropriate control type
- Implement provider interfaces (`IRangeValueProvider`, `IToggleProvider`, `ISelectionItemProvider`, etc.) that match the control's interaction model
- Call `RaisePropertyChangedEvent()` when observable properties change
- Call `RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementSelected)` for selection changes


## Keyboard Accessibility

WinUI XAML controls provide built-in keyboard support. Ensure custom controls follow the same patterns:

```xml
<!-- TabIndex controls navigation order -->
<TextBox Header="First name" TabIndex="1" />
<TextBox Header="Last name" TabIndex="2" />
<Button Content="Submit" TabIndex="3" />

<!-- AccessKey provides keyboard shortcuts (Alt + key) -->
<Button Content="Save" AccessKey="S" />
<Button Content="Delete" AccessKey="D" />

<!-- IsTabStop controls whether a control participates in tab navigation -->
<Border IsTabStop="false">
    <TextBlock Text="Decorative panel" />
</Border>
```

### Custom Keyboard Handling

```csharp
/// <summary>
/// Handles keyboard navigation for a custom list control.
/// </summary>
private void OnKeyDown(object sender, KeyRoutedEventArgs e)
{
    switch (e.Key)
    {
        case VirtualKey.Down:
            MoveSelection(1);
            e.Handled = true;
            break;
        case VirtualKey.Up:
            MoveSelection(-1);
            e.Handled = true;
            break;
        case VirtualKey.Enter:
        case VirtualKey.Space:
            ConfirmSelection();
            e.Handled = true;
            break;
    }
}
```

### Focus Management

```csharp
// Move focus to a specific control
myTextBox.Focus(FocusState.Programmatic);

// Find the next focusable element
FocusManager.TryMoveFocus(FocusNavigationDirection.Next);

// Move focus in a specific direction
var options = new FindNextElementOptions
{
    SearchRoot = myPanel
};
var nextElement = FocusManager.FindNextFocusableElement(
    FocusNavigationDirection.Down, options);
```


## High Contrast Support

WinUI 3 apps should respond to the system high-contrast theme. Built-in controls handle this automatically. Custom controls and styles need explicit support.

```xml
<!-- Use ThemeResource for colors that adapt to high contrast -->
<TextBlock Foreground="{ThemeResource TextFillColorPrimaryBrush}"
           Text="This adapts to high contrast" />

<!-- WRONG: Hardcoded colors do not adapt -->
<TextBlock Foreground="#333333"
           Text="This breaks in high contrast" />
```

```csharp
// Detect high contrast mode
var accessibilitySettings = new Windows.UI.ViewManagement.AccessibilitySettings();
bool isHighContrast = accessibilitySettings.HighContrast;
string schemeName = accessibilitySettings.HighContrastScheme;

// Listen for high contrast changes
accessibilitySettings.HighContrastChanged += (s, e) =>
{
    // Update custom visuals
};
```

### ThemeResource Best Practices

- Always use `{ThemeResource}` instead of hardcoded colors for text, backgrounds, and borders
- Use system brush resources (`TextFillColorPrimaryBrush`, `ControlFillColorDefaultBrush`, etc.) that automatically adapt
- Test with all four Windows high-contrast themes (High Contrast #1, #2, Black, White)


## Accessibility Testing

### Tools

| Tool | Purpose |
|---|---|
| [Accessibility Insights for Windows](https://accessibilityinsights.io/) | Comprehensive inspection, live tree view, FastPass automated checks |
| Narrator (Win+Ctrl+Enter) | Built-in Windows screen reader for manual testing |
| Inspect.exe (Windows SDK) | Low-level UI Automation tree inspection |

### Manual Testing Checklist

1. **Keyboard-only navigation** -- tab through the entire app without a mouse; verify all functionality is reachable
2. **Screen reader walkthrough** -- enable Narrator and navigate the full workflow
3. **High contrast** -- enable each system high-contrast theme and verify all content remains visible
4. **Zoom/scaling** -- increase text size to 200% and verify layout does not break or clip content
5. **Color contrast** -- verify all text and interactive elements meet WCAG AA ratios (4.5:1 for text, 3:1 for large text and UI components)


## WCAG Reference

This skill references the [Web Content Accessibility Guidelines (WCAG)](https://www.w3.org/WAI/standards-guidelines/wcag/) as the global accessibility standard. WCAG 2.1 is the current baseline; WCAG 2.2 adds additional criteria for mobile and cognitive accessibility.

**Four principles (POUR):**
1. **Perceivable** -- information must be presentable in ways all users can perceive
2. **Operable** -- UI components must be operable by all users
3. **Understandable** -- information and UI operation must be understandable
4. **Robust** -- content must be robust enough to work with assistive technologies

**Conformance levels:** A (minimum), AA (recommended target for most apps), AAA (enhanced). Most legal requirements and industry standards target WCAG 2.1 Level AA.

**Note:** This skill provides technical implementation guidance. It does not constitute legal advice regarding accessibility compliance requirements, which vary by jurisdiction and application type.


## Agent Gotchas

1. **Do not hardcode colors without verifying contrast ratios.** Use `{ThemeResource}` brushes that adapt to high contrast. Test with Accessibility Insights.
2. **Do not forget `AccessKey` on frequently used buttons.** Access keys (Alt+key shortcuts) are essential for keyboard-dependent users and are trivial to add.
3. **Do not use `aria-live="assertive"` patterns for routine status updates.** In WinUI, use `AutomationProperties.LiveSetting="Polite"` for non-critical updates; reserve `Assertive` for errors and time-critical alerts.
4. **Do not forget `AutomationProperties.Name` on icon-only buttons.** Buttons without visible text content are invisible to screen readers unless `AutomationProperties.Name` is set.
5. **Do not set `AutomationProperties.Name` on controls that already have visible text.** For `Button` with `Content="Save"`, Narrator already reads "Save". Adding `AutomationProperties.Name` creates redundancy or mismatch.
6. **Do not forget to implement `OnCreateAutomationPeer()` on custom controls.** Without an automation peer, custom controls are invisible to UI Automation clients and screen readers.


## Prerequisites

- Windows App SDK 1.4+
- Testing tools: Accessibility Insights for Windows, Inspect.exe (Windows SDK)
- Screen reader: Narrator (built-in), NVDA (free, third-party)


## References

- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [WCAG 2.2 Guidelines](https://www.w3.org/TR/WCAG22/)
- [WinUI Accessibility Overview](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/accessibility-overview)
- [UI Automation Overview](https://learn.microsoft.com/en-us/windows/desktop/WinAuto/uiauto-uiautomationoverview)
- [Accessibility Insights](https://accessibilityinsights.io/)
