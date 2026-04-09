using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using WindowSill.API;
using WindowSill.InlineTerminal.Core.Commands;
using WindowSill.InlineTerminal.Core.Shell;
using WindowSill.InlineTerminal.Messages;
using WindowSill.InlineTerminal.Views;
using Path = System.IO.Path;

namespace WindowSill.InlineTerminal.ViewModels;

internal sealed partial class CommandViewModel : ObservableObject, IObserver<CommandExecutionStatusChange>, IDisposable
{
    private readonly IMessenger _messenger;
    private readonly CommandExecutionService _commandExecutionService;
    private readonly WindowTextSelection? _windowTextSelection;

    private SillPopup? _popup;
    private CommandRunner? _commandRunner;
    private IDisposable? _unsubscriber;

    internal CommandViewModel(
        IMessenger messenger,
        CommandExecutionService commandExecutionService,
        CommandRunner commandRunner)
    {
        _messenger = messenger;
        _commandExecutionService = commandExecutionService;
        AvailableShells = Array.Empty<ShellInfo>();
        Title = commandRunner.Script ?? Path.GetFileName(commandRunner.ScriptFilePath!);
        SelectedShell = commandRunner.SelectedShell;
        Script = commandRunner.Script;
        WorkingDirectory = commandRunner.WorkingDirectory;
        _commandRunner = commandRunner;
        _unsubscriber = _commandRunner.Subscribe(this);
    }

    internal CommandViewModel(
        IMessenger messenger,
        CommandExecutionService commandExecutionService,
        WindowTextSelection? windowTextSelection,
        IReadOnlyList<ShellInfo> shells,
        ShellInfo? preferredShell,
        string? workingDirectory,
        string? script,
        string? scriptFilePath)
    {
        if (string.IsNullOrEmpty(script) && string.IsNullOrEmpty(scriptFilePath))
        {
            throw new ArgumentException($"At least one of {nameof(script)} or {nameof(scriptFilePath)} must be provided.");
        }

        _messenger = messenger;
        _commandExecutionService = commandExecutionService;
        _windowTextSelection = windowTextSelection;
        AvailableShells = shells;
        Title = script ?? Path.GetFileName(scriptFilePath!);
        SelectedShell = preferredShell ?? shells.FirstOrDefault();
        Script = script;
        ScriptFilePath = scriptFilePath;
        WorkingDirectory = workingDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    internal SillViewBase? SillView { get; set; }

    internal CommandState State => _commandRunner?.State ?? CommandState.Pending;

    internal IReadOnlyList<ShellInfo> AvailableShells { get; }

    internal string OutputText => _commandRunner?.Output ?? string.Empty;

    [ObservableProperty]
    internal partial string Title { get; set; }

    [ObservableProperty]
    internal partial ShellInfo? SelectedShell { get; set; }

    [ObservableProperty]
    internal partial string? Script { get; set; }

    [ObservableProperty]
    internal partial string? ScriptFilePath { get; set; }

    [ObservableProperty]
    internal partial string WorkingDirectory { get; set; }

    [ObservableProperty]
    internal partial bool RunAsAdministrator { get; set; }

    [RelayCommand]
    internal void Dismiss()
    {
        _unsubscriber?.Dispose();
        if (_commandRunner is not null)
        {
            _commandExecutionService.DestroyRunner(_commandRunner);
        }

        _commandRunner = null;
        OnPropertyChanged(nameof(State));

        _messenger.Send(new CommandPopupDismissMessage());
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    internal async Task ConfigureRunAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            try
            {
                Guard.IsNotNull(SillView);
                if (_popup is null)
                {
                    _popup = new SillPopup
                    {
                        Content = new CommandPopup(_messenger, this)
                    };
                }

                await _popup.ShowAsync(SillView);
            }
            catch (Exception ex)
            {
            }
        });
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

    [RelayCommand(CanExecute = nameof(CanCancel))]
    internal void Cancel()
    {
        _commandRunner?.Cancel();
        OnPropertyChanged(nameof(State));
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    internal async Task CopyOutputAsync()
    {
        if (_commandRunner is not null)
        {
            await _commandRunner.CopyOutputToClipboardAsync(includeInClipboardHistory: true);
        }
    }

    private bool CanCancel()
    {
        return State == CommandState.Running;
    }

    private bool CanRun()
    {
        return State != CommandState.Running;
    }

    private async Task StartRunScriptAsync(ActionOnCommandCompleted actionOnCommandCompleted)
    {
        if (_commandRunner is not null)
        {
            _unsubscriber?.Dispose();
            _commandExecutionService.DestroyRunner(_commandRunner);
        }

        (_commandRunner, _unsubscriber)
            = await _commandExecutionService.CreateAndStartRunnerAsync(
                 this,
                 _windowTextSelection,
                 SelectedShell,
                 WorkingDirectory,
                 Script,
                 ScriptFilePath,
                 actionOnCommandCompleted,
                 asElevated: RunAsAdministrator);

        await ThreadHelper.RunOnUIThreadAsync(NotifyUpdateUI);
    }

    public void OnCompleted()
    {
        ThreadHelper.RunOnUIThreadAsync(NotifyUpdateUI);
    }

    public void OnError(Exception error)
    {
        ThreadHelper.RunOnUIThreadAsync(NotifyUpdateUI);
    }

    public void OnNext(CommandExecutionStatusChange value)
    {
        ThreadHelper.RunOnUIThreadAsync(NotifyUpdateUI);
    }

    public void Dispose()
    {
        _unsubscriber?.Dispose();
    }

    private void NotifyUpdateUI()
    {
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(OutputText));
        RunMenuCommand.NotifyCanExecuteChanged();
        RunAndCopyMenuCommand.NotifyCanExecuteChanged();
        RunAndAppendMenuCommand.NotifyCanExecuteChanged();
        RunAndReplaceMenuCommand.NotifyCanExecuteChanged();
    }
}
