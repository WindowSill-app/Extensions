using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Core;

/// <summary>
/// The data persisted for a single calendar account. Contains account metadata
/// and a generic auth data dictionary that each provider populates with whatever
/// it needs (tokens, passwords, server URLs, etc.).
/// </summary>
public sealed class AccountData
{
    /// <summary>
    /// Gets the unique identifier for this account.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the user-facing display name.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the email address associated with this account.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets the calendar provider type.
    /// </summary>
    public required CalendarProviderType ProviderType { get; init; }

    /// <summary>
    /// Gets the provider-specific authentication/credential data.
    /// Keys and values are defined by each provider. The manager persists
    /// this opaquely — it does not interpret the contents.
    /// </summary>
    public Dictionary<string, string> AuthData { get; init; } = new();

    /// <summary>
    /// Creates a <see cref="CalendarAccount"/> from this data.
    /// </summary>
    public CalendarAccount ToCalendarAccount()
    {
        return new CalendarAccount
        {
            Id = Id,
            DisplayName = DisplayName,
            Email = Email,
            ProviderType = ProviderType,
        };
    }

    /// <summary>
    /// Creates an <see cref="AccountData"/> from a <see cref="CalendarAccount"/> and auth data.
    /// </summary>
    public static AccountData FromAccount(CalendarAccount account, Dictionary<string, string> authData)
    {
        return new AccountData
        {
            Id = account.Id,
            DisplayName = account.DisplayName,
            Email = account.Email,
            ProviderType = account.ProviderType,
            AuthData = authData,
        };
    }
}
