using WindowSill.API;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Views;

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
    public virtual string IconAssetFileName => "package.svg";

    /// <inheritdoc />
    public virtual ConnectExperience CreateConnectExperience()
    {
        return new CalDavConnectExperience();
    }

    /// <inheritdoc />
    public virtual ICalendarAccountClient CreateClient(
        CalendarAccount account,
        Func<IReadOnlyDictionary<string, string>, CancellationToken, Task> onAuthDataChanged)
    {
        return new CalDavCalendarAccountClient(account, account.AuthData);
    }

    /// <summary>
    /// Connect experience for CalDAV that collects server URL, username, and password.
    /// </summary>
    private sealed class CalDavConnectExperience : ConnectExperience
    {
        private readonly CalDavConnectContent _content = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="CalDavConnectExperience"/> class.
        /// </summary>
        public CalDavConnectExperience()
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
        public override Task<CalendarAccount> ConnectAsync(IntPtr parentWindowHandle, CancellationToken cancellationToken)
        {
            // TODO: Validate connection to the CalDAV server and retrieve account info.
            string serverUrl = _content.ServerUrl;
            string username = _content.Username;
            string password = _content.Password;

            var authData = new Dictionary<string, string>
            {
                ["server_url"] = serverUrl,
                ["username"] = username,
                ["password"] = password,
            };

            var account = new CalendarAccount
            {
                Id = $"caldav_{username}@{new Uri(serverUrl).Host}",
                DisplayName = $"CalDAV ({username})",
                Email = username,
                ProviderType = CalendarProviderType.CalDav,
                AuthData = authData,
            };

            return Task.FromResult(account);
        }
    }
}
