using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowSill.InlineTerminal.Models;

/// <summary>
/// Represents a single execution (run) of a <see cref="CommandDefinition"/>.
/// Tracks state, output, and timing for one invocation.
/// </summary>
internal sealed partial class CommandRun : ObservableObject, IDisposable
{
    private readonly Lock _lock = new();
    private readonly Subject<string> _outputSubject = new();
    private readonly StringBuilder _outputBuilder = new();

    private bool _disposed;

    internal CommandRun()
    {
        Id = Guid.NewGuid();
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the unique identifier for this run.
    /// </summary>
    internal Guid Id { get; }

    /// <summary>
    /// Gets or sets the current execution state.
    /// </summary>
    [ObservableProperty]
    internal partial CommandState State { get; set; } = CommandState.Running;

    /// <summary>
    /// Gets the accumulated output text.
    /// </summary>
    internal string Output
    {
        get
        {
            lock (_lock)
            {
                return _outputBuilder.ToString();
            }
        }
    }

    /// <summary>
    /// Gets the first line of output, trimmed, for display previews.
    /// </summary>
    internal string OutputTrimmed => Output.Trim();

    /// <summary>
    /// Gets the timestamp when this run was started.
    /// </summary>
    internal DateTime StartedAt { get; }

    /// <summary>
    /// Gets or sets the timestamp when this run completed.
    /// </summary>
    internal DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets the observable stream of output lines for live UI updates.
    /// </summary>
    internal IObservable<string> OutputLines => _outputSubject;

    /// <summary>
    /// Gets or sets whether this run is pinned (exempt from auto-dismiss).
    /// </summary>
    [ObservableProperty]
    internal partial bool IsPinned { get; set; }

    /// <summary>
    /// Gets or sets the action to perform when the command completes.
    /// </summary>
    internal ActionOnCommandCompleted ActionOnCompleted { get; set; }

    /// <summary>
    /// Appends a line of output and broadcasts it to subscribers.
    /// </summary>
    internal void AppendOutput(string line)
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _outputBuilder.AppendLine(line);
        }

        _outputSubject.OnNext(line);
    }

    /// <summary>
    /// Marks the run as completed with the specified final state.
    /// </summary>
    internal void Complete(CommandState finalState)
    {
        State = finalState;
        CompletedAt = DateTime.UtcNow;

        if (finalState == CommandState.Failed)
        {
            _outputSubject.OnError(new InvalidOperationException("Command failed."));
        }
        else
        {
            _outputSubject.OnCompleted();
        }

        OnPropertyChanged(nameof(Output));
        OnPropertyChanged(nameof(OutputTrimmed));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lock)
        {
            _disposed = true;
            _outputSubject.Dispose();
        }
    }
}
