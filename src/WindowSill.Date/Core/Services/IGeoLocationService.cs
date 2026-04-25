using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Core.Services;

/// <summary>
/// Provides the user's current geographic location.
/// </summary>
internal interface IGeoLocationService
{
    /// <summary>
    /// Gets the user's current geographic coordinates.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The user's coordinates, or <see langword="null"/> if location is unavailable or denied.</returns>
    Task<GeoCoordinate?> GetCurrentLocationAsync(CancellationToken cancellationToken = default);
}
