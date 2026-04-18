using System.ComponentModel.Composition;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
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

    /// <inheritdoc />
    public CalendarProviderType ProviderType => CalendarProviderType.Outlook;

    /// <inheritdoc />
    public string DisplayName => "Microsoft Outlook";

    /// <inheritdoc />
    public async Task<CalendarAccount> ConnectAccountAsync(CancellationToken cancellationToken)
    {
        IPublicClientApplication msalClient = CreateMsalClient();

        // Try silent auth first (cached tokens).
        AuthenticationResult authResult;
        try
        {
            IEnumerable<IAccount> accounts = await msalClient.GetAccountsAsync();
            IAccount? existingAccount = accounts.FirstOrDefault();
            if (existingAccount is not null)
            {
                authResult = await msalClient.AcquireTokenSilent(Scopes, existingAccount)
                    .ExecuteAsync(cancellationToken);
            }
            else
            {
                authResult = await msalClient.AcquireTokenInteractive(Scopes)
                    .ExecuteAsync(cancellationToken);
            }
        }
        catch (MsalUiRequiredException)
        {
            // Silent failed — must go interactive.
            authResult = await msalClient.AcquireTokenInteractive(Scopes)
                .ExecuteAsync(cancellationToken);
        }

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
        return new OutlookCalendarAccountClient(account, CreateMsalClient());
    }

    internal static IPublicClientApplication CreateMsalClient()
    {
        return PublicClientApplicationBuilder
            .Create(ClientId)
            .WithAuthority(Authority)
            .WithRedirectUri("http://localhost")
            .Build();
    }
}
