using Microsoft.UI;

namespace WindowSill.Date.Core.UI.Converters;

/// <summary>
/// Converts a hex color string (e.g., "#FF5733") to a <see cref="Windows.UI.Color"/>.
/// Used for gradient stops where a Brush is not accepted.
/// </summary>
internal sealed class HexToColorConverter : Microsoft.UI.Xaml.Data.IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is string hex && !string.IsNullOrEmpty(hex))
        {
            try
            {
                hex = hex.TrimStart('#');
                if (hex.Length == 6)
                {
                    byte r = System.Convert.ToByte(hex[..2], 16);
                    byte g = System.Convert.ToByte(hex[2..4], 16);
                    byte b = System.Convert.ToByte(hex[4..6], 16);
                    return ColorHelper.FromArgb(255, r, g, b);
                }
            }
            catch
            {
                // Fall through.
            }
        }

        return Colors.Gray;
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
        => throw new NotImplementedException();
}
