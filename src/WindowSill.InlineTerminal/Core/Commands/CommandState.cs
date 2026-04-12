namespace WindowSill.InlineTerminal.Core.Commands;

/// <summary>
/// Represents the state of a command execution.
/// </summary>
public enum CommandState
{
    /// <summary>
    /// Command is discovered but not yet started.
    /// </summary>
    Created,

    /// <summary>
    /// Command is currently executing.
    /// </summary>
    Running,

    /// <summary>
    /// Command finished successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Command finished with an error.
    /// </summary>
    Failed,

    /// <summary>
    /// Command was cancelled by the user.
    /// </summary>
    Cancelled
}
