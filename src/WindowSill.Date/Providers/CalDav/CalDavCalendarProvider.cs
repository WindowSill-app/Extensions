using System.ComponentModel.Composition;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Providers.CalDav;

/// <summary>
/// Calendar provider for generic CalDAV servers (RFC 4791).
/// </summary>
internal class CalDavCalendarProvider : ICalendarProvider
{
    private readonly ICalendarCredentialStore _credentialStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalDavCalendarProvider"/> class.
    /// </summary>
    /// <param name="credentialStore">The credential store for persisting credentials.</param>
    internal CalDavCalendarProvider(ICalendarCredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    /// <inheritdoc />
    public virtual CalendarProviderType ProviderType => CalendarProviderType.CalDav;

    /// <inheritdoc />
    public virtual string DisplayName => "CalDAV";

    /// <inheritdoc />
    public virtual async Task<CalendarAccount> ConnectAccountAsync(CancellationToken cancellationToken)
    {
        // CalDAV typically uses username/password or app-specific password.
        // TODO: Prompt user for server URL, username, and password via UI.
        throw new NotImplementedException("CalDAV account setup not yet implemented.");
    }

    /// <inheritdoc />
    public virtual ICalendarAccountClient CreateClient(CalendarAccount account)
    {
        return new CalDavCalendarAccountClient(account, _credentialStore);
    }
}
