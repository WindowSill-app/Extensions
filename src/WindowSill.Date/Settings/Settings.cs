using WindowSill.API;

namespace WindowSill.Date.Settings;

/// <summary>
/// Setting definitions for the Date extension, persisted via <see cref="ISettingsProvider"/>.
/// </summary>
internal static class Settings
{
    /// <summary>
    /// The list of connected calendar accounts to restore on app restart.
    /// </summary>
    internal static readonly SettingDefinition<AccountRecord[]> ConnectedAccounts
        = new([], typeof(Settings).Assembly);

    /// <summary>
    /// Serialized MSAL token cache for Outlook accounts.
    /// Stored as a base64-encoded string to survive JSON serialization.
    /// </summary>
    internal static readonly SettingDefinition<string> OutlookTokenCache
        = new(string.Empty, typeof(Settings).Assembly);
}
