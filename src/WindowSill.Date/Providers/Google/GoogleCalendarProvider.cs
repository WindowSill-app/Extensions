using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Providers.Google;

/// <summary>
/// Calendar provider for Google Calendar using the Google Calendar API.
/// </summary>
internal sealed class GoogleCalendarProvider : ICalendarProvider
{
    private readonly ICalendarCredentialStore _credentialStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCalendarProvider"/> class.
    /// </summary>
    /// <param name="credentialStore">The credential store for persisting tokens.</param>
    internal GoogleCalendarProvider(ICalendarCredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    /// <inheritdoc />
    public CalendarProviderType ProviderType => CalendarProviderType.Google;

    /// <inheritdoc />
    public string DisplayName => "Google Calendar";

    /// <inheritdoc />
    public async Task<CalendarAccount> ConnectAccountAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement Google OAuth 2.0 flow.
        // 1. Build authorize URL with Google Calendar scopes.
        // 2. Use _authBroker to acquire authorization code.
        // 3. Exchange code for tokens via Google token endpoint.
        // 4. Store tokens via _credentialStore.
        // 5. Fetch user info to build CalendarAccount.
        throw new NotImplementedException("Google OAuth flow not yet implemented.");
    }

    /// <inheritdoc />
    public ICalendarAccountClient CreateClient(CalendarAccount account)
    {
        return new GoogleCalendarAccountClient(account, _credentialStore);
    }
}
