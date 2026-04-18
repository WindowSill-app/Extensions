using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Providers.Google;

/// <summary>
/// Calendar provider for Google Calendar using the Google Calendar API.
/// </summary>
internal sealed class GoogleCalendarProvider : ICalendarProvider
{
    /// <inheritdoc />
    public CalendarProviderType ProviderType => CalendarProviderType.Google;

    /// <inheritdoc />
    public string DisplayName => "Google Calendar";

    /// <inheritdoc />
    public async Task<(CalendarAccount Account, Dictionary<string, string> AuthData)> ConnectAccountAsync(
        CancellationToken cancellationToken)
    {
        // TODO: Implement Google OAuth 2.0 flow.
        // 1. Build authorize URL with Google Calendar scopes.
        // 2. Open browser for user sign-in.
        // 3. Exchange code for tokens.
        // 4. Return account + authData with access_token, refresh_token.
        throw new NotImplementedException("Google OAuth flow not yet implemented.");
    }

    /// <inheritdoc />
    public ICalendarAccountClient CreateClient(
        CalendarAccount account,
        IReadOnlyDictionary<string, string> authData,
        Func<IReadOnlyDictionary<string, string>, CancellationToken, Task> onAuthDataChanged)
    {
        return new GoogleCalendarAccountClient(account, authData, onAuthDataChanged);
    }
}
