using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;
using WindowSill.InlineTerminal.Core.Commands;

namespace WindowSill.InlineTerminal.ViewModels;

internal sealed partial class OnGoingCommandsViewModel : ObservableObject, IObserver<CommandExecutionStatusChange>
{
    private readonly Lock _lock = new();
    private readonly CommandExecutionService _commandExecutionService;
    private readonly List<IDisposable> _subscriptions = new();

    private CancellationTokenSource? _throttleCts;

    internal OnGoingCommandsViewModel(CommandExecutionService commandExecutionService)
    {
        _commandExecutionService = commandExecutionService;

        RefreshSubscriptions();
        _commandExecutionService.RunnersChanged += CommandExecutionService_RunnersChanged;
    }

    internal bool HasCommandsRunning => _commandExecutionService.GetStartedRunners().Any(entry => entry.State == CommandState.Running);

    internal IReadOnlyList<CommandRunnerHandle> StartedCommandRunners => _commandExecutionService.GetStartedRunners();

    public void OnCompleted()
    {
        ThrottleNotifyHasCommandsRunning();
    }

    public void OnError(Exception error)
    {
        ThrottleNotifyHasCommandsRunning();
    }

    public void OnNext(CommandExecutionStatusChange value)
    {
        ThrottleNotifyHasCommandsRunning();
    }

    /// <summary>
    /// Debounces rapid status change notifications and dispatches a single
    /// <see cref="HasCommandsRunning"/> property-changed update on the UI thread.
    /// </summary>
    private void ThrottleNotifyHasCommandsRunning()
    {
        lock (_lock)
        {
            _throttleCts?.Cancel();
            _throttleCts?.Dispose();
            _throttleCts = new CancellationTokenSource();
            CancellationToken token = _throttleCts.Token;
            NotifyAfterDelayAsync(token).ForgetSafely();
        }
    }

    private async Task NotifyAfterDelayAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken).ConfigureAwait(false);

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            OnPropertyChanged(nameof(HasCommandsRunning));
            OnPropertyChanged(nameof(StartedCommandRunners));
        });
    }

    private void RefreshSubscriptions()
    {
        lock (_lock)
        {
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                _subscriptions[i].Dispose();
            }
            _subscriptions.Clear();

            IReadOnlyList<CommandRunnerHandle> runners = _commandExecutionService.GetAllRunners();
            for (int i = 0; i < runners.Count; i++)
            {
                _subscriptions.Add(runners[i].Subscribe(this));
            }
        }

        ThrottleNotifyHasCommandsRunning();
    }

    private void CommandExecutionService_RunnersChanged(object? sender, EventArgs e)
    {
        RefreshSubscriptions();
    }
}
