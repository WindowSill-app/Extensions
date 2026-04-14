namespace WindowSill.UniversalCommands.Core;

internal enum UniversalCommandType
{
    /// <summary>
    /// Simulates a keyboard shortcut (e.g., Ctrl+Shift+P).
    /// </summary>
    KeyboardShortcut,

    /// <summary>
    /// Executes a PowerShell command or script.
    /// </summary>
    PowerShellCommand
}
