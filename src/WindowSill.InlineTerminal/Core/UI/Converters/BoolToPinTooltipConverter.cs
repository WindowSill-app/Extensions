using WindowSill.API;

namespace WindowSill.InlineTerminal.Core.UI.Converters;

/// <summary>
/// Converts a boolean IsPinned value to a localized tooltip string.
/// </summary>
internal sealed class BoolToPinTooltipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true
            ? "/WindowSill.InlineTerminal/CommandPopupResultPage/UnpinButton".GetLocalizedString()
            : "/WindowSill.InlineTerminal/CommandPopupResultPage/PinButton".GetLocalizedString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
