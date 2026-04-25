using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Core.Services;

/// <summary>
/// Converts a text address into geographic coordinates.
/// </summary>
internal interface IGeocodingService
{
    /// <summary>
    /// Geocodes an address string into geographic coordinates.
    /// </summary>
    /// <param name="address">The address to geocode (e.g., "1600 Pennsylvania Ave, Washington DC").</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The coordinates, or <see langword="null"/> if the address could not be resolved.</returns>
    Task<GeoCoordinate?> GeocodeAsync(string address, CancellationToken cancellationToken = default);
}
