using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Providers.CalDav;

namespace WindowSill.Date.Core.Providers.ICloud;

/// <summary>
/// Per-account client for Apple iCloud Calendar.
/// Extends the CalDAV base client with the iCloud-specific server URL.
/// All discovery, calendar listing, and event fetching is handled by the base class.
/// </summary>
internal sealed class ICloudCalendarAccountClient : CalDavCalendarAccountClient
{
    private const string ICloudCalDavServer = "https://caldav.icloud.com";

    /// <summary>
    /// Initializes a new instance of the <see cref="ICloudCalendarAccountClient"/> class.
    /// </summary>
    /// <param name="account">The account this client is scoped to.</param>
    /// <param name="authData">The persisted auth data (username, password).</param>
    internal ICloudCalendarAccountClient(CalendarAccount account, IReadOnlyDictionary<string, string> authData)
        : base(account, authData)
    {
    }

    /// <inheritdoc />
    protected override string CalDavBaseUrl => ICloudCalDavServer;

    /// <summary>
    /// Validates iCloud credentials by attempting principal discovery.
    /// </summary>
    internal static Task<string> ValidateAndDiscoverAsync(
        string appleId, string appPassword, CancellationToken cancellationToken)
    {
        return CalDavCalendarAccountClient.ValidateAndDiscoverAsync(
            ICloudCalDavServer, appleId, appPassword, cancellationToken);
    }
}
