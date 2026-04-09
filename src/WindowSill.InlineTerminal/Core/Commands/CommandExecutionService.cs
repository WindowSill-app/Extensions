using System.ComponentModel.Composition;
using WindowSill.API;
using WindowSill.InlineTerminal.Core.Shell;

namespace WindowSill.InlineTerminal.Core.Commands;

[Export]
internal sealed class CommandExecutionService
{
    private readonly HashSet<CommandRunner> _backgroundRunners = new();
    private readonly IPluginInfo _pluginInfo;
    private readonly IProcessInteractionService _processInteractionService;
    private readonly ShellDetectionService _shellDetectionService;

    [ImportingConstructor]
    public CommandExecutionService(
        IPluginInfo pluginInfo,
        IProcessInteractionService processInteractionService,
        ShellDetectionService shellDetectionService)
    {
        _pluginInfo = pluginInfo;
        _processInteractionService = processInteractionService;
        _shellDetectionService = shellDetectionService;
    }

    internal event EventHandler? BackgroundRunnersRemoved;

    internal IReadOnlyList<CommandRunner> GetBackgroundRunners() => _backgroundRunners.ToArray();

    internal async Task<(CommandRunner runner, IDisposable unsubscriber)> CreateAndStartRunnerAsync(
        IObserver<CommandExecutionStatusChange> observer,
        WindowTextSelection? windowTextSelection,
        ShellInfo? shell,
        string? workingDirectory,
        string? script,
        string? scriptFilePath,
        ActionOnCommandCompleted actionOnCompleted,
        bool asElevated)
    {
        var runner
            = new CommandRunner(
                _pluginInfo,
                _processInteractionService,
                windowTextSelection,
                await _shellDetectionService.GetAvailableShellsAsync(),
                shell,
                workingDirectory,
                script,
                scriptFilePath);

        _backgroundRunners.Add(runner);

        IDisposable unsubscriber = runner.Subscribe(observer);
        runner.Start(actionOnCompleted, asElevated);

        return new(runner, unsubscriber);
    }

    internal void DestroyRunner(CommandRunner runner)
    {
        if (_backgroundRunners.Contains(runner))
        {
            runner.Cancel();
            runner.Dispose();
            _backgroundRunners.Remove(runner);
            BackgroundRunnersRemoved?.Invoke(this, EventArgs.Empty);
        }
    }
}
