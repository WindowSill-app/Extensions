using System.ComponentModel.Composition;
using WindowSill.API;
using WindowSill.InlineTerminal.Models;

namespace WindowSill.InlineTerminal.Services;

/// <summary>
/// Periodically dismisses completed, unpinned runs that have exceeded the configured auto-dismiss interval.
/// </summary>
[Export]
internal sealed class AutoDismissService : IDisposable
{
    private static readonly TimeSpan tickInterval = TimeSpan.FromSeconds(30);

    private readonly CommandService _commandService;
    private readonly ISettingsProvider _settingsProvider;
    private readonly System.Threading.Timer _timer;

    [ImportingConstructor]
    public AutoDismissService(CommandService commandService, ISettingsProvider settingsProvider)
    {
        _commandService = commandService;
        _settingsProvider = settingsProvider;
        _timer = new System.Threading.Timer(OnTick, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Starts the auto-dismiss timer if auto-dismiss is enabled.
    /// Safe to call multiple times; restarts the timer each time.
    /// </summary>
    internal void Start()
    {
        int minutes = _settingsProvider.GetSetting(Settings.Settings.AutoDismissMinutes);
        if (minutes > 0)
        {
            _timer.Change(tickInterval, tickInterval);
        }
        else
        {
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>
    /// Stops the auto-dismiss timer.
    /// </summary>
    internal void Stop()
    {
        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _timer.Dispose();
    }

    private void OnTick(object? state)
    {
        int minutes = _settingsProvider.GetSetting(Settings.Settings.AutoDismissMinutes);
        if (minutes <= 0)
        {
            Stop();
            return;
        }

        var threshold = TimeSpan.FromMinutes(minutes);
        DateTime now = DateTime.UtcNow;

        IReadOnlyList<(CommandDefinition Command, CommandRun Run)> activeRuns = _commandService.GetAllActiveRuns();

        foreach ((CommandDefinition command, CommandRun run) in activeRuns)
        {
            if (run.State is CommandState.Running or CommandState.Created)
            {
                continue;
            }

            if (run.IsPinned)
            {
                continue;
            }

            if (run.CompletedAt is { } completedAt && (now - completedAt) >= threshold)
            {
                ThreadHelper.RunOnUIThreadAsync(() =>
                {
                    _commandService.DismissRun(command.Id, run.Id);
                }).ForgetSafely();
            }
        }
    }
}
