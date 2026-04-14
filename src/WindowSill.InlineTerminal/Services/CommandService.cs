using System.ComponentModel.Composition;
using WindowSill.API;
using WindowSill.InlineTerminal.Core;
using WindowSill.InlineTerminal.Core.Shell;
using WindowSill.InlineTerminal.Models;

namespace WindowSill.InlineTerminal.Services;

/// <summary>
/// Central orchestrator for command and run lifecycle.
/// Creates commands, executes runs, manages dismissal.
/// ViewModels delegate here instead of containing business logic.
/// </summary>
[Export]
internal sealed class CommandService
{
    private readonly Dictionary<Guid, CommandDefinition> _commands = [];
    private readonly Dictionary<Guid, (CommandDefinition Command, CommandRun Run)> _runIndex = [];
    private readonly Dictionary<Guid, CancellationTokenSource> _runCancellations = [];
    private readonly IPluginInfo _pluginInfo;
    private readonly IProcessInteractionService _processInteractionService;

    [ImportingConstructor]
    public CommandService(
        IPluginInfo pluginInfo,
        IProcessInteractionService processInteractionService)
    {
        _pluginInfo = pluginInfo;
        _processInteractionService = processInteractionService;
    }

    /// <summary>
    /// Raised when a command is added or removed.
    /// </summary>
    internal event EventHandler? CommandsChanged;

    /// <summary>
    /// Raised when a run is added, removed, or its state changes.
    /// </summary>
    internal event EventHandler? RunsChanged;

    /// <summary>
    /// Raised when a specific command is removed (all runs dismissed).
    /// </summary>
    internal event EventHandler<Guid>? CommandRemoved;

    /// <summary>
    /// Creates a new command definition and registers it.
    /// </summary>
    internal CommandDefinition CreateCommand(
        string? script,
        string? scriptFilePath,
        string? workingDirectory,
        ShellInfo defaultShell,
        WindowTextSelection? source)
    {
        var command = new CommandDefinition(script, scriptFilePath, workingDirectory, defaultShell, source);
        _commands[command.Id] = command;

        CommandsChanged?.Invoke(this, EventArgs.Empty);
        return command;
    }

    /// <summary>
    /// Executes a new run for the specified command. Returns the new <see cref="CommandRun"/>.
    /// Re-registers the command if it was previously dismissed.
    /// </summary>
    internal CommandRun Execute(
        CommandDefinition command,
        ShellInfo shell,
        string? script,
        string workingDirectory,
        ActionOnCommandCompleted actionOnCompleted,
        bool asElevated)
    {
        // Re-register if the command was previously dismissed but the sill still exists.
        if (!_commands.ContainsKey(command.Id))
        {
            _commands[command.Id] = command;
            CommandsChanged?.Invoke(this, EventArgs.Empty);
        }

        // Update mutable state on the command definition.
        if (!string.IsNullOrEmpty(script) && !string.IsNullOrEmpty(command.Script))
        {
            command.Script = script;
        }

        command.WorkingDirectory = workingDirectory;
        command.DefaultShell = shell;

        var run = new CommandRun
        {
            ActionOnCompleted = actionOnCompleted
        };

        command.Runs.Add(run);
        _runIndex[run.Id] = (command, run);

        RunsChanged?.Invoke(this, EventArgs.Empty);

        // Start execution in the background.
        StartExecutionAsync(command, run, shell, workingDirectory, asElevated).ForgetSafely();

        return run;
    }

    /// <summary>
    /// Cancels a running command run.
    /// </summary>
    internal void CancelRun(Guid runId)
    {
        if (_runIndex.TryGetValue(runId, out (CommandDefinition Command, CommandRun Run) entry)
            && entry.Run.State == CommandState.Running)
        {
            // Cancellation is handled via the CancellationTokenSource stored during execution.
            // The run will transition to Cancelled state in the execution task.
            if (_runCancellations.TryGetValue(runId, out CancellationTokenSource? cts))
            {
                cts.Cancel();
            }
        }
    }

    /// <summary>
    /// Dismisses (removes) a single run from a command.
    /// If the command has no more runs and is not in Created state, the command is also removed.
    /// </summary>
    internal void DismissRun(Guid commandId, Guid runId)
    {
        if (!_commands.TryGetValue(commandId, out CommandDefinition? command))
        {
            return;
        }

        CommandRun? run = command.Runs.FirstOrDefault(r => r.Id == runId);
        if (run is null)
        {
            return;
        }

        CancelAndCleanupRun(run);
        command.Runs.Remove(run);
        _runIndex.Remove(runId);

        RunsChanged?.Invoke(this, EventArgs.Empty);

        if (command.Runs.Count == 0)
        {
            RemoveCommand(commandId);
        }
    }

