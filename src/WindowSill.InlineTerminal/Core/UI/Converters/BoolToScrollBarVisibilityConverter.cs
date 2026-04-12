namespace WindowSill.InlineTerminal.Core.UI.Converters;

/// <summary>
/// Converts a boolean (word wrap enabled) to <see cref="ScrollBarVisibility"/>.
/// When true (wrap on), returns <see cref="ScrollBarVisibility.Disabled"/> to constrain horizontal space.
/// When false (wrap off), returns <see cref="ScrollBarVisibility.Auto"/> to allow horizontal scrolling.
/// </summary>
internal sealed class BoolToScrollBarVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is ScrollBarVisibility.Disabled;
    }
}
