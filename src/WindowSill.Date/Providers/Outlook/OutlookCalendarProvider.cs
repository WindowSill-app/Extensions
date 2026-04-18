using System.ComponentModel.Composition;
using Microsoft.Identity.Client;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Providers.Outlook;

/// <summary>
/// Calendar provider for Microsoft Outlook using the Microsoft Graph API
/// and MSAL for authentication. Supports multiple concurrent accounts.
/// </summary>
internal sealed class OutlookCalendarProvider : ICalendarProvider
{
    // Multi-tenant app registration for calendar access.
    // Client ID is safe to include — it's not confidential per Microsoft documentation.
    internal const string ClientId = "3fb5c650-a9f3-41a2-a691-2bedd61980a8";
    internal const string Authority = "https://login.microsoftonline.com/common";

    internal static readonly string[] Scopes = ["Calendars.Read", "Calendars.ReadWrite", "User.Read"];

    private readonly CalendarDataStore _dataStore;
    private IPublicClientApplication? _msalClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookCalendarProvider"/> class.
    /// </summary>
    /// <param name="dataStore">The data store for persisting the MSAL token cache.</param>
    internal OutlookCalendarProvider(CalendarDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    /// <inheritdoc />
    public CalendarProviderType ProviderType => CalendarProviderType.Outlook;

    /// <inheritdoc />
    public string DisplayName => "Microsoft Outlook";

    /// <inheritdoc />
    public async Task<CalendarAccount> ConnectAccountAsync(CancellationToken cancellationToken)
    {
        IPublicClientApplication msalClient = GetOrCreateMsalClient();

        AuthenticationResult authResult
            = await msalClient
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

    private async Task OnBeforeTokenCacheAccessAsync(TokenCacheNotificationArgs args)
    {
        byte[] cacheData = await _dataStore.LoadProviderCacheAsync(CalendarProviderType.Outlook);
        if (cacheData.Length > 0)
        {
            args.TokenCache.DeserializeMsalV3(cacheData);
        }
    }

    private async Task OnAfterTokenCacheAccessAsync(TokenCacheNotificationArgs args)
    {
        if (args.HasStateChanged)
        {
            byte[] cacheData = args.TokenCache.SerializeMsalV3();
            await _dataStore.SaveProviderCacheAsync(CalendarProviderType.Outlook, cacheData);
        }
    }
}
