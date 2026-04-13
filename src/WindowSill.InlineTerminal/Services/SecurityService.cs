using WindowSill.API;

namespace WindowSill.InlineTerminal.Services;

/// <summary>
/// Determines whether security warnings should be shown for commands from specific sources.
/// </summary>
internal static class SecurityService
{
    private static readonly string[] browserAppIdentifiers =
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

        return IsBrowserApplication(source);
    }

    /// <summary>
    /// Checks whether the source application is a known web browser.
    /// </summary>
    internal static bool IsBrowserApplication(WindowTextSelection? source)
    {
        string? appIdentifier = source?.ApplicationIdentifier;
        if (string.IsNullOrEmpty(appIdentifier))
        {
            return false;
        }

        for (int i = 0; i < browserAppIdentifiers.Length; i++)
        {
            if (appIdentifier.EndsWith(browserAppIdentifiers[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
