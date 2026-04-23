using WindowSill.API;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Providers.CalDav;
using WindowSill.Date.Views;

namespace WindowSill.Date.Core.Providers.ICloud;

/// <summary>
/// Calendar provider for Apple iCloud Calendar, extending the CalDAV provider
/// with Apple-specific server endpoints and authentication.
/// </summary>
internal sealed class ICloudCalendarProvider : CalDavCalendarProvider
{
    private const string ICloudCalDavServer = "https://caldav.icloud.com";

    /// <inheritdoc />
    public override CalendarProviderType ProviderType => CalendarProviderType.ICloud;

    /// <inheritdoc />
    public override string DisplayName => "Apple iCloud";

    /// <inheritdoc />
    public override string IconAssetFileName => "icloud.png";

    /// <inheritdoc />
    public override ConnectExperience CreateConnectExperience()
    {
        return new ICloudConnectExperience();
    }

    /// <inheritdoc />
    public override ICalendarAccountClient CreateClient(
        CalendarAccount account,
        Func<IReadOnlyDictionary<string, string>, CancellationToken, Task> onAuthDataChanged)
    {
        return new ICloudCalendarAccountClient(account, account.AuthData);
    }

    /// <summary>
    /// Connect experience for iCloud that collects Apple ID and app-specific password.
    /// </summary>
    private sealed class ICloudConnectExperience : ConnectExperience
    {
        private readonly ICloudConnectContent _content = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ICloudConnectExperience"/> class.
        /// </summary>
        public ICloudConnectExperience()
        {
            _content.FormValidityChanged += (_, _) => OnCanSubmitChanged();
        }

        /// <inheritdoc />
        public override FrameworkElement Content => _content;

        /// <inheritdoc />
        public override string? PrimaryButtonText
            => "/WindowSill.Date/Settings/SignIn".GetLocalizedString();

        /// <inheritdoc />
        public override bool CanSubmit => _content.IsValid;

        /// <inheritdoc />
        public override async Task<CalendarAccount> ConnectAsync(IntPtr parentWindowHandle, CancellationToken cancellationToken)
        {
            string appleId = _content.AppleId;
            string appPassword = _content.AppPassword;

            try
            {
                // Validate credentials by discovering the calendar home URL.
                string calendarHome = await ICloudCalendarAccountClient.ValidateAndDiscoverAsync(
                    appleId, appPassword, cancellationToken);

                var authData = new Dictionary<string, string>
                {
                    ["server_url"] = ICloudCalDavServer,
                    ["username"] = appleId,
                    ["password"] = appPassword,
                    ["calendar_home"] = calendarHome,
                };

                return new CalendarAccount
                {
                    Id = $"icloud_{appleId}",
                    DisplayName = $"iCloud ({appleId})",
                    Email = appleId,
                    ProviderType = CalendarProviderType.ICloud,
                    AuthData = authData,
                };
            }
            catch (HttpRequestException)
            {
                _content.ShowError("/WindowSill.Date/Settings/ICloudAuthFailed".GetLocalizedString());
                throw;
            }
            catch (InvalidOperationException)
            {
                _content.ShowError("/WindowSill.Date/Settings/ICloudDiscoveryFailed".GetLocalizedString());
                throw;
            }
        }
    }
}
