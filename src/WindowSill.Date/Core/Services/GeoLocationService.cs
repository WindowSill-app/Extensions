using System.ComponentModel.Composition;

using Microsoft.Extensions.Logging;

using Windows.Devices.Geolocation;

using WindowSill.API;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Core.Services;

/// <summary>
/// Provides the user's current geographic location via the Windows Geolocation API.
/// Caches the last known position for a configurable duration to avoid repeated OS prompts.
/// </summary>
[Export(typeof(IGeoLocationService))]
internal sealed class GeoLocationService : IGeoLocationService
{
    private static readonly TimeSpan cacheDuration = TimeSpan.FromMinutes(5);

    private readonly ILogger _logger;

    private GeoCoordinate? _cachedLocation;
    private DateTimeOffset _cacheExpiry = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeoLocationService"/> class.
    /// </summary>
    [ImportingConstructor]
    public GeoLocationService()
    {
        _logger = this.Log();
    }

    /// <inheritdoc/>
    public async Task<GeoCoordinate?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedLocation.HasValue && DateTimeOffset.Now < _cacheExpiry)
        {
            return _cachedLocation;
        }

        try
        {
            GeolocationAccessStatus accessStatus = await Geolocator.RequestAccessAsync()
                .AsTask(cancellationToken);

            if (accessStatus != GeolocationAccessStatus.Allowed)
            {
                _logger.LogWarning("Geolocation access denied: {Status}", accessStatus);
                return null;
            }

            var geolocator = new Geolocator
            {
                DesiredAccuracy = PositionAccuracy.Default,
            };

            Geoposition position = await geolocator.GetGeopositionAsync()
                .AsTask(cancellationToken);

            var coordinate = new GeoCoordinate(
                position.Coordinate.Point.Position.Latitude,
                position.Coordinate.Point.Position.Longitude);

            _cachedLocation = coordinate;
            _cacheExpiry = DateTimeOffset.Now + cacheDuration;

            return coordinate;
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Geolocation access unauthorized.");
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to get geolocation.");
            return null;
        }
    }
}
