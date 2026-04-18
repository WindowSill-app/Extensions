using Microsoft.Identity.Client;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Providers.Outlook;

/// <summary>
/// Calendar provider for Microsoft Outlook using the Microsoft Graph API
/// and MSAL for authentication. Creates one MSAL client per account so each
/// account gets its own isolated token cache file.
/// </summary>
internal sealed class OutlookCalendarProvider : ICalendarProvider
{
    // Multi-tenant app registration for calendar access.
    // Client ID is safe to include — it's not confidential per Microsoft documentation.
    internal const string ClientId = "3fb5c650-a9f3-41a2-a691-2bedd61980a8";
    internal const string Authority = "https://login.microsoftonline.com/common";

    internal static readonly string[] Scopes = ["Calendars.Read", "Calendars.ReadWrite", "User.Read"];

    private readonly CalendarDataStore _dataStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookCalendarProvider"/> class.
    /// </summary>
    /// <param name="dataStore">The data store for persisting per-account token caches.</param>
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
        // Use a temporary MSAL client for the interactive flow.
        IPublicClientApplication tempClient = BuildMsalClient();

        // Hook a one-time cache capture so we can persist after auth.
        byte[]? capturedCache = null;
        tempClient.UserTokenCache.SetAfterAccess(args =>
        {
            if (args.HasStateChanged)
            {
                capturedCache = args.TokenCache.SerializeMsalV3();
            }
        });

        AuthenticationResult authResult = await tempClient
            .AcquireTokenInteractive(Scopes)
            .WithPrompt(Prompt.SelectAccount)
            .ExecuteAsync(cancellationToken);

        string email = authResult.Account.Username ?? "unknown@outlook.com";
        string accountId = $"outlook_{email}";

        // Persist the captured token cache into the account's file.
        if (capturedCache is { Length: > 0 })
        {
            await _dataStore.SaveAccountCacheAsync(accountId, capturedCache, cancellationToken);
        }

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
        IPublicClientApplication msalClient = BuildMsalClientForAccount(account.Id);
        return new OutlookCalendarAccountClient(account, msalClient);
    }

    /// <summary>
    /// Builds a fresh MSAL client (no cache hooks).
    /// </summary>
    private static IPublicClientApplication BuildMsalClient()
    {
        return PublicClientApplicationBuilder
            .Create(ClientId)
            .WithAuthority(Authority)
            .WithRedirectUri("http://localhost")
            .Build();
    }

    /// <summary>
    /// Builds an MSAL client for a specific account, with token cache
    /// hooks that persist to the account's encrypted file.
    /// </summary>
    private IPublicClientApplication BuildMsalClientForAccount(string accountId)
    {
        IPublicClientApplication client = BuildMsalClient();

        client.UserTokenCache.SetBeforeAccessAsync(async args =>
        {
            byte[] cacheData = await _dataStore.LoadAccountCacheAsync(accountId);
            if (cacheData.Length > 0)
            {
                args.TokenCache.DeserializeMsalV3(cacheData);
            }
        });

        client.UserTokenCache.SetAfterAccessAsync(async args =>
        {
            if (args.HasStateChanged)
            {
                byte[] cacheData = args.TokenCache.SerializeMsalV3();
                await _dataStore.SaveAccountCacheAsync(accountId, cacheData);
            }
        });

        return client;
    }
}
