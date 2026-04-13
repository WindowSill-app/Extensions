using WindowSill.API;

namespace WindowSill.InlineTerminal.Services;

/// <summary>
/// Determines whether security warnings should be shown for commands from specific sources.
/// </summary>
internal static class SecurityService
{
    private static readonly string[] BrowserAppIdentifiers =
    [
        "msedge.exe",
        "chrome.exe",
        "firefox.exe",
        "brave.exe",
        "opera.exe",
        "vivaldi.exe",
        "zen.exe"
    ];

    /// <summary>
    /// Determines whether the ClickFix security warning should be shown.
    /// Returns true when the command originates from a web browser and the warning is not disabled.
    /// </summary>
    internal static bool ShouldShowClickFixWarning(
        WindowTextSelection? source,
        ISettingsProvider settingsProvider)
    {
        if (settingsProvider.GetSetting(Settings.Settings.DisableClickFixWarning))
        {
            return false;
        }

        return IsBrowserApplication(source?.ApplicationIdentifier);
    }

    /// <summary>
    /// Checks whether the source application is a known web browser.
    /// </summary>
    internal static bool IsBrowserApplication(WindowTextSelection? source)
    {
        return IsBrowserApplication(source?.ApplicationIdentifier);
    }

    /// <summary>
    /// Checks whether the given application identifier matches a known web browser.
    /// </summary>
    internal static bool IsBrowserApplication(string? appIdentifier)
    {
        if (string.IsNullOrEmpty(appIdentifier))
        {
            return false;
        }

        for (int i = 0; i < BrowserAppIdentifiers.Length; i++)
        {
            if (appIdentifier.EndsWith(BrowserAppIdentifiers[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
