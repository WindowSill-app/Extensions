using WindowSill.API;
using WindowSill.InlineTerminal.Core.Shell;
using Path = System.IO.Path;

namespace WindowSill.InlineTerminal.Core.Commands;

/// <summary>
/// A read-only view of a <see cref="CommandRunner"/> that exposes its properties and output stream
/// without allowing callers to start, cancel, or dispose the runner directly.
/// Use <see cref="CommandExecutionService"/> to perform lifecycle operations.
/// </summary>
public sealed class CommandRunnerHandle : IObservable<CommandExecutionStatusChange>
{
    private readonly CommandRunner _runner;

    internal CommandRunnerHandle(CommandRunner runner)
    {
        _runner = runner;
    }

    /// <summary>
    /// Gets the unique identifier for this command runner.
    /// </summary>
    internal Guid Id => _runner.Id;

    internal string Title => GenerateTitle();

    /// <summary>
    /// Gets the current execution state of the command.
    /// </summary>
    internal CommandState State => _runner.State;

    /// <summary>
    /// Gets the shell script or command text to execute, if provided.
    /// </summary>
    internal string? DefaultScript => _runner.DefaultScript;

    /// <summary>
    /// Gets the path to the script file to execute, if provided.
    /// </summary>
    internal string? ScriptFilePath => _runner.ScriptFilePath;

    /// <summary>
    /// Gets the working directory for the command execution.
    /// </summary>
    internal string WorkingDirectory => _runner.DefaultWorkingDirectory;

    /// <summary>
    /// Gets the default shell for execution.
    /// </summary>
    internal ShellInfo DefaultShell => _runner.DefaultShell;

    /// <summary>
    /// Gets all output produced by the command so far.
    /// </summary>
    internal string Output => _runner.Output;

    /// <summary>
    /// Gets all output produced by the command so far.
    /// </summary>
    internal string OutputTrimmed => _runner.Output.Trim();

    /// <summary>
    /// Gets the text selection context from the source window, if any.
    /// </summary>
    internal WindowTextSelection? WindowTextSelection => _runner.WindowTextSelection;

    /// <summary>
    /// Gets the action to perform when the command completes.
    /// </summary>
    internal ActionOnCommandCompleted ActionOnCommandCompleted => _runner.ActionOnCommandCompleted;

    /// <summary>
    /// Subscribes to the command's output stream. Late subscribers receive all previously
    /// emitted output lines before receiving live updates.
    /// </summary>
    /// <param name="observer">The observer to receive output notifications.</param>
    /// <returns>A handle that, when disposed, unsubscribes the observer.</returns>
    public IDisposable Subscribe(IObserver<CommandExecutionStatusChange> observer)
    {
        return _runner.Subscribe(observer);
    }

    private string GenerateTitle()
    {
        if (!string.IsNullOrEmpty(ScriptFilePath))
        {
            return Path.GetFileName(ScriptFilePath);
        }

        return DefaultScript?
            .Substring(0, Math.Min(100, DefaultScript.Length))
            .Replace("\r\n", "⏎")
            .Replace("\n\r", "⏎")
            .Replace('\r', '⏎')
            .Replace('\n', '⏎')
            ?? string.Empty;
    }
}
