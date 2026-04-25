using System.Collections.ObjectModel;
using WindowSill.API;
using WindowSill.InlineTerminal.Core.Shell;
using Path = System.IO.Path;

namespace WindowSill.InlineTerminal.Models;

/// <summary>
/// Represents a command definition — the immutable intent of what the user wants to run.
/// A single definition can have multiple <see cref="CommandRun"/> instances (re-runs).
/// </summary>
internal sealed class CommandDefinition
{
    internal CommandDefinition(
        string? script,
        string? scriptFilePath,
        string? workingDirectory,
        ShellInfo defaultShell,
        WindowTextSelection? source)
    {
        if (string.IsNullOrEmpty(script) && string.IsNullOrEmpty(scriptFilePath))
        {
            throw new ArgumentException($"At least one of {nameof(script)} or {nameof(scriptFilePath)} must be provided.");
        }

        Id = Guid.NewGuid();
        Script = script;
        ScriptFilePath = scriptFilePath;
        WorkingDirectory = workingDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        DefaultShell = defaultShell;
        Source = source;
    }

    /// <summary>
    /// Gets the unique identifier for this command definition.
    /// </summary>
    internal Guid Id { get; }

    /// <summary>
    /// Gets the shell script or command text to execute, if provided.
    /// </summary>
    internal string? Script { get; set; }

    /// <summary>
    /// Gets the path to the script file to execute, if provided.
    /// </summary>
    internal string? ScriptFilePath { get; }

    /// <summary>
    /// Gets or sets the working directory for the command execution.
    /// </summary>
    internal string WorkingDirectory { get; set; }

    /// <summary>
    /// Gets the default shell for execution.
    /// </summary>
    internal ShellInfo DefaultShell { get; set; }

    /// <summary>
    /// Gets the text selection context from the source window, if any.
    /// </summary>
    internal WindowTextSelection? Source { get; }

    /// <summary>
    /// Gets the collection of runs (executions) of this command.
    /// Observable so the UI can bind to it.
    /// </summary>
    internal ObservableCollection<CommandRun> Runs { get; } = [];

    /// <summary>
    /// Gets a display title derived from the script or file path.
    /// </summary>
    internal string Title => GenerateTitle(Script, ScriptFilePath);

    /// <summary>
    /// Gets whether this command has been executed at least once.
    /// </summary>
    internal bool HasBeenExecuted => Runs.Count > 0;

    /// <summary>
    /// Gets the most recent run, or <see langword="null"/> if never executed.
    /// </summary>
    internal CommandRun? LatestRun => Runs.Count > 0 ? Runs[^1] : null;

    /// <summary>
    /// Generates a display title from a script or script file path.
    /// </summary>
    /// <param name="script">The script text, if any.</param>
    /// <param name="scriptFilePath">The script file path, if any.</param>
    /// <returns>A short display title.</returns>
    internal static string GenerateTitle(string? script, string? scriptFilePath)
    {
        if (!string.IsNullOrEmpty(scriptFilePath))
        {
            return Path.GetFileName(scriptFilePath);
        }

        return script?
            .Substring(0, Math.Min(100, script.Length))
            .Replace("\r\n", "⏎")
            .Replace("\n\r", "⏎")
            .Replace('\r', '⏎')
            .Replace('\n', '⏎')
            ?? string.Empty;
    }
}
