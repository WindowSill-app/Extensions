namespace WindowSill.Date.Core.Models;

/// <summary>
/// Reasons why a travel time estimation could not be produced.
/// </summary>
internal enum TravelTimeFailureReason
{
    /// <summary>
    /// User location could not be determined (permission denied or unavailable).
    /// </summary>
    NoUserLocation,

    /// <summary>
    /// The meeting has no physical address in its Location field.
    /// </summary>
    NoMeetingAddress,

    /// <summary>
    /// The meeting address could not be geocoded to coordinates.
    /// </summary>
    InvalidMeetingAddress,

    /// <summary>
    /// No routing API key is configured.
    /// </summary>
    NoApiKey,

    /// <summary>
    /// The routing provider returned an error or was unreachable.
    /// </summary>
    RoutingProviderError,

    /// <summary>
    /// The request was rate-limited by the provider.
    /// </summary>
    RateLimited,
}
