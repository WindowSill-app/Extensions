namespace WindowSill.InlineTerminal.Core.UI.Converters;

/// <summary>
/// Converts a boolean value to a <see cref="TextWrapping"/> value.
/// Returns <see cref="TextWrapping.Wrap"/> when true, <see cref="TextWrapping.NoWrap"/> when false.
/// </summary>
internal sealed class BoolToTextWrappingConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? TextWrapping.Wrap : TextWrapping.NoWrap;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is TextWrapping.Wrap;
    }
}
