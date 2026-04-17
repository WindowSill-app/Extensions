using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ThrottleDebounce;
using WindowSill.API;
using WindowSill.InlineTerminal.Core.Shell;
using WindowSill.InlineTerminal.Models;
using WindowSill.InlineTerminal.Services;

namespace WindowSill.InlineTerminal.ViewModels;

/// <summary>
/// Thin presentation wrapper around a <see cref="CommandDefinition"/>.
/// Delegates all business logic to <see cref="CommandService"/>.
/// </summary>
internal sealed partial class CommandViewModel : ObservableObject, IDisposable
{
    private readonly CommandService _commandService;
    private readonly CommandDefinition _command;
    private readonly ISettingsProvider _settingsProvider;
    private readonly List<IDisposable> _runSubscriptions = [];
    private readonly RateLimitedAction _throttledNotify;

    internal CommandViewModel(
        CommandService commandService,
        CommandDefinition command,
        IReadOnlyList<ShellInfo> availableShells,
        ISettingsProvider settingsProvider)
    {
        _commandService = commandService;
        _command = command;
        _settingsProvider = settingsProvider;
        AvailableShells = availableShells;
        _throttledNotify
            = Throttler.Throttle(
                () => ThreadHelper.RunOnUIThreadAsync(NotifyUpdateUI).ForgetSafely(),
                TimeSpan.FromMilliseconds(100));

        SelectedShell = command.DefaultShell;
        Script = command.Script;
        WorkingDirectory = command.WorkingDirectory;

        SubscribeToRuns();
    }

    /// <summary>
    /// Gets the command definition ID.
    /// </summary>
    internal Guid CommandId => _command.Id;

    /// <summary>
    /// Gets the command definition.
    /// </summary>
    internal CommandDefinition Command => _command;

    /// <summary>
    /// Gets the display title, derived from the current (possibly edited) script or file path.
    /// </summary>
    internal string Title => CommandDefinition.GenerateTitle(Script, ScriptFilePath);

    /// <summary>
    /// Gets whether this command has been executed at least once.
    /// </summary>
    internal bool HasBeenExecuted => _command.HasBeenExecuted;

    /// <summary>
    /// Gets the number of runs.
    /// </summary>
    internal int RunCount => _command.Runs.Count;

    /// <summary>
    /// Gets the latest run, if any.
    /// </summary>
    internal CommandRun? LatestRun => _command.LatestRun;

    /// <summary>
    /// Gets the current state (from latest run, or Created if none).
    /// </summary>
    internal CommandState State => _command.LatestRun?.State ?? CommandState.Created;

    /// <summary>
    /// Gets the output text from the latest run.
    /// </summary>
    internal string OutputText => _command.LatestRun?.Output ?? string.Empty;

    /// <summary>
    /// Gets the latest run's start time formatted for display, or empty if no runs.
    /// </summary>
    internal string LatestRunStartedAt => _command.LatestRun?.StartedAt.ToLocalTime().ToString("T") ?? string.Empty;

    /// <summary>
    /// Gets the available shells.
    /// </summary>
    internal IReadOnlyList<ShellInfo> AvailableShells { get; }

    /// <summary>
    /// Gets or sets the selected shell.
    /// </summary>
    [ObservableProperty]
    internal partial ShellInfo SelectedShell { get; set; }

    /// <summary>
    /// Gets or sets the editable command text.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    internal partial string? Script { get; set; }

    /// <summary>
    /// Gets the script file path, if any.
    /// </summary>
    internal string? ScriptFilePath => _command.ScriptFilePath;

    /// <summary>
    /// Gets or sets the working directory.
    /// </summary>
    [ObservableProperty]
    internal partial string WorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets whether to run as administrator.
    /// </summary>
    [ObservableProperty]
    internal partial bool RunAsAdministrator { get; set; }

    /// <summary>
    /// Optional callback to confirm run (e.g., ClickFix warning dialog).
    /// </summary>
    internal Func<Task<bool>>? ConfirmRunAsync { get; set; }

    /// <summary>
    /// Raised when the popup should close (e.g., after dismiss).
    /// </summary>
    internal event EventHandler? RequestClose;

    /// <summary>
    /// Raised when this view model is disposed, allowing external subscribers to clean up.
    /// </summary>
    internal event EventHandler? Disposed;

    /// <summary>
    /// Gets whether output text should wrap.
    /// </summary>
    internal bool WordWrapOutput => _settingsProvider.GetSetting(Settings.Settings.WordWrapOutput);

    /// <summary>
    /// Gets the label for the run button — "Run" or "Re-run".
    /// </summary>
    internal string RunButtonLabel => HasBeenExecuted
        ? "/WindowSill.InlineTerminal/CommandPopupConfigurePage/RerunButton".GetLocalizedString()
        : "/WindowSill.InlineTerminal/CommandPopupConfigurePage/RunButton".GetLocalizedString();

