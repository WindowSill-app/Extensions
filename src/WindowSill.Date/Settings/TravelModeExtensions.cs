namespace WindowSill.Date.Settings;

/// <summary>
/// Extension methods for <see cref="TravelMode"/>.
/// </summary>
internal static class TravelModeExtensions
{
    /// <summary>
    /// Returns the OpenRouteService profile string for this travel mode.
    /// </summary>
    public static string ToOrsProfile(this TravelMode mode) => mode switch
    {
        TravelMode.Driving => "driving-car",
        TravelMode.Walking => "foot-walking",
        TravelMode.Cycling => "cycling-regular",
        _ => "driving-car",
    };

    /// <summary>
    /// Returns the Segoe Fluent Icons glyph for this travel mode.
    /// </summary>
    public static string ToIconGlyph(this TravelMode mode) => mode switch
    {
        TravelMode.Driving => "\U0001F697",
        TravelMode.Walking => "\U0001F6B6",
        TravelMode.Cycling => "\U0001F6B2",
        _ => "\U0001F697",
    };
}
