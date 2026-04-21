using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Core.Services;

/// <summary>
/// Orchestrates user location, address geocoding, and routing to estimate
/// travel time to a calendar event with a physical location.
/// </summary>
internal interface ITravelTimeEstimator
{
    /// <summary>
    /// Estimates the travel time from the user's current location to the meeting's address.
    /// </summary>
    /// <param name="calendarEvent">The calendar event with a Location field.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A result containing the estimated travel duration (from provider or fallback),
    /// or a failed result with a reason if estimation is not possible.
    /// </returns>
    Task<TravelTimeEstimateResult> EstimateTravelTimeAsync(
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken = default);
}
