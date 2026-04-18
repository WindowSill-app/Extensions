using System.ComponentModel.Composition;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Providers.Outlook;

/// <summary>
/// Calendar provider for Microsoft Outlook using the Microsoft Graph API.
/// </summary>
[Export(typeof(ICalendarProvider))]
internal sealed class OutlookCalendarProvider : ICalendarProvider
{
    private readonly ICalendarAuthBroker _authBroker;
    private readonly ICalendarCredentialStore _credentialStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookCalendarProvider"/> class.
    /// </summary>
    /// <param name="authBroker">The auth broker for handling OAuth flows.</param>
    /// <param name="credentialStore">The credential store for persisting tokens.</param>
    [ImportingConstructor]
    public OutlookCalendarProvider(ICalendarAuthBroker authBroker, ICalendarCredentialStore credentialStore)
    {
        _authBroker = authBroker;
        _credentialStore = credentialStore;
    }

    /// <inheritdoc />
    public CalendarProviderType ProviderType => CalendarProviderType.Outlook;

    /// <inheritdoc />
    public string DisplayName => "Microsoft Outlook";

    /// <inheritdoc />
    public async Task<CalendarAccount> ConnectAccountAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement full OAuth 2.0 authorization code flow with MSAL.
        // 1. Build authorize URL with Graph scopes (Calendars.Read, User.Read).
        // 2. Use _authBroker to acquire authorization code.
        // 3. Exchange code for tokens.
        // 4. Store tokens via _credentialStore.
        // 5. Fetch user profile to build CalendarAccount.
        throw new NotImplementedException("Outlook OAuth flow not yet implemented.");
    }

    /// <inheritdoc />
    public ICalendarAccountClient CreateClient(CalendarAccount account)
    {
        return new OutlookCalendarAccountClient(account, _credentialStore);
    }
}
