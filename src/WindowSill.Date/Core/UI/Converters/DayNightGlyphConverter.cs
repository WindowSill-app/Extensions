namespace WindowSill.Date.Core.UI.Converters;

/// <summary>
/// Converts a boolean "isDaytime" value to a sun/moon glyph.
/// </summary>
internal sealed class DayNightGlyphConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, string language)
        => value is true ? "\uE706" : "\uE708"; // Sunny / ClearNight

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
        => throw new NotImplementedException();
}
