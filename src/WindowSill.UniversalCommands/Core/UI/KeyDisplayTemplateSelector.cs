using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WindowSill.UniversalCommands.Core.UI;

/// <summary>
/// Selects between a key pill template and a separator template
/// based on whether the display token is empty.
/// </summary>
public sealed partial class KeyDisplayTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Gets or sets the template used to display a key name inside a pill.
    /// </summary>
    public DataTemplate? KeyTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template used to display a chord separator.
    /// </summary>
    public DataTemplate? SeparatorTemplate { get; set; }

    /// <inheritdoc/>
    protected override DataTemplate SelectTemplateCore(object item)
    {
        return item is string { Length: 0 }
            ? SeparatorTemplate!
            : KeyTemplate!;
    }
}
