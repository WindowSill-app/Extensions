using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Core.Services;

/// <summary>
/// Calculates travel time between two geographic coordinates.
/// </summary>
internal interface IRoutingService
{
    /// <summary>
    /// Gets the name of this routing provider (e.g., "OpenRouteService").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Calculates the driving travel time and distance between two points.
    /// </summary>
    /// <param name="from">The origin coordinates.</param>
    /// <param name="to">The destination coordinates.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result with duration and distance, or <see langword="null"/> on failure.</returns>
    Task<TravelTimeEstimateResult?> GetTravelTimeAsync(
        GeoCoordinate from,
        GeoCoordinate to,
        CancellationToken cancellationToken = default);
}
