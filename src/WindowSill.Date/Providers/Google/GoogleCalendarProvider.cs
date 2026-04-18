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
    public async Task<CalendarAccount> ConnectAccountAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement Google OAuth 2.0 flow.
        throw new NotImplementedException("Google OAuth flow not yet implemented.");
    }

    /// <inheritdoc />
    public ICalendarAccountClient CreateClient(
        CalendarAccount account,
        Func<IReadOnlyDictionary<string, string>, CancellationToken, Task> onAuthDataChanged)
    {
        return new GoogleCalendarAccountClient(account, account.AuthData, onAuthDataChanged);
    }
}
