using System.Text.Json.Serialization;

namespace WindowSill.Date.Core.Models;

/// <summary>
/// Represents a user-configured world clock entry with a timezone and optional custom display name.
/// </summary>
internal sealed class WorldClockEntry
{
    /// <summary>
    /// Gets the IANA timezone identifier (e.g., "America/New_York").
    /// </summary>
    [JsonPropertyName("tz")]
    public required string TimeZoneId { get; init; }

    /// <summary>
    /// Gets the user-defined display name, or <see langword="null"/> to use the default city name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? CustomDisplayName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this clock is pinned to the sill bar.
    /// </summary>
    [JsonPropertyName("pin")]
    public bool ShowInBar { get; set; }

    /// <summary>
    /// Gets the display name to show in the UI. Returns the custom name if set,
    /// otherwise derives a friendly name from the timezone ID.
    /// </summary>
    [JsonIgnore]
    public string DisplayName => CustomDisplayName ?? GetDefaultDisplayName();

    private string GetDefaultDisplayName()
    {
        // "America/New_York" → "New York", "Asia/Ho_Chi_Minh" → "Ho Chi Minh"
        int lastSlash = TimeZoneId.LastIndexOf('/');
        string city = lastSlash >= 0 ? TimeZoneId[(lastSlash + 1)..] : TimeZoneId;
        return city.Replace('_', ' ');
    }
}
