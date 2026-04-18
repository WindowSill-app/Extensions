using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Providers.CalDav;

namespace WindowSill.Date.Core.Providers.ICloud;

/// <summary>
/// Per-account client for Apple iCloud Calendar, extending CalDAV with Apple-specific endpoints.
/// iCloud CalDAV server: https://caldav.icloud.com
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

    /// <summary>
    /// Gets the iCloud CalDAV server URL.
    /// </summary>
    protected override string CalDavBaseUrl => ICloudCalDavServer;
}
