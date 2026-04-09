using System.Diagnostics;
using System.Text;
using CommunityToolkit.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using WindowSill.API;
using WindowSill.InlineTerminal.Core.Shell;
using Path = System.IO.Path;

namespace WindowSill.InlineTerminal.Core.Commands;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal sealed class CommandRunner : IObservable<CommandExecutionStatusChange>, IDisposable
{
    private readonly Lock _lock = new();
    private readonly IPluginInfo _pluginInfo;
    private readonly IProcessInteractionService _processInteractionService;
    private readonly HashSet<IObserver<CommandExecutionStatusChange>> _observers = [];
    private readonly HashSet<Unsubscriber> _unsubscribers = [];
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly CancellationToken _cancellationToken;
    private readonly StringBuilder _outputStringBuilder = new();

    private bool _disposed;
    private Task? _onGoingCommandTask;

    internal CommandRunner(
        IPluginInfo pluginInfo,
        IProcessInteractionService processInteractionService,
        WindowTextSelection? windowTextSelection,
        IReadOnlyList<ShellInfo> shells,
        ShellInfo? preferredShell,
        string? workingDirectory,
        string? terminalCommand,
        string? filePath)
    {
        if (string.IsNullOrEmpty(terminalCommand) && string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException($"At least one of {nameof(terminalCommand)} or {nameof(filePath)} must be provided.");
        }

        if (shells.Count == 0)
        {
            throw new ArgumentOutOfRangeException($"At least one shell must be present in {nameof(shells)}.");
        }

        _pluginInfo = pluginInfo;
        _processInteractionService = processInteractionService;
        _cancellationToken = _cancellationTokenSource.Token;
        WindowTextSelection = windowTextSelection;
        AvailableShells = shells;
        SelectedShell = preferredShell ?? shells.First();
        WorkingDirectory = workingDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Script = terminalCommand;
        ScriptFilePath = filePath;
    }

    internal WindowTextSelection? WindowTextSelection { get; }

    internal IReadOnlyList<ShellInfo> AvailableShells { get; }

    internal ShellInfo SelectedShell { get; }

    internal string WorkingDirectory { get; }

    internal string? Script { get; }

    internal string? ScriptFilePath { get; }

    internal CommandState State { get; private set; } = CommandState.Pending;

    internal string Output => _outputStringBuilder.ToString();

    internal ActionOnCommandCompleted ActionOnCommandCompleted { get; private set; }

