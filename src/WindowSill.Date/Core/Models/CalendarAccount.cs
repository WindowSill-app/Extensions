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
        };
    }
}