    /// <summary>
    /// Dismisses all runs of a specific command, cancelling any that are still running.
    /// Removes the command when no runs remain.
    /// </summary>
    internal void DismissAllRuns(Guid commandId)
    {
        if (!_commands.TryGetValue(commandId, out CommandDefinition? command))
        {
            return;
        }

        foreach (CommandRun run in command.Runs.ToList())
        {
            CancelAndCleanupRun(run);
            _runIndex.Remove(run.Id);
        }

        command.Runs.Clear();
        RunsChanged?.Invoke(this, EventArgs.Empty);

        RemoveCommand(commandId);
    }

    /// <summary>
    /// Dismisses all runs of a command except the specified one, cancelling any that are still running.
    /// </summary>
    internal void DismissOtherRuns(Guid commandId, Guid keepRunId)
    {
        if (!_commands.TryGetValue(commandId, out CommandDefinition? command))
        {
            return;
        }

        var toDismiss = command.Runs.Where(r => r.Id != keepRunId).ToList();
        if (toDismiss.Count == 0)
        {
            return;
        }

        foreach (CommandRun run in toDismiss)
        {
            CancelAndCleanupRun(run);
            command.Runs.Remove(run);
            _runIndex.Remove(run.Id);
        }

        RunsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Dismisses all commands and their runs.
    /// </summary>
    internal void DismissAllCommands()
    {
        List<Guid> commandIds = [.. _commands.Keys];
        foreach (Guid id in commandIds)
        {
            DismissAllRuns(id);
        }
    }

    /// <summary>
    /// Gets all active runs (across all commands) that have started.
    /// </summary>
    internal IReadOnlyList<(CommandDefinition Command, CommandRun Run)> GetAllActiveRuns()
    {
        return _runIndex.Values.ToArray();
    }

    /// <summary>
    /// Gets whether any run is currently in the Running state.
    /// </summary>
    internal bool HasRunningRuns()
    {
        return _runIndex.Values.Any(entry => entry.Run.State == CommandState.Running);
    }

    /// <summary>
    /// Copies the output of the latest run of a command to the clipboard.
    /// </summary>
    internal async Task CopyLatestOutputAsync(Guid commandId)
    {
        if (_commands.TryGetValue(commandId, out CommandDefinition? command) && command.LatestRun is { } run)
        {
            await ClipboardService.CopyAsync(run.Output, includeInClipboardHistory: true);
        }
    }

    private async Task StartExecutionAsync(
        CommandDefinition command,
        CommandRun run,
        ShellInfo shell,
        string workingDirectory,
        bool asElevated)
    {
        var cts = new CancellationTokenSource();
        _runCancellations[run.Id] = cts;

        try
        {
            string? script = command.Script;
            bool isScriptFile = false;

            if (!string.IsNullOrEmpty(command.ScriptFilePath) && File.Exists(command.ScriptFilePath))
            {
                script = $"\"{command.ScriptFilePath}\"";
                isScriptFile = true;
            }

            if (string.IsNullOrEmpty(script))
            {
                run.Complete(CommandState.Failed);
                return;
            }

            int exitCode;
            if (asElevated)
            {
                exitCode = await CommandExecutionHelper.ExecuteElevatedAsync(
                    script,
                    shell,
                    workingDirectory,
                    run.AppendOutput,
                    _pluginInfo,
                    cts.Token,
                    skipEscaping: isScriptFile);
            }
            else
            {
                exitCode = await CommandExecutionHelper.ExecuteAsync(
                    script,
                    shell,
                    workingDirectory,
                    run.AppendOutput,
                    cts.Token,
                    skipEscaping: isScriptFile);
            }

            run.Complete(exitCode == 0 ? CommandState.Completed : CommandState.Failed);
        }
        catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
        {
            run.Complete(CommandState.Cancelled);
        }
        catch (Exception)
        {
            run.Complete(CommandState.Failed);
        }
        finally
        {
            _runCancellations.Remove(run.Id);
            cts.Dispose();

            RunsChanged?.Invoke(this, EventArgs.Empty);

            await PerformPostCompletionActionAsync(command, run);
        }
    }

    private async Task PerformPostCompletionActionAsync(CommandDefinition command, CommandRun run)
    {
        await ClipboardService.PerformPostCompletionActionAsync(
            run.ActionOnCompleted,
            run.Output,
            command.Source,
            _processInteractionService);
    }

    private void RemoveCommand(Guid commandId)
    {
        if (_commands.Remove(commandId))
        {
            CommandsChanged?.Invoke(this, EventArgs.Empty);
            CommandRemoved?.Invoke(this, commandId);
        }
    }

    private void CancelAndCleanupRun(CommandRun run)
    {
        if (_runCancellations.TryGetValue(run.Id, out CancellationTokenSource? cts))
        {
            cts.Cancel();
            cts.Dispose();
            _runCancellations.Remove(run.Id);
        }

        run.Dispose();
    }
}
