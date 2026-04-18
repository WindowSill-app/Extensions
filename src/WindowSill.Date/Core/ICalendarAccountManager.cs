using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Core;

/// <summary>
/// Manages connected calendar accounts across all providers and provides
/// aggregated access to calendar events.
/// </summary>
public interface ICalendarAccountManager
{
    /// <summary>
    /// Gets all currently connected accounts.
    /// </summary>
    /// <returns>A read-only list of connected calendar accounts.</returns>
    IReadOnlyList<CalendarAccount> GetAccounts();

    /// <summary>
    /// Adds a new account by initiating the authentication flow for the specified provider.
    /// </summary>
    /// <param name="providerType">The type of calendar provider to connect.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly connected account.</returns>
    Task<CalendarAccount> AddAccountAsync(CalendarProviderType providerType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a connected account and cleans up associated resources.
    /// </summary>
    /// <param name="accountId">The identifier of the account to remove.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task RemoveAccountAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the account client for a specific connected account.
    /// </summary>
    /// <param name="accountId">The identifier of the account.</param>
    /// <returns>The client scoped to the specified account.</returns>
    ICalendarAccountClient GetClientForAccount(string accountId);

    /// <summary>
    /// Retrieves upcoming events across all connected accounts within the specified look-ahead window.
    /// Events are sorted by start time and deduplicated across calendars.
    /// </summary>
    /// <param name="lookAhead">How far ahead to look for events.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A sorted, deduplicated list of upcoming events.</returns>
    Task<IReadOnlyList<CalendarEvent>> GetUpcomingEventsAsync(
        TimeSpan lookAhead,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Raised when a new account is successfully connected.
    /// </summary>
    event EventHandler<CalendarAccount>? AccountAdded;

    /// <summary>
    /// Raised when an account is disconnected and removed.
    /// </summary>
    event EventHandler<CalendarAccount>? AccountRemoved;
}