    /// <summary>
    /// Determines whether the ClickFix security warning should be shown.
    /// </summary>
    internal bool ShouldShowClickFixWarning()
    {
        return SecurityService.ShouldShowClickFixWarning(_command.Source, _settingsProvider);
    }

    /// <summary>
    /// Persists the user's choice to disable the ClickFix warning.
    /// </summary>
    internal void DisableClickFixWarning()
    {
        _settingsProvider.SetSetting(Settings.Settings.DisableClickFixWarning, true);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeRunSubscriptions();
        Disposed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    internal async Task CopyLatestOutputAsync()
    {
        await _commandService.CopyLatestOutputAsync(_command.Id);
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    internal void Cancel()
    {
        if (_command.LatestRun is { } run)
        {
            _commandService.CancelRun(run.Id);
        }

        NotifyUpdateUI();
    }

    [RelayCommand]
    internal void Dismiss()
    {
        if (_command.LatestRun is { } run)
        {
            _commandService.DismissRun(_command.Id, run.Id);
        }

        if (!_command.HasBeenExecuted)
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            NotifyUpdateUI();
        }
    }

    [RelayCommand]
    internal void DismissOthers()
    {
        if (_command.LatestRun is { } run)
        {
            _commandService.DismissOtherRuns(_command.Id, run.Id);
        }

        NotifyUpdateUI();
    }

    [RelayCommand]
    internal void DismissAll()
    {
        _commandService.DismissAllRuns(_command.Id);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the number of other runs (excluding the latest).
    /// </summary>
    internal int OtherRunsCount => Math.Max(0, _command.Runs.Count - 1);

    /// <summary>
    /// Gets whether there are other runs besides the latest.
    /// </summary>
    internal bool HasOtherRuns => _command.Runs.Count > 1;

    [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanRun))]
    internal async Task RunMenuAsync()
    {
        await StartRunAsync(ActionOnCommandCompleted.None);
    }

    [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanRun))]
    internal async Task RunAndCopyMenuAsync()
    {
        await StartRunAsync(ActionOnCommandCompleted.Copy);
    }

    [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanRun))]
    internal async Task RunAndAppendMenuAsync()
    {
        await StartRunAsync(ActionOnCommandCompleted.AppendSelection);
    }

    [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanRun))]
    internal async Task RunAndReplaceMenuAsync()
    {
        await StartRunAsync(ActionOnCommandCompleted.ReplaceSelection);
    }

    private async Task StartRunAsync(ActionOnCommandCompleted action)
    {
        if (ConfirmRunAsync is not null)
        {
            bool confirmed = await ConfirmRunAsync();
            if (!confirmed)
            {
                return;
            }
        }

        _commandService.Execute(
            _command,
            SelectedShell,
            Script,
            WorkingDirectory,
            action,
            RunAsAdministrator);

        SubscribeToRuns();
        await ThreadHelper.RunOnUIThreadAsync(NotifyUpdateUI);
    }

    private void SubscribeToRuns()
    {
        DisposeRunSubscriptions();

        foreach (CommandRun run in _command.Runs)
        {
            _runSubscriptions.Add(run.OutputLines.Subscribe(new OutputObserver(this)));
        }
    }

    private void DisposeRunSubscriptions()
    {
        foreach (IDisposable sub in _runSubscriptions)
        {
            sub.Dispose();
        }

        _runSubscriptions.Clear();
    }

    private void NotifyUpdateUI()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(OutputText));
        OnPropertyChanged(nameof(HasBeenExecuted));
        OnPropertyChanged(nameof(RunCount));
        OnPropertyChanged(nameof(LatestRun));
        OnPropertyChanged(nameof(LatestRunStartedAt));
        OnPropertyChanged(nameof(RunButtonLabel));
        OnPropertyChanged(nameof(HasOtherRuns));
        OnPropertyChanged(nameof(OtherRunsCount));
        CancelCommand.NotifyCanExecuteChanged();
        RunMenuCommand.NotifyCanExecuteChanged();
        RunAndAppendMenuCommand.NotifyCanExecuteChanged();
        RunAndCopyMenuCommand.NotifyCanExecuteChanged();
        RunAndReplaceMenuCommand.NotifyCanExecuteChanged();
    }

    private bool CanCancel() => State == CommandState.Running;

    private bool CanRun() => State != CommandState.Running;

    /// <summary>
    /// Syncs edits back to the model and updates the command title for other consumers.
    /// </summary>
    partial void OnScriptChanged(string? value)
    {
        _command.Script = value;
    }

    private sealed class OutputObserver(CommandViewModel vm) : IObserver<string>
    {
        public void OnNext(string value) => vm._throttledNotify.Invoke();

        public void OnCompleted() => ThreadHelper.RunOnUIThreadAsync(vm.NotifyUpdateUI).ForgetSafely();

        public void OnError(Exception error) => ThreadHelper.RunOnUIThreadAsync(vm.NotifyUpdateUI).ForgetSafely();
    }
}
