namespace WindowSill.URLHelper.Core;

/// <summary>
/// Represents a web browser installed on the system.
/// </summary>
/// <param name="Name">Display name of the browser.</param>
/// <param name="ExecutablePath">Full path to the browser executable.</param>
/// <param name="PrivateModeFlag">Command-line flag to launch in private/incognito mode, or <c>null</c> if unknown.</param>
/// <param name="PrivateModeName">Localized label for private mode (e.g. "Incognito", "InPrivate"), or <c>null</c>.</param>
internal sealed record BrowserInfo(string Name, string ExecutablePath, string? PrivateModeFlag, string? PrivateModeName);
