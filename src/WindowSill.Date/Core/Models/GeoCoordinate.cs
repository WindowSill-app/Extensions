namespace WindowSill.Date.Core.Models;

/// <summary>
/// Represents a geographic coordinate (latitude and longitude).
/// </summary>
/// <param name="Latitude">The latitude in decimal degrees.</param>
/// <param name="Longitude">The longitude in decimal degrees.</param>
internal readonly record struct GeoCoordinate(double Latitude, double Longitude)
{
    /// <inheritdoc/>
    public override string ToString() => $"{Latitude:F6},{Longitude:F6}";
}
