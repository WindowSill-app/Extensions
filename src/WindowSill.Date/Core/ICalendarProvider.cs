using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Core;

/// <summary>
/// Represents a calendar provider factory that handles account authentication
/// and creates per-account clients. Each provider is exported via MEF.
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
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The newly connected account information.</returns>
    Task<CalendarAccount> ConnectAccountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a per-account client for fetching calendars and events.
    /// </summary>
    /// <param name="account">The account to create a client for.</param>
    /// <returns>A client scoped to the specified account.</returns>
    ICalendarAccountClient CreateClient(CalendarAccount account);
}
