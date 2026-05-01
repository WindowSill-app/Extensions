using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WindowSill.API;
using Path = System.IO.Path;

namespace WindowSill.URLHelper.Core;

/// <summary>
/// Detects web browsers installed on the system by reading the Windows registry.
/// </summary>
internal static class BrowserDetector
{
    private static readonly ILogger _logger = typeof(BrowserDetector).Log();

    /// <summary>
    /// Returns a list of installed web browsers detected from the registry.
    /// </summary>
    internal static IReadOnlyList<BrowserInfo> GetInstalledBrowsers()
    {
        var browsers = new Dictionary<string, BrowserInfo>(StringComparer.OrdinalIgnoreCase);

        // Primary source: HKLM\SOFTWARE\Clients\StartMenuInternet
        ScanStartMenuInternet(Registry.LocalMachine, browsers);

        // Secondary source: HKCU (per-user installs)
        ScanStartMenuInternet(Registry.CurrentUser, browsers);

        // Fallback: well-known paths for browsers that may not register under StartMenuInternet
        AddWellKnownBrowsers(browsers);

        return [.. browsers.Values.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase)];
    }

    private static void ScanStartMenuInternet(RegistryKey hive, Dictionary<string, BrowserInfo> browsers)
    {
        try
        {
            using RegistryKey? clientsKey = hive.OpenSubKey(@"SOFTWARE\Clients\StartMenuInternet");
            if (clientsKey is null)
            {
                return;
            }

            foreach (string subKeyName in clientsKey.GetSubKeyNames())
            {
                try
                {
                    using RegistryKey? browserKey = clientsKey.OpenSubKey(subKeyName);
                    if (browserKey is null)
                    {
                        continue;
                    }

                    string? displayName = browserKey.GetValue(null) as string;

                    using RegistryKey? commandKey = browserKey.OpenSubKey(@"shell\open\command");
                    string? commandLine = commandKey?.GetValue(null) as string;

                    if (string.IsNullOrWhiteSpace(commandLine))
                    {
                        continue;
                    }

                    string exePath = ExtractExePath(commandLine);
                    if (!File.Exists(exePath))
                    {
                        continue;
                    }

                    string name = !string.IsNullOrWhiteSpace(displayName) ? displayName : Path.GetFileNameWithoutExtension(exePath);

                    // Deduplicate by executable path (case-insensitive).
                    if (!browsers.ContainsKey(exePath))
                    {
                        (string? flag, string? displayNamePrivate) = BrowserMatcher.GetPrivateModeInfo(exePath);
                        browsers[exePath] = new BrowserInfo(name, exePath, flag, displayNamePrivate);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to read browser entry '{SubKey}'.", subKeyName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to scan StartMenuInternet under {Hive}.", hive.Name);
        }
    }

    private static void AddWellKnownBrowsers(Dictionary<string, BrowserInfo> browsers)
    {
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        ReadOnlySpan<(string Name, string[] RelativePaths)> wellKnown =
        [
            ("Microsoft Edge", [
                Path.Combine(programFilesX86, @"Microsoft\Edge\Application\msedge.exe"),
                Path.Combine(programFiles, @"Microsoft\Edge\Application\msedge.exe"),
            ]),
            ("Google Chrome", [
                Path.Combine(programFiles, @"Google\Chrome\Application\chrome.exe"),
                Path.Combine(programFilesX86, @"Google\Chrome\Application\chrome.exe"),
                Path.Combine(localAppData, @"Google\Chrome\Application\chrome.exe"),
            ]),
            ("Mozilla Firefox", [
                Path.Combine(programFiles, @"Mozilla Firefox\firefox.exe"),
                Path.Combine(programFilesX86, @"Mozilla Firefox\firefox.exe"),
            ]),
            ("Brave", [
                Path.Combine(programFiles, @"BraveSoftware\Brave-Browser\Application\brave.exe"),
                Path.Combine(programFilesX86, @"BraveSoftware\Brave-Browser\Application\brave.exe"),
                Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\Application\brave.exe"),
            ]),
            ("Opera", [
                Path.Combine(programFiles, @"Opera\opera.exe"),
                Path.Combine(programFilesX86, @"Opera\opera.exe"),
                Path.Combine(localAppData, @"Programs\Opera\opera.exe"),
            ]),
            ("Opera GX", [
                Path.Combine(programFiles, @"Opera GX\opera.exe"),
                Path.Combine(programFilesX86, @"Opera GX\opera.exe"),
                Path.Combine(localAppData, @"Programs\Opera GX\opera.exe"),
            ]),
            ("Vivaldi", [
                Path.Combine(localAppData, @"Vivaldi\Application\vivaldi.exe"),
                Path.Combine(programFiles, @"Vivaldi\Application\vivaldi.exe"),
            ]),
            ("Arc", [
                Path.Combine(localAppData, @"Arc\Application\arc.exe"),
            ]),
            ("Waterfox", [
                Path.Combine(programFiles, @"Waterfox\waterfox.exe"),
                Path.Combine(programFilesX86, @"Waterfox\waterfox.exe"),
            ]),
            ("Tor Browser", [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), @"Tor Browser\Browser\firefox.exe"),
            ]),
            ("Chromium", [
                Path.Combine(localAppData, @"Chromium\Application\chrome.exe"),
            ]),
        ];

        foreach ((string name, string[] paths) in wellKnown)
        {
            foreach (string path in paths)
            {
                if (!browsers.ContainsKey(path) && File.Exists(path))
                {
                    (string? flag, string? displayNamePrivate) = BrowserMatcher.GetPrivateModeInfo(path);
                    browsers[path] = new BrowserInfo(name, path, flag, displayNamePrivate);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Extracts the executable path from a registry command-line value,
    /// stripping surrounding quotes and trailing arguments.
    /// </summary>
    internal static string ExtractExePath(string commandLine)
    {
        commandLine = commandLine.Trim();

        if (commandLine.StartsWith('"'))
        {
            int closingQuote = commandLine.IndexOf('"', 1);
            return closingQuote > 1
                ? commandLine[1..closingQuote]
                : commandLine.Trim('"');
        }

        // No quotes — take everything up to the first space that precedes a dash argument.
        int spaceIndex = commandLine.IndexOf(' ');
        return spaceIndex > 0
            ? commandLine[..spaceIndex]
            : commandLine;
    }
}
