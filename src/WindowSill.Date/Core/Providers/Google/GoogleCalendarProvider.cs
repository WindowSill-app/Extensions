using WindowSill.API;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Views;

namespace WindowSill.Date.Core.Providers.Google;

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
    public string IconAssetFileName => "google-calendar.png";

    /// <inheritdoc />
    public ConnectExperience CreateConnectExperience()
    {
        return new GoogleConnectExperience();
    }

    /// <inheritdoc />
    public ICalendarAccountClient CreateClient(
        CalendarAccount account,
        Func<IReadOnlyDictionary<string, string>, CancellationToken, Task> onAuthDataChanged)
    {
        return new GoogleCalendarAccountClient(account, account.AuthData, onAuthDataChanged);
    }

    /// <summary>
    /// Connect experience for Google Calendar using browser-based OAuth.
    /// </summary>
    private sealed class GoogleConnectExperience : ConnectExperience
    {
        private readonly OAuthConnectContent _content
            = new("/WindowSill.Date/Settings/GoogleConnectMessage".GetLocalizedString());

        /// <inheritdoc />
        public override FrameworkElement Content => _content;

        /// <inheritdoc />
        public override Task<CalendarAccount> ConnectAsync(IntPtr parentWindowHandle, CancellationToken cancellationToken)
        {
            // TODO: Implement Google OAuth 2.0 flow.
            throw new NotImplementedException("Google OAuth flow not yet implemented.");
        }
    }
}
