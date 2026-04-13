namespace WindowSill.InlineTerminal.Models;

/// <summary>
/// Defines the action to perform after a command completes execution.
/// </summary>
internal enum ActionOnCommandCompleted
{
    /// <summary>
    /// No post-completion action.
    /// </summary>
    None,

    /// <summary>
    /// Copy the output to the clipboard.
    /// </summary>
    Copy,

    /// <summary>
    /// Append the output after the selected text.
    /// </summary>
    AppendSelection,

    /// <summary>
    /// Replace the selected text with the output.
    /// </summary>
    ReplaceSelection
}