    public void Dispose()
    {
        lock (_lock)
        {
            _disposed = true;
            Unsubscriber[] unsubscribers = _unsubscribers.ToArray();
            for (int i = 0; i < unsubscribers.Length; i++)
            {
                unsubscribers[i].Dispose();
            }

            Guard.HasSizeEqualTo(_observers.ToArray(), 0);

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }

    public IDisposable Subscribe(IObserver<CommandExecutionStatusChange> observer)
    {
        lock (_lock)
        {
            if (_observers.Add(observer))
            {
                var unsubscriber = new Unsubscriber(_lock, _observers, _unsubscribers, observer);
                _unsubscribers.Add(unsubscriber);
                return unsubscriber;
            }

            throw new InvalidOperationException($"This observer is already registered.");
        }
    }

    internal void Start(ActionOnCommandCompleted actionOnCompleted, bool asElevated)
    {
        lock (_lock)
        {
            if (_onGoingCommandTask is not null)
            {
                // Can't start two times.
                return;
            }

            if (_observers.Count == 0)
            {
                throw new InvalidOperationException("At least one observer must be subscribed before starting the command runner.");
            }

            string? script;
            if (!string.IsNullOrEmpty(ScriptFilePath)
                && File.Exists(ScriptFilePath))
            {
                script = File.ReadAllText(ScriptFilePath);
            }
            else
            {
                script = Script;
            }

            if (string.IsNullOrEmpty(script))
            {
                // There's nothing to run.
                return;
            }

            ActionOnCommandCompleted = actionOnCompleted;
            State = CommandState.Running;

            if (asElevated)
            {
                _onGoingCommandTask
                    = CommandExecutionHelper.ExecuteElevatedAsync(
                        script,
                        SelectedShell,
                        WorkingDirectory,
                        PropagateOnOutputLineReceived,
                        _pluginInfo,
                        _cancellationToken);
            }
            else
            {
                _onGoingCommandTask
                    = CommandExecutionHelper.ExecuteAsync(
                        script,
                        SelectedShell,
                        WorkingDirectory,
                        PropagateOnOutputLineReceived,
                        _cancellationToken);
            }

            ObserveOnGoingCommandTaskAsync().ForgetSafely();
        }
    }

    internal void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    internal async Task CopyOutputToClipboardAsync(bool includeInClipboardHistory)
    {
        var dataPackage = new DataPackage();
        if (includeInClipboardHistory)
        {
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
        }
        else
        {
            dataPackage.RequestedOperation = DataPackageOperation.Move;
        }

        dataPackage.SetText(Output);

        Clipboard.SetContentWithOptions(
            dataPackage,
            new ClipboardContentOptions()
            {
                IsAllowedInHistory = includeInClipboardHistory,
                IsRoamable = includeInClipboardHistory
            });
        Clipboard.Flush();
    }

    private async Task ObserveOnGoingCommandTaskAsync()
    {
        Guard.IsNotNull(_onGoingCommandTask);

        try
        {
            await _onGoingCommandTask.ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
        {
            PropagateOnCanceled();
        }
        catch (Exception ex)
        {
            PropagateOnError(ex);
        }

        PropagateOnCompleted();

        await PerformActionOnCompleteAsync();
    }

    private void PropagateOnOutputLineReceived(string outputLine)
    {
        lock (_lock)
        {
            if (!_disposed)
            {
                _outputStringBuilder.AppendLine(outputLine);
                State = CommandState.Running;
                var argument = new CommandExecutionStatusChange(outputLine);
                foreach (IObserver<CommandExecutionStatusChange> observer in _observers)
                {
                    observer.OnNext(argument);
                }
            }
        }
    }

    private void PropagateOnCanceled()
    {
        lock (_lock)
        {
            State = CommandState.Cancelled;
            var argument = new CommandExecutionStatusChange();
            foreach (IObserver<CommandExecutionStatusChange> observer in _observers)
            {
                observer.OnNext(argument);
            }
        }
    }

    private void PropagateOnCompleted()
    {
        lock (_lock)
        {
            State = CommandState.Completed;
            foreach (IObserver<CommandExecutionStatusChange> observer in _observers)
            {
                observer.OnCompleted();
            }
        }
    }

    private void PropagateOnError(Exception exception)
    {
        lock (_lock)
        {
            State = CommandState.Failed;
            foreach (IObserver<CommandExecutionStatusChange> observer in _observers)
            {
                observer.OnError(exception);
            }
        }
    }
    private async Task PerformActionOnCompleteAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            var clipboardBackup = new Dictionary<string, object>();
            if (ActionOnCommandCompleted == ActionOnCommandCompleted.AppendSelection || ActionOnCommandCompleted == ActionOnCommandCompleted.ReplaceSelection)
            {
                DataPackageView clipboardContent = Clipboard.GetContent();
                foreach (string? format in clipboardContent.AvailableFormats)
                {
                    try
                    {
                        clipboardBackup[format] = await clipboardContent.GetDataAsync(format);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            try
            {
                await CopyOutputToClipboardAsync(includeInClipboardHistory: ActionOnCommandCompleted == ActionOnCommandCompleted.Copy);

                await Task.Delay(200); // Wait for the clipboard to be set.

                if (WindowTextSelection is not null
                    && (ActionOnCommandCompleted == ActionOnCommandCompleted.AppendSelection
                        || ActionOnCommandCompleted == ActionOnCommandCompleted.ReplaceSelection))
                {
                    if (ActionOnCommandCompleted == ActionOnCommandCompleted.AppendSelection)
                    {
                        // Simulate Right key to move the cursor to the end of the selected text,
                        // then Enter key to add new lines.
                        await _processInteractionService.SimulateKeysOnWindow(
                            WindowTextSelection,
                            [
                                VirtualKey.Right,
                                VirtualKey.Enter,
                                VirtualKey.Enter,
                            ]);

                        await Task.Delay(200);
                    }

                    // Simulate Ctrl+V to paste the new text into the window.
                    await _processInteractionService.SimulateKeysOnWindow(
                        WindowTextSelection,
                        [
                            VirtualKey.LeftControl,
                            VirtualKey.V
                        ]);

                    await Task.Delay(200);
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                if (ActionOnCommandCompleted == ActionOnCommandCompleted.AppendSelection || ActionOnCommandCompleted == ActionOnCommandCompleted.ReplaceSelection)
                {
                    // Restore the clipboard content.
                    var dataPackage = new DataPackage();
                    foreach (KeyValuePair<string, object> item in clipboardBackup)
                    {
                        try
                        {
                            dataPackage.SetData(item.Key, item.Value);
                        }
                        catch (Exception ex)
                        {
                        }
                    }

                    Clipboard.SetContent(dataPackage);
                }
            }
        });
    }

    private string GetDebuggerDisplay()
    {
        return Script ?? Path.GetFileName(ScriptFilePath) ?? string.Empty;
    }

    private sealed class Unsubscriber(
        Lock syncLock,
        HashSet<IObserver<CommandExecutionStatusChange>> observers,
        HashSet<Unsubscriber> unsubscribers,
        IObserver<CommandExecutionStatusChange> observer)
        : IDisposable
    {
        public void Dispose()
        {
            lock (syncLock)
            {
                observers.Remove(observer);
                unsubscribers.Remove(this);
            }
        }
    }
}
