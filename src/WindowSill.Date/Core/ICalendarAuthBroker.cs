namespace WindowSill.Date.Core;

/// <summary>
/// Handles OAuth and other interactive authentication flows for calendar providers.
/// Implementations are responsible for launching browser windows, handling redirects,
/// and returning authorization codes or tokens.
/// </summary>
public interface ICalendarAuthBroker
{
    /// <summary>
    /// Initiates an interactive OAuth authorization flow.
    /// </summary>
    /// <param name="authorizeUrl">The authorization endpoint URL.</param>
    /// <param name="redirectUri">The redirect URI to capture the authorization response.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The authorization code received from the provider.</returns>
    Task<string> AcquireAuthorizationCodeAsync(
        Uri authorizeUrl,
        Uri redirectUri,
        CancellationToken cancellationToken = default);
}
