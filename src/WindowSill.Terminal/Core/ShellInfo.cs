namespace WindowSill.Terminal.Core;

/// <summary>
/// Represents a detected command-line shell.
/// </summary>
/// <param name="DisplayName">Human-readable name (e.g., "PowerShell 7").</param>
/// <param name="ExecutablePath">Full path to the shell executable.</param>
/// <param name="ArgumentPrefix">Argument prefix to run a command (e.g., "-Command" for pwsh, "/c" for cmd).</param>
public sealed record ShellInfo(string DisplayName, string ExecutablePath, string ArgumentPrefix);
