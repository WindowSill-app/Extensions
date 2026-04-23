using System.ComponentModel.Composition;

using Microsoft.Extensions.Logging;

using WindowSill.API;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Core.Services;

/// <summary>
/// Orchestrates user geolocation, address geocoding, and routing to estimate
/// travel time to a calendar event's physical location.
/// Falls back to a configurable default commute time when routing is unavailable.
/// </summary>
[Export(typeof(ITravelTimeEstimator))]
internal sealed class TravelTimeEstimator : ITravelTimeEstimator
{
    private readonly ILogger _logger;
    private readonly IGeoLocationService _geoLocationService;
    private readonly IGeocodingService _geocodingService;
    private readonly IRoutingService _routingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TravelTimeEstimator"/> class.
    /// </summary>
    [ImportingConstructor]
    public TravelTimeEstimator(
        IGeoLocationService geoLocationService,
        IGeocodingService geocodingService,
        IRoutingService routingService)
    {
        _logger = this.Log();
        _geoLocationService = geoLocationService;
        _geocodingService = geocodingService;
        _routingService = routingService;
    }

    /// <inheritdoc/>
    public async Task<TravelTimeEstimateResult> EstimateTravelTimeAsync(
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken = default)
    {
        // Step 0: Check if the event has a location.
        if (string.IsNullOrWhiteSpace(calendarEvent.Location))
        {
            return TravelTimeEstimateResult.Failed(TravelTimeFailureReason.NoMeetingAddress);
        }

        // Step 1: Get user's current location.
        GeoCoordinate? userLocation = await _geoLocationService.GetCurrentLocationAsync(cancellationToken);
        if (userLocation is null)
        {
            _logger.LogInformation("User location unavailable, using fallback commute time.");
            return TravelTimeEstimateResult.Failed(TravelTimeFailureReason.NoUserLocation);
        }

        // Step 2: Geocode the meeting address.
        GeoCoordinate? meetingLocation = await _geocodingService.GeocodeAsync(calendarEvent.Location, cancellationToken);
        if (meetingLocation is null)
        {
            _logger.LogInformation(
                "Could not geocode meeting address: {Address}. Using fallback commute time.",
                calendarEvent.Location);
            return TravelTimeEstimateResult.Failed(TravelTimeFailureReason.InvalidMeetingAddress);
        }

        // Step 3: Get routing estimate.
        TravelTimeEstimateResult? routeResult = await _routingService.GetTravelTimeAsync(
            userLocation.Value,
            meetingLocation.Value,
            cancellationToken);

        if (routeResult is not null && routeResult.IsSuccess)
        {
            return routeResult;
        }

        // Routing failed — check reason.
        if (routeResult?.FailureReason == TravelTimeFailureReason.NoApiKey)
        {
            // No API key configured — return fallback.
            return TravelTimeEstimateResult.Failed(TravelTimeFailureReason.NoApiKey);
        }

        if (routeResult?.FailureReason == TravelTimeFailureReason.RateLimited)
        {
            _logger.LogWarning("Routing provider rate-limited. Using fallback commute time.");
            return TravelTimeEstimateResult.Failed(TravelTimeFailureReason.RateLimited);
        }

        // Generic routing failure — use fallback.
        _logger.LogWarning("Routing failed ({Reason}). Using fallback commute time.", routeResult?.FailureReason);
        return TravelTimeEstimateResult.Failed(TravelTimeFailureReason.RoutingProviderError);
    }
}
