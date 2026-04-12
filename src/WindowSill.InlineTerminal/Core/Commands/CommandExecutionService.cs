using System.ComponentModel.Composition;
using WindowSill.API;
using WindowSill.InlineTerminal.Core.Shell;

namespace WindowSill.InlineTerminal.Core.Commands;

/// <summary>
/// Manages the lifecycle of command runners. Creates, starts, cancels, and destroys runners.
/// Callers interact with runners through <see cref="CommandRunnerHandle"/> — a read-only view
/// that exposes properties and output streaming without allowing direct lifecycle control.
/// </summary>
[Export]
internal sealed class CommandExecutionService
{
    private readonly Dictionary<Guid, (CommandRunner Runner, CommandRunnerHandle Handle)> _runners = [];
    private readonly IPluginInfo _pluginInfo;
    private readonly IProcessInteractionService _processInteractionService;

    [ImportingConstructor]
    public CommandExecutionService(
        IPluginInfo pluginInfo,
        IProcessInteractionService processInteractionService)
    {
        _pluginInfo = pluginInfo;
        _processInteractionService = processInteractionService;
    }

    /// <summary>
    /// Raised when runners are added, removed, or their status changed, so the UI can refresh its list.
    /// </summary>
    internal event EventHandler? RunnersChanged;

    internal event EventHandler<Guid>? RunnerDestroyed;

    /// <summary>
    /// Returns read-only handles for all tracked runners.
    /// </summary>
    internal IReadOnlyList<CommandRunnerHandle> GetAllRunners()
    {
        return _runners.Values.Select(entry => entry.Handle).ToArray();
    }

    /// <summary>
    /// Returns read-only handles for all tracked runners that have started execution.
    /// </summary>
    internal IReadOnlyList<CommandRunnerHandle> GetStartedRunners()
    {
        return _runners.Values.Where(entry => entry.Handle.State != CommandState.Created).Select(entry => entry.Handle).ToArray();
    }

    /// <summary>
    /// Creates a new command runner and returns a read-only handle.
    /// The runner starts in <see cref="CommandState.Created"/> state.
    /// Use <see cref="Start"/> to begin execution.
    /// </summary>
    internal async Task<CommandRunnerHandle> CreateAsync(
        WindowTextSelection? windowTextSelection,
        ShellInfo defaultShell,
        string? workingDirectory,
        string? script,
        string? scriptFilePath)
    {
        var runner
            = new CommandRunner(
                _pluginInfo,
                _processInteractionService,
                windowTextSelection,
                defaultShell,
                workingDirectory,
                script,
                scriptFilePath);

        var handle = new CommandRunnerHandle(runner);
        _runners[runner.Id] = (runner, handle);

        RunnersChanged?.Invoke(this, EventArgs.Empty);
        return handle;
    }

    /// <summary>
    /// Starts execution of the runner identified by <paramref name="id"/>.
    /// Does nothing if the runner is not found or is already running.
    /// </summary>
    internal void Start(
        Guid id,
        ShellInfo shell,
        string? script,
        string workingDirectory,
        ActionOnCommandCompleted actionOnCompleted,
        bool asElevated)
    {
        if (_runners.TryGetValue(id, out (CommandRunner Runner, CommandRunnerHandle Handle) entry))
        {
            entry.Runner.Start(shell, script, workingDirectory, actionOnCompleted, asElevated);
            RunnersChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Cancels execution of the runner identified by <paramref name="id"/>.
    /// Does nothing if the runner is not found.
    /// </summary>
    internal void Cancel(Guid id)
    {
        if (_runners.TryGetValue(id, out (CommandRunner Runner, CommandRunnerHandle Handle) entry))
        {
            entry.Runner.Cancel();
            RunnersChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Copies the output of the runner identified by <paramref name="id"/> to the clipboard.
    /// Does nothing if the runner is not found.
    /// </summary>
    internal async Task CopyOutputAsync(Guid id, bool includeInClipboardHistory)
    {
        if (_runners.TryGetValue(id, out (CommandRunner Runner, CommandRunnerHandle Handle) entry))
        {
            await entry.Runner.CopyOutputToClipboardAsync(includeInClipboardHistory);
        }
    }

    /// <summary>
    /// Destroys the runner identified by <paramref name="id"/>, cancelling it if running
    /// and releasing all resources.
    /// </summary>
    internal void Destroy(Guid id)
    {
        if (_runners.Remove(id, out (CommandRunner Runner, CommandRunnerHandle Handle) entry))
        {
            entry.Runner.Cancel();
            entry.Runner.Dispose();
            RunnersChanged?.Invoke(this, EventArgs.Empty);
            RunnerDestroyed?.Invoke(this, id);
        }
    }

    internal void DestroyAllStartedRunners()
    {
        List<Guid>? pendingIds = null;
        foreach ((Guid id, (CommandRunner Runner, CommandRunnerHandle Handle) entry) in _runners)
        {
            if (entry.Runner.State != CommandState.Created)
            {
                pendingIds ??= [];
                pendingIds.Add(id);
            }
        }

        if (pendingIds is not null)
        {
            for (int i = 0; i < pendingIds.Count; i++)
            {
                Destroy(pendingIds[i]);
            }
        }
    }
}
