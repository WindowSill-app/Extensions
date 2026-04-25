namespace WindowSill.Date.Core.UI.Converters;

/// <summary>
/// Converts a hex color string (e.g., "#FF5733") to a <see cref="SolidColorBrush"/>.
/// </summary>
internal sealed class HexColorBrushConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is string hex && !string.IsNullOrEmpty(hex))
        {
            try
            {
                hex = hex.TrimStart('#');

                byte r, g, b;
                if (hex.Length == 6)
                {
                    r = System.Convert.ToByte(hex[..2], 16);
                    g = System.Convert.ToByte(hex[2..4], 16);
                    b = System.Convert.ToByte(hex[4..6], 16);
                    return new SolidColorBrush(ColorHelper.FromArgb(255, r, g, b));
                }
            }
            catch
            {
                // Fall through to default.
            }
        }

        return new SolidColorBrush(Colors.Gray);
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
        => throw new NotImplementedException();
}
