using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowSill.API;
using WindowSill.InlineTerminal.Core.Commands;
using WindowSill.InlineTerminal.Core.Shell;

namespace WindowSill.InlineTerminal.ViewModels;

internal sealed partial class CommandViewModel : ObservableObject, IObserver<CommandExecutionStatusChange>, IDisposable
{
    private static readonly long throttleIntervalTicks = TimeSpan.FromMilliseconds(100).Ticks;

    private readonly CommandExecutionService _commandExecutionService;
    private readonly CommandRunnerHandle _commandRunnerHandle;
    private readonly IDisposable _commandRunnerSubscriber;

    private long _lastOnNextTimestamp;

    public CommandViewModel(
        CommandExecutionService commandExecutionService,
        CommandRunnerHandle commandRunnerHandle,
        IReadOnlyList<ShellInfo> availableShells)
    {
        _commandExecutionService = commandExecutionService;
        _commandRunnerHandle = commandRunnerHandle;
        AvailableShells = availableShells;

        _commandRunnerSubscriber = _commandRunnerHandle.Subscribe(this);
        SelectedShell = _commandRunnerHandle.DefaultShell;
        Script = _commandRunnerHandle.DefaultScript;
        WorkingDirectory = _commandRunnerHandle.WorkingDirectory;
    }

    internal Guid Id => _commandRunnerHandle.Id;

    internal CommandState State => _commandRunnerHandle.State;

    internal string OutputText => _commandRunnerHandle.Output;

    internal IReadOnlyList<ShellInfo> AvailableShells { get; }

    [ObservableProperty]
    internal partial ShellInfo SelectedShell { get; set; }

    [ObservableProperty]
    internal partial string? Script { get; set; }

    internal string? ScriptFilePath => _commandRunnerHandle.ScriptFilePath;

    internal string? Title => _commandRunnerHandle.Title;

    [ObservableProperty]
    internal partial string WorkingDirectory { get; set; }

    [ObservableProperty]
    internal partial bool RunAsAdministrator { get; set; }

    internal event EventHandler? RequestClose;

    public void Dispose()
    {
        _commandRunnerSubscriber.Dispose();
    }

    public void OnCompleted()
    {
        ThreadHelper.RunOnUIThreadAsync(NotifyUpdateUI).ForgetSafely();
    }

    public void OnError(Exception error)
    {
        ThreadHelper.RunOnUIThreadAsync(NotifyUpdateUI).ForgetSafely();
    }

    public void OnNext(CommandExecutionStatusChange value)
    {
        long now = Environment.TickCount64 * TimeSpan.TicksPerMillisecond;
        long last = Interlocked.Read(ref _lastOnNextTimestamp);

        if (now - last < throttleIntervalTicks)
        {
            return;
        }

        Interlocked.Exchange(ref _lastOnNextTimestamp, now);
        ThreadHelper.RunOnUIThreadAsync(NotifyUpdateUI).ForgetSafely();
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    internal async Task CopyOutputAsync()
    {
        await _commandExecutionService.CopyOutputAsync(_commandRunnerHandle.Id, includeInClipboardHistory: true);
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    internal void Cancel()
    {
        _commandExecutionService.Cancel(_commandRunnerHandle.Id);
        OnPropertyChanged(nameof(State));
    }

    [RelayCommand]
    internal void Dismiss()
    {
        _commandExecutionService.Destroy(_commandRunnerHandle.Id);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanRun))]
    internal async Task RunMenuAsync()
    {
        await StartRunScriptAsync(ActionOnCommandCompleted.None);
    }

    [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanRun))]
    internal async Task RunAndCopyMenuAsync()
    {
        await StartRunScriptAsync(ActionOnCommandCompleted.Copy);
    }

    [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanRun))]
    internal async Task RunAndAppendMenuAsync()
    {
        await StartRunScriptAsync(ActionOnCommandCompleted.AppendSelection);
    }

    [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanRun))]
    internal async Task RunAndReplaceMenuAsync()
    {
        await StartRunScriptAsync(ActionOnCommandCompleted.ReplaceSelection);
    }

    private async Task StartRunScriptAsync(ActionOnCommandCompleted actionOnCommandCompleted)
    {
        _commandExecutionService.Start(
            _commandRunnerHandle.Id,
            SelectedShell,
            Script,
            WorkingDirectory,
            actionOnCommandCompleted,
            RunAsAdministrator);
        await ThreadHelper.RunOnUIThreadAsync(NotifyUpdateUI);
    }

    private void NotifyUpdateUI()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(OutputText));
        CancelCommand.NotifyCanExecuteChanged();
        RunMenuCommand.NotifyCanExecuteChanged();
        RunAndAppendMenuCommand.NotifyCanExecuteChanged();
        RunAndCopyMenuCommand.NotifyCanExecuteChanged();
        RunAndReplaceMenuCommand.NotifyCanExecuteChanged();
    }

    private bool CanCancel()
    {
        return State == CommandState.Running;
    }

    private bool CanRun()
    {
        return State != CommandState.Running;
    }
}
