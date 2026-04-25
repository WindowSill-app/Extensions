using System.Text.Json.Serialization;

namespace WindowSill.Date.Core.Models;

/// <summary>
/// Represents a connected calendar account from a specific provider,
/// including opaque auth data that the provider needs to authenticate.
/// </summary>
public sealed class CalendarAccount
{
    /// <summary>
    /// Gets the unique identifier for this account.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the user-facing display name (e.g., "Work - John Doe").
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the email address associated with this account.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets the calendar provider type for this account.
    /// </summary>
    public required CalendarProviderType ProviderType { get; init; }

    /// <summary>
    /// Gets the provider-specific authentication/credential data.
    /// Keys and values are defined by each provider. The manager persists
    /// this opaquely — it does not interpret the contents.
    /// </summary>
    [JsonPropertyName("authData")]
    public IReadOnlyDictionary<string, string> AuthData { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets the set of calendar IDs that the user has chosen to hide.
    /// Calendars not in this set are visible by default.
    /// </summary>
    [JsonPropertyName("hiddenCalendarIds")]
    public HashSet<string> HiddenCalendarIds { get; init; } = [];

    /// <summary>
    /// Gets the user-defined color overrides per calendar, keyed by calendar ID.
    /// Values are hex color strings (e.g., "#FF5733"). Calendars not in this
    /// dictionary use their provider-assigned color.
    /// </summary>
    [JsonPropertyName("calendarColorOverrides")]
    public IReadOnlyDictionary<string, string> CalendarColorOverrides { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Creates a copy of this account with updated auth data.
    /// </summary>
    /// <param name="authData">The new auth data.</param>
    /// <returns>A new account with the same metadata but updated auth data.</returns>
    public CalendarAccount WithAuthData(IReadOnlyDictionary<string, string> authData)
    {
        return new CalendarAccount
        {
            Id = Id,
            DisplayName = DisplayName,
            Email = Email,
            ProviderType = ProviderType,
            AuthData = authData,
            HiddenCalendarIds = new HashSet<string>(HiddenCalendarIds),
            CalendarColorOverrides = new Dictionary<string, string>(CalendarColorOverrides),
        };
    }

    /// <summary>
    /// Creates a copy of this account with updated hidden calendar IDs.
    /// </summary>
    /// <param name="hiddenCalendarIds">The new set of hidden calendar IDs.</param>
    /// <returns>A new account with the same metadata but updated hidden calendars.</returns>
    public CalendarAccount WithHiddenCalendarIds(HashSet<string> hiddenCalendarIds)
    {
        return new CalendarAccount
        {
            Id = Id,
            DisplayName = DisplayName,
            Email = Email,
            ProviderType = ProviderType,
            AuthData = AuthData,
            HiddenCalendarIds = hiddenCalendarIds,
            CalendarColorOverrides = new Dictionary<string, string>(CalendarColorOverrides),
        };
    }

    /// <summary>
    /// Creates a copy of this account with updated calendar color overrides.
    /// </summary>
    /// <param name="calendarColorOverrides">The new color overrides dictionary.</param>
    /// <returns>A new account with the same metadata but updated color overrides.</returns>
    public CalendarAccount WithCalendarColorOverrides(IReadOnlyDictionary<string, string> calendarColorOverrides)
    {
        return new CalendarAccount
        {
            Id = Id,
            DisplayName = DisplayName,
            Email = Email,
            ProviderType = ProviderType,
            AuthData = AuthData,
            HiddenCalendarIds = new HashSet<string>(HiddenCalendarIds),
            CalendarColorOverrides = calendarColorOverrides,
        };
    }
}
