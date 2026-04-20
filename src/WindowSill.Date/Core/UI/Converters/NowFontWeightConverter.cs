namespace WindowSill.Date.Core.UI.Converters;

/// <summary>
/// Converts a boolean "isNow" value to a <see cref="FontWeight"/>.
/// </summary>
internal sealed class NowFontWeightConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, string language)
        => value is true
            ? Microsoft.UI.Text.FontWeights.SemiBold
            : Microsoft.UI.Text.FontWeights.Normal;

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
        => throw new NotImplementedException();
}
