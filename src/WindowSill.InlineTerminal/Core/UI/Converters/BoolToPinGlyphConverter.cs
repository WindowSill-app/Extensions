namespace WindowSill.InlineTerminal.Core.UI.Converters;

/// <summary>
/// Converts a boolean IsPinned value to the appropriate pin/unpin glyph.
/// </summary>
internal sealed class BoolToPinGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? "\uE77A" : "\uE718";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
