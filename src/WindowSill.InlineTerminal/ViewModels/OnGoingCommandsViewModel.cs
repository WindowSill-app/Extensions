using CommunityToolkit.Mvvm.ComponentModel;
using ThrottleDebounce;
using WindowSill.API;
using WindowSill.InlineTerminal.Models;
using WindowSill.InlineTerminal.Services;

namespace WindowSill.InlineTerminal.ViewModels;

/// <summary>
/// Manages the list of active command runs for the on-going commands view.
/// Listens to <see cref="CommandService"/> events instead of directly subscribing to runners.
/// </summary>
internal sealed partial class OnGoingCommandsViewModel : ObservableObject
{
    private readonly CommandService _commandService;
    private readonly RateLimitedAction _throttledNotify;
    private readonly List<IDisposable> _subscriptions = [];

    internal OnGoingCommandsViewModel(CommandService commandService)
    {
        _commandService = commandService;
        _throttledNotify
            = Throttler.Throttle(
                () => ThreadHelper.RunOnUIThreadAsync(NotifyUpdateUI).ForgetSafely(),
                TimeSpan.FromMilliseconds(200));

        _commandService.RunsChanged += OnRunsChanged;
        _commandService.CommandsChanged += OnCommandsChanged;
        RefreshSubscriptions();
    }

    /// <summary>
    /// Gets whether any run is currently executing.
    /// </summary>
    internal bool HasCommandsRunning => _commandService.HasRunningRuns();

    /// <summary>
    /// Gets all active runs for display in the UI.
    /// </summary>
    internal IReadOnlyList<ActiveRunItem> ActiveRuns
        => _commandService.GetAllActiveRuns()
            .Select(entry => new ActiveRunItem(entry.Command, entry.Run))
            .ToArray();

    private void RefreshSubscriptions()
    {
        foreach (IDisposable sub in _subscriptions)
        {
            sub.Dispose();
        }

        _subscriptions.Clear();

        foreach ((CommandDefinition _, CommandRun run) in _commandService.GetAllActiveRuns())
        {
            _subscriptions.Add(run.OutputLines.Subscribe(new RunObserver(this)));
        }

        _throttledNotify.Invoke();
    }

    private void NotifyUpdateUI()
    {
        OnPropertyChanged(nameof(HasCommandsRunning));
        OnPropertyChanged(nameof(ActiveRuns));
    }

    private void OnRunsChanged(object? sender, EventArgs e) => RefreshSubscriptions();

    private void OnCommandsChanged(object? sender, EventArgs e) => RefreshSubscriptions();

    private sealed class RunObserver(OnGoingCommandsViewModel vm) : IObserver<string>
    {
        public void OnNext(string value) => vm._throttledNotify.Invoke();
        public void OnCompleted() => vm._throttledNotify.Invoke();
        public void OnError(Exception error) => vm._throttledNotify.Invoke();
    }
}
