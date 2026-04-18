using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Core;

/// <summary>
/// Represents a calendar provider factory that handles account authentication
/// and creates per-account clients. Providers do not access storage directly —
/// they produce auth data on connect and receive it back when creating clients.
/// </summary>
public interface ICalendarProvider
{
    /// <summary>
    /// Gets the type of calendar provider.
    /// </summary>
    CalendarProviderType ProviderType { get; }

    /// <summary>
    /// Gets the human-readable display name for this provider (e.g., "Microsoft Outlook").
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Initiates an interactive authentication flow and connects a new account.
    /// Returns both the account and the auth data to persist.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The account and auth data that should be persisted by the manager.</returns>
    Task<(CalendarAccount Account, Dictionary<string, string> AuthData)> ConnectAccountAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a per-account client for fetching calendars and events.
    /// The provider receives the previously persisted auth data and a callback
    /// to persist updated auth data when tokens are refreshed.
    /// </summary>
    /// <param name="account">The account to create a client for.</param>
    /// <param name="authData">The persisted auth data from a previous session or connect.</param>
    /// <param name="onAuthDataChanged">
    /// Callback to persist updated auth data (e.g., after a token refresh).
    /// Providers must call this whenever their auth state changes.
    /// </param>
    /// <returns>A client scoped to the specified account.</returns>
    ICalendarAccountClient CreateClient(
        CalendarAccount account,
        IReadOnlyDictionary<string, string> authData,
        Func<IReadOnlyDictionary<string, string>, CancellationToken, Task> onAuthDataChanged);
}
