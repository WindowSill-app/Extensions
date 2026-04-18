namespace WindowSill.Date.Core.Models;

/// <summary>
/// Represents a connected calendar account from a specific provider.
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
}
