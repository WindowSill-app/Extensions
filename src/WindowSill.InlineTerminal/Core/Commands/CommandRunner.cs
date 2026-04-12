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
    private readonly Subject<CommandExecutionStatusChange> _subject = new();
    private readonly StringBuilder _outputStringBuilder = new();

    private bool _disposed;
    private Task? _onGoingCommandTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private CancellationToken _cancellationToken = CancellationToken.None;

    internal CommandRunner(
        IPluginInfo pluginInfo,
        IProcessInteractionService processInteractionService,
        WindowTextSelection? windowTextSelection,
        ShellInfo defaultShell,
        string? defaultWorkingDirectory,
        string? defaultScript,
        string? scriptFilePath)
    {
        if (string.IsNullOrEmpty(defaultScript) && string.IsNullOrEmpty(scriptFilePath))
        {
            throw new ArgumentException($"At least one of {nameof(defaultScript)} or {nameof(scriptFilePath)} must be provided.");
        }

        Id = Guid.NewGuid();
        _pluginInfo = pluginInfo;
        _processInteractionService = processInteractionService;
        WindowTextSelection = windowTextSelection;
        DefaultShell = defaultShell;
        DefaultWorkingDirectory = defaultWorkingDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        DefaultScript = defaultScript;
        ScriptFilePath = scriptFilePath;
    }

    /// <summary>
    /// Gets the unique identifier for this command runner instance.
    /// </summary>
    internal Guid Id { get; }

    internal WindowTextSelection? WindowTextSelection { get; }

    internal ShellInfo DefaultShell { get; }

    internal string DefaultWorkingDirectory { get; }

    internal string? DefaultScript { get; private set; }

    internal string? ScriptFilePath { get; }

    internal CommandState State { get; private set; } = CommandState.Created;

    internal string Output => _outputStringBuilder.ToString();

    internal ActionOnCommandCompleted ActionOnCommandCompleted { get; private set; }

    public void Dispose()
    {
        lock (_lock)
        {
            _disposed = true;
            _subject.Dispose();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }

    public IDisposable Subscribe(IObserver<CommandExecutionStatusChange> observer)
    {
        return _subject.Subscribe(observer);
    }

    internal void Start(ShellInfo shell, string? script, string workingDirectory, ActionOnCommandCompleted actionOnCompleted, bool asElevated)
    {
        Guard.IsNotNull(shell);

        lock (_lock)
        {
            if (_onGoingCommandTask is not null)
            {
                // Can't start two times.
                return;
            }

            bool isScriptFile = false;

            if (!string.IsNullOrEmpty(ScriptFilePath)
                && File.Exists(ScriptFilePath))
            {
                // Execute the script file by path rather than reading its contents,
                // so that batch variables like %~dp0 resolve to the script's directory.
                script = $"\"{ScriptFilePath}\"";
                isScriptFile = true;
            }

            if (string.IsNullOrEmpty(script))
            {
                // There's nothing to run.
                return;
            }

            if (!string.IsNullOrEmpty(DefaultScript))
            {
                DefaultScript = script;
            }

            // Reset cancellation token.
            Cancel();

            ActionOnCommandCompleted = actionOnCompleted;
            State = CommandState.Running;
            _outputStringBuilder.Clear();

            if (asElevated)
            {
                _onGoingCommandTask
                    = CommandExecutionHelper.ExecuteElevatedAsync(
                        script,
                        shell,
                        workingDirectory,
                        PropagateOnOutputLineReceived,
                        _pluginInfo,
                        _cancellationToken,
                        skipEscaping: isScriptFile);
            }
            else
            {
                _onGoingCommandTask
                    = CommandExecutionHelper.ExecuteAsync(
                        script,
                        shell,
                        workingDirectory,
                        PropagateOnOutputLineReceived,
                        _cancellationToken,
                        skipEscaping: isScriptFile);
            }

            ObserveOnGoingCommandTaskAsync().ForgetSafely();
        }
    }

    internal void Cancel()
    {
        lock (_lock)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
        }
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
            PropagateOnCompleted();
        }
        catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
        {
            PropagateOnCanceled();
        }
        catch (Exception ex)
        {
            PropagateOnError(ex);
        }

        await PerformActionOnCompleteAsync();
        _onGoingCommandTask = null;
    }

    private void PropagateOnOutputLineReceived(string outputLine)
    {
        lock (_lock)
        {
            if (!_disposed)
            {
                _outputStringBuilder.AppendLine(outputLine);
                State = CommandState.Running;
                _subject.OnNext(new CommandExecutionStatusChange(outputLine));
            }
        }
    }

    private void PropagateOnCanceled()
    {
        lock (_lock)
        {
            State = CommandState.Cancelled;
            _subject.OnNext(new CommandExecutionStatusChange());
            _subject.OnCompleted();
        }
    }

    private void PropagateOnCompleted()
    {
        lock (_lock)
        {
            State = CommandState.Completed;
            _subject.OnCompleted();
        }
    }

    private void PropagateOnError(Exception exception)
    {
        lock (_lock)
        {
            State = CommandState.Failed;
            _subject.OnError(exception);
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
        return DefaultScript ?? Path.GetFileName(ScriptFilePath) ?? string.Empty;
    }
}
