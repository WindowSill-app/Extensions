namespace WindowSill.URLHelper.Core;

/// <summary>
/// Pure helper methods for matching application identifiers to known browsers.
/// Separated from <see cref="OpenInBrowserSillItem"/> for unit testability
/// without requiring the WindowSill logging infrastructure.
/// </summary>
internal static class BrowserMatcher
{
    /// <summary>
    /// Known browser executable names used to match <see cref="WindowSill.API.WindowInfo.ApplicationIdentifier"/>.
    /// </summary>
    private static readonly string[] KnownBrowserExeNames =
    [
        "msedge.exe",
        "chrome.exe",
        "firefox.exe",
        "brave.exe",
        "opera.exe",
        "vivaldi.exe",
        "zen.exe",
        "arc.exe",
        "waterfox.exe",
        "chromium.exe",
    ];

    /// <summary>
    /// Checks whether the given application identifier matches a known web browser executable.
    /// </summary>
    internal static bool IsKnownBrowser(string? appIdentifier)
    {
        if (string.IsNullOrEmpty(appIdentifier))
        {
            return false;
        }

        for (int i = 0; i < KnownBrowserExeNames.Length; i++)
        {
            if (appIdentifier.EndsWith(KnownBrowserExeNames[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether a detected browser's exe path matches the source application identifier.
    /// </summary>
    internal static bool IsMatchingBrowser(string browserExePath, string? appIdentifier)
    {
        if (string.IsNullOrEmpty(appIdentifier))
        {
            return false;
        }

        string browserExeName = System.IO.Path.GetFileName(browserExePath);
        return appIdentifier.EndsWith(browserExeName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the command-line flag and display name for private/incognito mode
    /// based on the browser executable name. Returns <c>(null, null)</c> if unsupported.
    /// </summary>
    internal static (string? Flag, string? DisplayName) GetPrivateModeInfo(string exePath)
    {
        string exeName = System.IO.Path.GetFileName(exePath);

        // Chromium-based browsers that use --incognito
        if (exeName.EndsWith("chrome.exe", StringComparison.OrdinalIgnoreCase)
            || exeName.EndsWith("brave.exe", StringComparison.OrdinalIgnoreCase)
            || exeName.EndsWith("vivaldi.exe", StringComparison.OrdinalIgnoreCase)
            || exeName.EndsWith("chromium.exe", StringComparison.OrdinalIgnoreCase)
            || exeName.EndsWith("arc.exe", StringComparison.OrdinalIgnoreCase))
        {
            return ("--incognito", "Incognito");
        }

        // Edge uses --inprivate
        if (exeName.EndsWith("msedge.exe", StringComparison.OrdinalIgnoreCase))
        {
            return ("--inprivate", "InPrivate");
        }

        // Firefox-based browsers
        if (exeName.EndsWith("firefox.exe", StringComparison.OrdinalIgnoreCase)
            || exeName.EndsWith("waterfox.exe", StringComparison.OrdinalIgnoreCase)
            || exeName.EndsWith("zen.exe", StringComparison.OrdinalIgnoreCase))
        {
            return ("-private-window", "Private Window");
        }

        // Opera
        if (exeName.EndsWith("opera.exe", StringComparison.OrdinalIgnoreCase))
        {
            return ("--private", "Private Window");
        }

        return (null, null);
    }
}
