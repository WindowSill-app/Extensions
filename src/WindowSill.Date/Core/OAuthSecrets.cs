namespace WindowSill.Date.Core;

/// <summary>
/// OAuth client credentials for calendar providers.
/// This file is generated at build time from environment variables (CI)
/// or from <c>OAuthSecrets.json</c> (local development).
/// <para>
/// <b>Setup for contributors:</b>
/// <list type="number">
///   <item>Copy <c>OAuthSecrets.template.json</c> to <c>OAuthSecrets.json</c></item>
///   <item>Fill in your own OAuth credentials from Google Cloud Console and Azure AD</item>
///   <item><c>OAuthSecrets.json</c> is gitignored and never committed</item>
/// </list>
/// </para>
/// <para>
/// <b>CI setup:</b> Set the environment variables <c>OUTLOOK_CLIENT_ID</c>,
/// <c>GOOGLE_CLIENT_ID</c>, and <c>GOOGLE_CLIENT_SECRET</c> in your CI pipeline.
/// </para>
/// </summary>
internal static class OAuthSecrets
{
    /// <summary>
    /// Microsoft Entra (Azure AD) application client ID for Outlook calendar access.
    /// </summary>
    internal const string OutlookClientId = OAuthSecretsValues.OutlookClientId;

    /// <summary>
    /// Google OAuth 2.0 client ID for a desktop application.
    /// </summary>
    internal const string GoogleClientId = OAuthSecretsValues.GoogleClientId;

    /// <summary>
    /// Google OAuth 2.0 client secret for a desktop application.
    /// </summary>
    internal const string GoogleClientSecret = OAuthSecretsValues.GoogleClientSecret;
}
