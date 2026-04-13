namespace WindowSill.InlineTerminal.Core.Shell;

/// <summary>
/// Converts file system paths between Windows and WSL formats.
/// </summary>
internal static class WslPathConverter
{
    /// <summary>
    /// Converts a Windows path (e.g., <c>C:\Users\me</c>) to a WSL mount path (e.g., <c>/mnt/c/Users/me</c>).
    /// Returns the original path unchanged if it is not a rooted Windows path with a drive letter.
    /// </summary>
    internal static string ConvertToWslPath(string windowsPath)
    {
        // Already a Unix-style path — return as-is.
        if (windowsPath.StartsWith('/'))
        {
            return windowsPath;
        }

        // Expect "X:\" or "X:/" rooted paths.
        if (windowsPath.Length >= 3
            && char.IsAsciiLetter(windowsPath[0])
            && windowsPath[1] == ':'
            && (windowsPath[2] == '\\' || windowsPath[2] == '/'))
        {
            char driveLetter = char.ToLowerInvariant(windowsPath[0]);
            string rest = windowsPath[3..].Replace('\\', '/');
            return $"/mnt/{driveLetter}/{rest}";
        }

        // UNC or relative paths — return with slashes replaced as a best-effort.
        return windowsPath.Replace('\\', '/');
    }
}
