namespace WindowSill.Terminal.Core;

/// <summary>
/// Detects available command-line shells on the system.
/// </summary>
public interface IShellDetectionService
{
    /// <summary>
    /// Returns the list of available shells, ordered by preference (first = default).
    /// </summary>
    IReadOnlyList<ShellInfo> GetAvailableShells();
}
