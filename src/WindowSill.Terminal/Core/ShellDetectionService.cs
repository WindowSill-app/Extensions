using System.ComponentModel.Composition;

using Path = System.IO.Path;

namespace WindowSill.Terminal.Core;

/// <summary>
/// Detects available command-line shells by searching the system PATH.
/// </summary>
[Export(typeof(IShellDetectionService))]
public sealed class ShellDetectionService : IShellDetectionService
{
    private IReadOnlyList<ShellInfo>? _cachedShells;

    /// <inheritdoc />
    public IReadOnlyList<ShellInfo> GetAvailableShells()
    {
        return _cachedShells ??= DetectShells();
    }

    private static List<ShellInfo> DetectShells()
    {
        (string ExeName, string DisplayName, string ArgumentPrefix)[] candidates =
        [
            ("pwsh", "PowerShell 7", "-Command"),
            ("powershell", "Windows PowerShell", "-Command"),
            ("cmd", "Command Prompt", "/c"),
        ];

        List<ShellInfo> shells = [];

        foreach (var (exeName, displayName, argPrefix) in candidates)
        {
            string? fullPath = FindExecutableOnPath(exeName);

            if (fullPath is not null)
            {
                shells.Add(new ShellInfo(displayName, fullPath, argPrefix));
            }
        }

        return shells;
    }

    private static string? FindExecutableOnPath(string exeName)
    {
        string fileName = OperatingSystem.IsWindows() ? $"{exeName}.exe" : exeName;

        string? pathVariable = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrEmpty(pathVariable))
        {
            return null;
        }

        foreach (string directory in pathVariable.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            string fullPath = Path.Combine(directory, fileName);

            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }
}
