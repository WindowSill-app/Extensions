namespace WindowSill.Date.Core.Models;

/// <summary>
/// The result of a travel time estimation, including whether it came from
/// a routing provider or a fallback default.
/// </summary>
internal sealed class TravelTimeEstimateResult
{
    private TravelTimeEstimateResult() { }

    /// <summary>
    /// Gets the estimated travel duration, if available.
    /// </summary>
    public TimeSpan? Duration { get; private init; }

    /// <summary>
    /// Gets the distance in meters, if available from the routing provider.
    /// </summary>
    public double? DistanceMeters { get; private init; }

    /// <summary>
    /// Gets a value indicating whether the fallback commute time was used
    /// instead of a real routing estimate.
    /// </summary>
    public bool UsedFallback { get; private init; }

    /// <summary>
    /// Gets the name of the routing provider that produced this estimate (e.g., "OpenRouteService").
    /// </summary>
    public string? Provider { get; private init; }

    /// <summary>
    /// Gets the failure reason if the estimate could not be produced.
    /// </summary>
    public TravelTimeFailureReason? FailureReason { get; private init; }

    /// <summary>
    /// Gets a value indicating whether this result represents a successful estimate
    /// (either from a provider or a fallback).
    /// </summary>
    public bool IsSuccess => Duration.HasValue;

    /// <summary>
    /// Creates a successful result from a routing provider.
    /// </summary>
    public static TravelTimeEstimateResult FromProvider(TimeSpan duration, double distanceMeters, string provider)
        => new()
        {
            Duration = duration,
            DistanceMeters = distanceMeters,
            Provider = provider,
        };

    /// <summary>
    /// Creates a result using the fallback commute time.
    /// </summary>
    public static TravelTimeEstimateResult FromFallback(TimeSpan fallbackDuration)
        => new()
        {
            Duration = fallbackDuration,
            UsedFallback = true,
        };

    /// <summary>
    /// Creates a failed result with the given reason.
    /// </summary>
    public static TravelTimeEstimateResult Failed(TravelTimeFailureReason reason)
        => new()
        {
            FailureReason = reason,
        };
}
