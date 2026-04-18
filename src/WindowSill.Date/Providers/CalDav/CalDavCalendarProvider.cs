using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Providers.CalDav;

/// <summary>
/// Calendar provider for generic CalDAV servers (RFC 4791).
/// </summary>
internal class CalDavCalendarProvider : ICalendarProvider
{
    /// <inheritdoc />
    public virtual CalendarProviderType ProviderType => CalendarProviderType.CalDav;

    /// <inheritdoc />
    public virtual string DisplayName => "CalDAV";

    /// <inheritdoc />
    public virtual async Task<(CalendarAccount Account, Dictionary<string, string> AuthData)> ConnectAccountAsync(
        CancellationToken cancellationToken)
    {
        // CalDAV typically uses username/password or app-specific password.
        // TODO: Prompt user for server URL, username, and password via UI.
        // Return authData with server_url, username, password.
        throw new NotImplementedException("CalDAV account setup not yet implemented.");
    }

    /// <inheritdoc />
    public virtual ICalendarAccountClient CreateClient(
        CalendarAccount account,
        IReadOnlyDictionary<string, string> authData,
        Func<IReadOnlyDictionary<string, string>, CancellationToken, Task> onAuthDataChanged)
    {
        return new CalDavCalendarAccountClient(account, authData);
    }
}
