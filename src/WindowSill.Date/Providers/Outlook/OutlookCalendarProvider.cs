using System.ComponentModel.Composition;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using WindowSill.API;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Providers.Outlook;

/// <summary>
/// Calendar provider for Microsoft Outlook using the Microsoft Graph API
/// and MSAL for authentication. Supports multiple concurrent accounts.
/// </summary>
[Export(typeof(ICalendarProvider))]
internal sealed class OutlookCalendarProvider : ICalendarProvider
{
    // Multi-tenant app registration for calendar access.
    // Client ID is safe to include — it's not confidential per Microsoft documentation.
    internal const string ClientId = "3fb5c650-a9f3-41a2-a691-2bedd61980a8";
    internal const string Authority = "https://login.microsoftonline.com/common";

    internal static readonly string[] Scopes = ["Calendars.Read", "Calendars.ReadWrite", "User.Read"];

    private readonly ISettingsProvider _settingsProvider;
    private IPublicClientApplication? _msalClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookCalendarProvider"/> class.
    /// </summary>
    /// <param name="settingsProvider">The settings provider for persisting the MSAL token cache.</param>
    [ImportingConstructor]
    public OutlookCalendarProvider(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
    }

    /// <inheritdoc />
    public CalendarProviderType ProviderType => CalendarProviderType.Outlook;

    /// <inheritdoc />
    public string DisplayName => "Microsoft Outlook";

    /// <inheritdoc />
    public async Task<CalendarAccount> ConnectAccountAsync(CancellationToken cancellationToken)
    {
        IPublicClientApplication msalClient = GetOrCreateMsalClient();

        // Always go interactive with account picker so user can choose
        // which account to add (supports adding multiple accounts).
        AuthenticationResult authResult = await msalClient
            .AcquireTokenInteractive(Scopes)
            .WithPrompt(Prompt.SelectAccount)
            .ExecuteAsync(cancellationToken);

        string email = authResult.Account.Username ?? "unknown@outlook.com";
        string accountId = $"outlook_{email}";

        return new CalendarAccount
        {
            Id = accountId,
            DisplayName = authResult.Account.Username ?? "Outlook Account",
            Email = email,
            ProviderType = CalendarProviderType.Outlook,
        };
    }

    /// <inheritdoc />
    public ICalendarAccountClient CreateClient(CalendarAccount account)
    {
        return new OutlookCalendarAccountClient(account, GetOrCreateMsalClient());
    }

    /// <summary>
    /// Gets the shared MSAL client with token cache persistence.
    /// A single MSAL client manages all Outlook accounts; MSAL internally
    /// partitions token caches per account.
    /// </summary>
    internal IPublicClientApplication GetOrCreateMsalClient()
    {
        if (_msalClient is not null)
        {
            return _msalClient;
        }

        _msalClient = PublicClientApplicationBuilder
            .Create(ClientId)
            .WithAuthority(Authority)
            .WithRedirectUri("http://localhost")
            .Build();

        // Hook into MSAL's token cache to persist across app restarts.
        _msalClient.UserTokenCache.SetBeforeAccessAsync(OnBeforeTokenCacheAccessAsync);
        _msalClient.UserTokenCache.SetAfterAccessAsync(OnAfterTokenCacheAccessAsync);

        return _msalClient;
    }

    private Task OnBeforeTokenCacheAccessAsync(TokenCacheNotificationArgs args)
    {
        string serialized = _settingsProvider.GetSetting(Settings.Settings.OutlookTokenCache);
        if (!string.IsNullOrEmpty(serialized))
        {
            byte[] cacheData = Convert.FromBase64String(serialized);
            args.TokenCache.DeserializeMsalV3(cacheData);
        }

        return Task.CompletedTask;
    }

    private Task OnAfterTokenCacheAccessAsync(TokenCacheNotificationArgs args)
    {
        if (args.HasStateChanged)
        {
            byte[] cacheData = args.TokenCache.SerializeMsalV3();
            string serialized = Convert.ToBase64String(cacheData);
            _settingsProvider.SetSetting(Settings.Settings.OutlookTokenCache, serialized);
        }

        return Task.CompletedTask;
    }
}
