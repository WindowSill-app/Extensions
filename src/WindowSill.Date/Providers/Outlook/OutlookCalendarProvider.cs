using Microsoft.Identity.Client;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Providers.Outlook;

/// <summary>
/// Calendar provider for Microsoft Outlook using Microsoft Graph API and MSAL.
/// Creates one MSAL client per account with isolated token caches.
/// </summary>
internal sealed class OutlookCalendarProvider : ICalendarProvider
{
    internal const string ClientId = "3fb5c650-a9f3-41a2-a691-2bedd61980a8";
    internal const string Authority = "https://login.microsoftonline.com/common";
    internal const string MsalCacheKey = "msal_cache";

    internal static readonly string[] Scopes = ["Calendars.Read", "Calendars.ReadWrite", "User.Read"];

    /// <inheritdoc />
    public CalendarProviderType ProviderType => CalendarProviderType.Outlook;

    /// <inheritdoc />
    public string DisplayName => "Microsoft Outlook";

    /// <inheritdoc />
    public async Task<CalendarAccount> ConnectAccountAsync(CancellationToken cancellationToken)
    {
        IPublicClientApplication msalClient = BuildMsalClient();

        byte[]? capturedCache = null;
        msalClient.UserTokenCache.SetAfterAccess(args =>
        {
            if (args.HasStateChanged)
            {
                capturedCache = args.TokenCache.SerializeMsalV3();
            }
        });

        AuthenticationResult authResult = await msalClient
            .AcquireTokenInteractive(Scopes)
            .WithPrompt(Prompt.SelectAccount)
            .ExecuteAsync(cancellationToken);

        string email = authResult.Account.Username ?? "unknown@outlook.com";

        var authData = new Dictionary<string, string>();
        if (capturedCache is { Length: > 0 })
        {
            authData[MsalCacheKey] = Convert.ToBase64String(capturedCache);
        }

        return new CalendarAccount
        {
            Id = $"outlook_{email}",
            DisplayName = authResult.Account.Username ?? "Outlook Account",
            Email = email,
            ProviderType = CalendarProviderType.Outlook,
            AuthData = authData,
        };
    }

    /// <inheritdoc />
    public ICalendarAccountClient CreateClient(
        CalendarAccount account,
        Func<IReadOnlyDictionary<string, string>, CancellationToken, Task> onAuthDataChanged)
    {
        IPublicClientApplication msalClient = BuildMsalClient();

        if (account.AuthData.TryGetValue(MsalCacheKey, out string? cached) && !string.IsNullOrEmpty(cached))
        {
            byte[] cacheData = Convert.FromBase64String(cached);
            msalClient.UserTokenCache.SetBeforeAccess(args =>
            {
                args.TokenCache.DeserializeMsalV3(cacheData);
            });
        }

        msalClient.UserTokenCache.SetAfterAccessAsync(async args =>
        {
            if (args.HasStateChanged)
            {
                byte[] updatedCache = args.TokenCache.SerializeMsalV3();
                var updatedAuthData = new Dictionary<string, string>
                {
                    [MsalCacheKey] = Convert.ToBase64String(updatedCache),
                };
                await onAuthDataChanged(updatedAuthData, CancellationToken.None);
            }
        });

        return new OutlookCalendarAccountClient(account, msalClient);
    }

    private static IPublicClientApplication BuildMsalClient()
    {
        return PublicClientApplicationBuilder
            .Create(ClientId)
            .WithAuthority(Authority)
            .WithRedirectUri("http://localhost")
            .Build();
    }
}
