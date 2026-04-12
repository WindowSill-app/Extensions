namespace WindowSill.InlineTerminal.Core.Shell;

/// <summary>
/// Represents a detected command-line shell with shell-specific formatting strategies.
/// </summary>
internal sealed class ShellInfo
{
    private readonly Func<string, string> _escapeCommand;
    private readonly Func<string, string> _buildArguments;
    private readonly Func<string, string, string> _buildElevatedArguments;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellInfo"/> class.
    /// </summary>
    /// <param name="displayName">Human-readable name (e.g., "PowerShell 7").</param>
    /// <param name="executablePath">Full path to the shell executable.</param>
    /// <param name="escapeCommand">Strategy to escape a command string for safe execution.</param>
    /// <param name="buildArguments">Strategy to build the full argument string for a command.</param>
    /// <param name="buildElevatedArguments">Strategy to build the argument string for elevated execution, redirecting output to a file. Parameters: (command, outputFilePath).</param>
    /// <param name="wslDistroName">The WSL distribution name, or <see langword="null"/> for native Windows shells.</param>
    /// <param name="icon">The icon extracted from the shell executable, or <see langword="null"/> if unavailable.</param>
    internal ShellInfo(
        string displayName,
        string executablePath,
        Func<string, string> escapeCommand,
        Func<string, string> buildArguments,
        Func<string, string, string> buildElevatedArguments,
        string? wslDistroName = null,
        ImageSource? icon = null)
    {
        DisplayName = displayName;
        ExecutablePath = executablePath;
        WslDistroName = wslDistroName;
        Icon = icon;
        _escapeCommand = escapeCommand;
        _buildArguments = buildArguments;
        _buildElevatedArguments = buildElevatedArguments;
    }

    /// <summary>
    /// Gets the human-readable shell name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the full path to the shell executable.
    /// </summary>
    public string ExecutablePath { get; }

    /// <summary>
    /// Gets the WSL distribution name, or <see langword="null"/> for native Windows shells.
    /// </summary>
    public string? WslDistroName { get; }

    /// <summary>
    /// Gets the icon extracted from the shell executable, or <see langword="null"/> if unavailable.
    /// </summary>
    public ImageSource? Icon { get; }

    /// <summary>
    /// Gets a value indicating whether this shell runs inside WSL.
    /// </summary>
    public bool IsWsl => WslDistroName is not null;

    /// <summary>
    /// Escapes a command string for safe execution in this shell.
    /// </summary>
    internal string EscapeCommand(string command) => _escapeCommand(command);

    /// <summary>
    /// Builds the full argument string to execute a command in this shell.
    /// </summary>
    internal string BuildArguments(string command) => _buildArguments(command);

    /// <summary>
    /// Builds the full argument string to execute a command with elevated privileges,
    /// redirecting all output to the specified file.
    /// </summary>
    internal string BuildElevatedArguments(string command, string outputFilePath)
        => _buildElevatedArguments(command, outputFilePath);
}
