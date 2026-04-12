using CommunityToolkit.Mvvm.ComponentModel;
using ThrottleDebounce;
using WindowSill.API;
using WindowSill.InlineTerminal.Core.Commands;

namespace WindowSill.InlineTerminal.ViewModels;

internal sealed partial class OnGoingCommandsViewModel : ObservableObject, IObserver<CommandExecutionStatusChange>
{
    private readonly Lock _lock = new();
    private readonly CommandExecutionService _commandExecutionService;
    private readonly List<IDisposable> _subscriptions = new();
    private readonly RateLimitedAction _throttledNotify;

    internal OnGoingCommandsViewModel(CommandExecutionService commandExecutionService)
    {
        _commandExecutionService = commandExecutionService;
        _throttledNotify
            = Throttler.Throttle(
                () => ThreadHelper.RunOnUIThreadAsync(NotifyUpdateUI).ForgetSafely(),
                TimeSpan.FromMilliseconds(200));

        RefreshSubscriptions();
        _commandExecutionService.RunnersChanged += CommandExecutionService_RunnersChanged;
    }

    internal bool HasCommandsRunning => _commandExecutionService.GetStartedRunners().Any(entry => entry.State == CommandState.Running);

    internal IReadOnlyList<CommandRunnerHandle> StartedCommandRunners => _commandExecutionService.GetStartedRunners();

    public void OnCompleted()
    {
        _throttledNotify.Invoke();
    }

    public void OnError(Exception error)
    {
        _throttledNotify.Invoke();
    }

    public void OnNext(CommandExecutionStatusChange value)
    {
        _throttledNotify.Invoke();
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

        _throttledNotify.Invoke();
    }

    private void NotifyUpdateUI()
    {
        OnPropertyChanged(nameof(HasCommandsRunning));
        OnPropertyChanged(nameof(StartedCommandRunners));
    }

    private void CommandExecutionService_RunnersChanged(object? sender, EventArgs e)
    {
        RefreshSubscriptions();
    }
}
