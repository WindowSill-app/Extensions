using NodaTime;

namespace WindowSill.Date.Core.Services;

/// <summary>
/// Represents a city search result for timezone selection.
/// </summary>
/// <param name="TimeZoneId">The IANA timezone identifier.</param>
/// <param name="CityName">The user-friendly city name.</param>
/// <param name="CountryName">The country name.</param>
/// <param name="CurrentUtcOffset">The current UTC offset for this timezone.</param>
internal sealed record CitySearchResult(
    string TimeZoneId,
    string CityName,
    string CountryName,
    Offset CurrentUtcOffset)
{
    /// <inheritdoc/>
    public override string ToString()
        => $"{CityName}, {CountryName} (UTC{CurrentUtcOffset})";
}
