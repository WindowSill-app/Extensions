namespace WindowSill.InlineTerminal.Core.UI.Converters;

/// <summary>
/// Converts a boolean (word wrap enabled) to <see cref="ScrollMode"/>.
/// When true (wrap on), returns <see cref="ScrollMode.Disabled"/> to prevent horizontal scrolling.
/// When false (wrap off), returns <see cref="ScrollMode.Auto"/> to enable horizontal scrolling.
/// </summary>
internal sealed class BoolToScrollModeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? ScrollMode.Disabled : ScrollMode.Auto;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is ScrollMode.Disabled;
    }
}
