using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Core;

/// <summary>
/// A per-account client for interacting with a calendar provider.
/// Each instance is scoped to a single connected account.
/// </summary>
public interface ICalendarAccountClient : IAsyncDisposable
{
    /// <summary>
    /// Gets the account this client is scoped to.
    /// </summary>
    CalendarAccount Account { get; }

    /// <summary>
    /// Retrieves the list of calendars available in this account (e.g., "Work", "Personal").
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The calendars available in this account.</returns>
    Task<IReadOnlyList<CalendarInfo>> GetCalendarsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves calendar events within the specified time range across all calendars in this account.
    /// </summary>
    /// <param name="from">The start of the time range (inclusive).</param>
    /// <param name="to">The end of the time range (exclusive).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The events occurring within the specified time range.</returns>
    Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to refresh the authentication credentials for this account.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the credentials were successfully refreshed; otherwise, <see langword="false"/>.</returns>
    Task<bool> RefreshAuthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects this account by revoking tokens and cleaning up resources.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
