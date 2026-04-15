using System.ComponentModel.Composition;

using Microsoft.Identity.Client;

namespace WindowSill.CalendarPlus.Core.Auth;

/// <summary>
/// Handles Microsoft account authentication using MSAL with WAM broker.
/// Supports both personal and work/school accounts.
/// </summary>
[Export(typeof(MicrosoftAuthHelper))]
internal sealed class MicrosoftAuthHelper
{
    // Multi-tenant app registration for calendar access.
    // In production this would be a registered Azure AD app.
    private const string ClientId = "YOUR_CLIENT_ID_HERE";
    private const string Authority = "https://login.microsoftonline.com/common";

    private static readonly string[] Scopes = ["Calendars.Read", "Calendars.ReadWrite", "User.Read"];

    private IPublicClientApplication? _msalClient;
    private AuthenticationResult? _authResult;

    /// <summary>
    /// Gets a value indicating whether the user is currently authenticated with a valid token.
    /// </summary>
    public bool IsAuthenticated => _authResult is not null && _authResult.ExpiresOn > DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the authenticated account email, if available.
    /// </summary>
    public string? AccountEmail => _authResult?.Account?.Username;

    /// <summary>
    /// Gets the authenticated account display name, if available.
    /// </summary>
    public string? AccountDisplayName => _authResult?.Account?.Username;

    private IPublicClientApplication GetOrCreateClient()
    {
        if (_msalClient is null)
        {
            _msalClient = PublicClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(Authority)
                .WithDefaultRedirectUri()
                .Build();
        }

        return _msalClient;
    }

    /// <summary>
    /// Attempts silent authentication first, then interactive if needed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if authentication succeeded.</returns>
    public async Task<bool> AuthenticateAsync(CancellationToken cancellationToken)
    {
        try
        {
            IPublicClientApplication client = GetOrCreateClient();

            // Try silent auth first
            IEnumerable<IAccount> accounts = await client.GetAccountsAsync();
            IAccount? account = accounts.FirstOrDefault();

            if (account is not null)
            {
                try
                {
                    _authResult = await client.AcquireTokenSilent(Scopes, account)
                        .ExecuteAsync(cancellationToken);
                    return true;
                }
                catch (MsalUiRequiredException)
                {
                    // Fall through to interactive
                }
            }

            // Interactive auth
            _authResult = await client.AcquireTokenInteractive(Scopes)
                .ExecuteAsync(cancellationToken);
            return _authResult is not null;
        }
        catch (Exception)
        {
            _authResult = null;
            return false;
        }
    }

    /// <summary>
    /// Gets a valid access token, refreshing silently if needed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The access token, or null if unavailable.</returns>
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_authResult is null)
        {
            return null;
        }

        IPublicClientApplication client = GetOrCreateClient();
        try
        {
            _authResult = await client.AcquireTokenSilent(Scopes, _authResult.Account)
                .ExecuteAsync(cancellationToken);
            return _authResult.AccessToken;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Signs out from all Microsoft accounts.
    /// </summary>
    public async Task SignOutAsync()
    {
        if (_msalClient is not null)
        {
            IEnumerable<IAccount> accounts = await _msalClient.GetAccountsAsync();
            foreach (IAccount account in accounts)
            {
                await _msalClient.RemoveAsync(account);
            }
        }

        _authResult = null;
    }
}
