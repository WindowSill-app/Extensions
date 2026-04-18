using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Core.Providers.CalDav;

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
    public virtual async Task<CalendarAccount> ConnectAccountAsync(CancellationToken cancellationToken)
    {
        // TODO: Prompt user for server URL, username, and password via UI.
        throw new NotImplementedException("CalDAV account setup not yet implemented.");
    }

    /// <inheritdoc />
    public virtual ICalendarAccountClient CreateClient(
        CalendarAccount account,
        Func<IReadOnlyDictionary<string, string>, CancellationToken, Task> onAuthDataChanged)
    {
        return new CalDavCalendarAccountClient(account, account.AuthData);
    }
}
