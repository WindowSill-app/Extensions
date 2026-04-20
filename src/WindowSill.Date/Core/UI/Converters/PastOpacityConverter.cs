namespace WindowSill.Date.Core.UI.Converters;

/// <summary>
/// Converts a boolean "isPast" value to an opacity (0.5 for past, 1.0 for current/future).
/// </summary>
internal sealed class PastOpacityConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, string language)
        => value is true ? 0.5 : 1.0;

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
        => throw new NotImplementedException();
}
