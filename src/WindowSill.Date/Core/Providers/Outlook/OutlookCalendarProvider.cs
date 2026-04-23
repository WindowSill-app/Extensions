using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using WindowSill.API;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Views;

namespace WindowSill.Date.Core.Providers.Outlook;

/// <summary>
/// Calendar provider for Microsoft Outlook using Microsoft Graph API and MSAL.
/// Uses Windows Account Manager (WAM) broker for native sign-in experience.
/// Creates one MSAL client per account with isolated token caches.
/// </summary>
internal sealed class OutlookCalendarProvider : ICalendarProvider
{
    internal const string Authority = "https://login.microsoftonline.com/common";
    internal const string MsalCacheKey = "msal_cache";

    internal static readonly string[] Scopes = ["Calendars.Read", "Calendars.ReadWrite", "User.Read"];

    /// <inheritdoc />
    public CalendarProviderType ProviderType => CalendarProviderType.Outlook;

    /// <inheritdoc />
    public string DisplayName => "Microsoft Outlook";

    /// <inheritdoc />
    public string IconAssetFileName => "outlook.png";

    /// <inheritdoc />
    public ConnectExperience CreateConnectExperience()
    {
        return new OutlookConnectExperience();
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
            .Create(OAuthSecrets.OutlookClientId)
            .WithAuthority(Authority)
            .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
            .WithDefaultRedirectUri()
            .Build();
    }

    /// <summary>
    /// Connect experience for Outlook that uses WAM broker for authentication.
    /// </summary>
    private sealed class OutlookConnectExperience : ConnectExperience
    {
        private readonly OAuthConnectContent _content
            = new("/WindowSill.Date/Settings/OutlookConnectMessage".GetLocalizedString());

        /// <inheritdoc />
        public override FrameworkElement Content => _content;

        /// <inheritdoc />
        public override async Task<CalendarAccount> ConnectAsync(IntPtr parentWindowHandle, CancellationToken cancellationToken)
        {
            AuthenticationResult authResult;
            byte[]? capturedCache = null;

            // Try WAM broker first for native Windows sign-in experience.
            IPublicClientApplication brokerClient = BuildMsalClient();
            brokerClient.UserTokenCache.SetAfterAccess(args =>
            {
                if (args.HasStateChanged)
                {
                    capturedCache = args.TokenCache.SerializeMsalV3();
                }
            });

            authResult = await brokerClient
                .AcquireTokenInteractive(Scopes)
                .WithPrompt(Prompt.SelectAccount)
                .WithParentActivityOrWindow(parentWindowHandle)
                .ExecuteAsync(cancellationToken);

            string email = authResult.Account.Username ?? "unknown@outlook.com";
            string displayName
                = authResult.Account.GetTenantProfiles().FirstOrDefault()?.ClaimsPrincipal.Claims.FirstOrDefault(c => c.Type == "name")?.Value
                ?? "Outlook";

            var authData = new Dictionary<string, string>();
            if (capturedCache is { Length: > 0 })
            {
                authData[MsalCacheKey] = Convert.ToBase64String(capturedCache);
            }

            return new CalendarAccount
            {
                Id = $"outlook_{email}",
                DisplayName = displayName,
                Email = email,
                ProviderType = CalendarProviderType.Outlook,
                AuthData = authData,
            };
        }
    }
}
