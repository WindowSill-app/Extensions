using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Calendar.v3;
using WindowSill.API;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Views;

namespace WindowSill.Date.Core.Providers.Google;

/// <summary>
/// Calendar provider for Google Calendar using the Google Calendar API.
/// </summary>
internal sealed class GoogleCalendarProvider : ICalendarProvider
{
    internal static readonly string[] Scopes = [CalendarService.Scope.CalendarReadonly];

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
    /// Builds the Google OAuth authorization code flow shared by connect and client.
    /// </summary>
    internal static GoogleAuthorizationCodeFlow CreateFlow()
    {
        return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = OAuthSecrets.GoogleClientId,
                ClientSecret = OAuthSecrets.GoogleClientSecret,
            },
            Scopes = Scopes,
        });
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
        public override async Task<CalendarAccount> ConnectAsync(IntPtr parentWindowHandle, CancellationToken cancellationToken)
        {
            GoogleAuthorizationCodeFlow flow = CreateFlow();

            UserCredential credential = await new AuthorizationCodeInstalledApp(
                flow, new LocalServerCodeReceiver())
                .AuthorizeAsync("user", cancellationToken);

            // The credential's UserId is typically the email from the consent flow.
            string email = credential.UserId ?? "unknown@gmail.com";

            var authData = new Dictionary<string, string>
            {
                ["access_token"] = credential.Token.AccessToken,
                ["refresh_token"] = credential.Token.RefreshToken ?? string.Empty,
                ["token_expiry"] = credential.Token.ExpiresInSeconds?.ToString() ?? "3600",
                ["issued_utc"] = credential.Token.IssuedUtc.ToString("O"),
            };

            return new CalendarAccount
            {
                Id = $"google_{email}",
                DisplayName = $"Google ({email})",
                Email = email,
                ProviderType = CalendarProviderType.Google,
                AuthData = authData,
            };
        }
    }
}
