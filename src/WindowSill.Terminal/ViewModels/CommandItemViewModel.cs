using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowSill.API;
using WindowSill.Terminal.Core;

namespace WindowSill.Terminal.ViewModels;

/// <summary>
/// Holds the shared state for a single command across all popup pages.
/// </summary>
internal sealed partial class CommandItemViewModel : ObservableObject
{
    private const int MaxDisplayedLines = 500;

    private readonly ICommandExecutionService _commandExecutionService;
    private readonly List<string> _allOutputLines = [];
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _showFullOutput;

    internal CommandItemViewModel(
        string commandText,
        IReadOnlyList<ShellInfo> availableShells,
        ICommandExecutionService commandExecutionService)
    {
        CommandText = commandText;
        AvailableShells = availableShells;
        SelectedShell = availableShells[0];
        WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _commandExecutionService = commandExecutionService;
    }

    [ObservableProperty]
    public partial string CommandText { get; set; }

    [ObservableProperty]
    public partial string WorkingDirectory { get; set; }

    [ObservableProperty]
    public partial ShellInfo SelectedShell { get; set; }

    /// <summary>Gets or sets whether to run the command as administrator in an external terminal.</summary>
    [ObservableProperty]
    public partial bool RunAsAdministrator { get; set; }

    /// <summary>Gets the list of shells available on the system.</summary>
    public IReadOnlyList<ShellInfo> AvailableShells { get; }

    [ObservableProperty]
    public partial CommandState State { get; set; } = CommandState.Pending;

    [ObservableProperty]
    public partial int? ExitCode { get; set; }

    [ObservableProperty]
    public partial string ElapsedTimeText { get; set; } = "0:00";

    [ObservableProperty]
    public partial string OutputText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsOutputCapped { get; set; }

    /// <summary>Gets a display string for the exit code and state.</summary>
    public string ExitCodeDisplayText => State switch
    {
        CommandState.Completed => "✅ Exit 0",
        CommandState.Failed => $"❌ Exit {ExitCode}",
        CommandState.Cancelled => "⊘ Cancelled",
        _ => string.Empty
    };

    /// <summary>Raised when execution starts (navigate Configure → Execution).</summary>
    internal event EventHandler? ExecutionStarted;

    /// <summary>Raised when execution completes (navigate Execution → Result).</summary>
    internal event EventHandler? ExecutionCompleted;

    /// <summary>Raised when the user clicks Dismiss.</summary>
    internal event EventHandler? DismissRequested;

    /// <summary>Raised when Re-run is clicked (navigate back to Execution).</summary>
    internal event EventHandler? RerunRequested;

    partial void OnStateChanged(CommandState value)
    {
        OnPropertyChanged(nameof(ExitCodeDisplayText));
    }

    [RelayCommand]
    private async Task RunAsync()
    {
        State = CommandState.Running;
        _allOutputLines.Clear();
        OutputText = string.Empty;
        _showFullOutput = false;
        IsOutputCapped = false;
        ExitCode = null;

        ExecutionStarted?.Invoke(this, EventArgs.Empty);

        _cancellationTokenSource = new CancellationTokenSource();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _ = UpdateElapsedTimeAsync(stopwatch, _cancellationTokenSource.Token);

        try
        {
            int exitCode = RunAsAdministrator
                ? await _commandExecutionService.ExecuteElevatedAsync(
                    CommandText,
                    SelectedShell,
                    WorkingDirectory,
                    OnOutputLine,
                    _cancellationTokenSource.Token)
                : await _commandExecutionService.ExecuteAsync(
                    CommandText,
                    SelectedShell,
                    WorkingDirectory,
                    OnOutputLine,
                    _cancellationTokenSource.Token);

            stopwatch.Stop();
            ExitCode = exitCode;
            State = exitCode == 0 ? CommandState.Completed : CommandState.Failed;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            State = CommandState.Cancelled;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // User declined the UAC prompt.
            stopwatch.Stop();
            State = CommandState.Cancelled;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            State = CommandState.Failed;
            ExitCode = -1;
        }
        finally
        {
            ElapsedTimeText = FormatElapsed(stopwatch.Elapsed);
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            ExecutionCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    [RelayCommand]
    private void CopyOutput()
    {
        var sb = new StringBuilder();
        foreach (string line in _allOutputLines)
        {
            sb.AppendLine(line);
        }

        var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
        package.SetText(sb.ToString());
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
    }

    [RelayCommand]
    private async Task RerunAsync()
    {
        RerunRequested?.Invoke(this, EventArgs.Empty);
        await RunAsync();
    }

    [RelayCommand]
    private void Dismiss()
    {
        _cancellationTokenSource?.Cancel();
        DismissRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ShowFullOutput()
    {
        _showFullOutput = true;
        RefreshDisplayedOutput();
    }

    private void OnOutputLine(string line)
    {
        _allOutputLines.Add(line);
        ThreadHelper.RunOnUIThreadAsync(RefreshDisplayedOutput).ForgetSafely();
    }

    private void RefreshDisplayedOutput()
    {
        int startIndex = 0;
        bool capped = _allOutputLines.Count > MaxDisplayedLines && !_showFullOutput;

        if (capped)
        {
            startIndex = _allOutputLines.Count - MaxDisplayedLines;
        }

        IsOutputCapped = capped;

        var sb = new StringBuilder();
        for (int i = startIndex; i < _allOutputLines.Count; i++)
        {
            sb.AppendLine(_allOutputLines[i]);
        }

        OutputText = sb.ToString();
    }

    private async Task UpdateElapsedTimeAsync(System.Diagnostics.Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && State == CommandState.Running)
            {
                await Task.Delay(1000, cancellationToken);
                ElapsedTimeText = FormatElapsed(stopwatch.Elapsed);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled.
        }
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        return elapsed.TotalHours >= 1
            ? elapsed.ToString(@"h\:mm\:ss")
            : elapsed.ToString(@"m\:ss");
    }
}
