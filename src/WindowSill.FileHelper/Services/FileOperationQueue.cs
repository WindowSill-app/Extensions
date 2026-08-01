using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;
using WindowSill.FileHelper.Core;

namespace WindowSill.FileHelper.Services;

/// <summary>
/// Represents a queue of file operations that runs them one at a time.
/// Each queue runs independently and owns its own cancellation lifecycle, and persists in
/// <see cref="IFileOperationService.Queues"/> independently of any popup or file selection.
/// </summary>
internal sealed partial class FileOperationQueue : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FileOperationQueue"/> class.
    /// </summary>
    /// <param name="operations">The operations to run, in order.</param>
    /// <param name="progressText">
    /// Label shown next to the queue's progress in the sill (e.g. "Converting 3 files", "Merging 3 files"). Supplied
    /// by the caller because only it knows which action the queue represents.
    /// </param>
    internal FileOperationQueue(IReadOnlyList<IFileOperation> operations, string progressText)
    {
        for (int i = 0; i < operations.Count; i++)
        {
            var task = new FileOperationTaskItem(operations[i]);
            task.PropertyChanged += OnTaskPropertyChanged;
            Tasks.Add(task);
        }

        ProgressText = progressText;
    }

    /// <summary>
    /// Gets the collection of individual operation tasks.
    /// </summary>
    public ObservableCollection<FileOperationTaskItem> Tasks { get; } = [];

    /// <summary>
    /// Gets or sets the overall conversion progress across all tasks, from 0 to 100.
    /// </summary>
    [ObservableProperty]
    public partial double Progress { get; set; }

    /// <summary>
    /// Gets or sets a text displayed next to the progress bar/ring in the sill item.
    /// </summary>
    [ObservableProperty]
    public partial string ProgressText { get; set; }

    /// <summary>
    /// Gets or sets the current state of this conversion queue.
    /// </summary>
    [ObservableProperty]
    public partial FileOperationQueueState State { get; set; } = FileOperationQueueState.Pending;

    /// <summary>
    /// Gets the number of tasks that have completed successfully.
    /// </summary>
    [ObservableProperty]
    public partial int SucceededCount { get; set; }

    /// <summary>
    /// Gets the number of tasks that have failed.
    /// </summary>
    [ObservableProperty]
    public partial int FailedCount { get; set; }

    /// <summary>
    /// Gets the number of tasks that were canceled (either interrupted mid-conversion, or never started because
    /// the queue was cancelled before reaching them). Canceled tasks are not counted in <see cref="FailedCount"/>.
    /// </summary>
    [ObservableProperty]
    public partial int CanceledCount { get; set; }

    /// <summary>
    /// Gets the output file paths for all successfully converted files.
    /// </summary>
    public IReadOnlyList<string> OutputPaths
        => Tasks
            .Where(t => t.IsSucceeded && t.OutputPath is not null)
            .Select(t => t.OutputPath!)
            .ToList();

    /// <summary>
    /// Runs all conversion tasks in the queue sequentially.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal async Task RunAsync()
    {
        CancellationToken cancellationToken = _cancellationTokenSource.Token;

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            State = FileOperationQueueState.InProgress;
        });

        int nextTaskIndex = 0;

        try
        {
            for (; nextTaskIndex < Tasks.Count; nextTaskIndex++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                FileOperationTaskItem task = Tasks[nextTaskIndex];
                await ThreadHelper.RunOnUIThreadAsync(() =>
                {
                    task.IsRunning = true;
                });

                await task.RunAsync(cancellationToken);

                await ThreadHelper.RunOnUIThreadAsync(() =>
                {
                    if (task.IsSucceeded)
                    {
                        SucceededCount++;
                    }
                    else if (task.IsCanceled)
                    {
                        CanceledCount++;
                    }
                    else
                    {
                        FailedCount++;
                    }
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Queue was cancelled
        }

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            FinalizeState(nextTaskIndex);
        });
    }

    /// <summary>
    /// Finalizes the queue's terminal state once the run loop in <see cref="RunAsync"/> stops, whether it
    /// processed every task or stopped early due to a cancellation request. Must be called on the UI thread.
    /// </summary>
    /// <remarks>
    /// Any tasks from <paramref name="nextTaskIndex"/> onward never got a chance to run (because the loop
    /// stopped early due to cancellation) and are explicitly marked canceled here — rather than left in their
    /// initial "not started" state forever — so they are reflected in <see cref="CanceledCount"/> and their
    /// progress contributes 100% to the aggregate, letting the queue's overall <see cref="Progress"/> reach
    /// 100% once it reaches a terminal state.
    /// <para>
    /// The queue only ends up in the <see cref="FileOperationQueueState.Canceled"/> state when at least one
    /// task was actually canceled or left unprocessed by the loop above. A cancellation request that arrives
    /// after every task already finished processing (a "late" cancel) has nothing left to cancel, so the queue
    /// still reports <see cref="FileOperationQueueState.Completed"/> in that case.
    /// </para>
    /// </remarks>
    /// <param name="nextTaskIndex">Index of the first task, if any, that never got a chance to run.</param>
    internal void FinalizeState(int nextTaskIndex)
    {
        for (int i = nextTaskIndex; i < Tasks.Count; i++)
        {
            Tasks[i].MarkCanceled();
            CanceledCount++;
        }

        bool wasCanceled = CanceledCount > 0;
        State = wasCanceled
            ? FileOperationQueueState.Canceled
            : FileOperationQueueState.Completed;

        ProgressText = wasCanceled
            ? "/WindowSill.FileHelper/Misc/ConversionCanceled".GetLocalizedString()
            : "/WindowSill.FileHelper/Misc/ConversionCompleted".GetLocalizedString();
    }

    /// <summary>
    /// Cancels any remaining conversion tasks in the queue.
    /// </summary>
    internal void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        for (int i = 0; i < Tasks.Count; i++)
        {
            Tasks[i].PropertyChanged -= OnTaskPropertyChanged;
        }

        _cancellationTokenSource.Dispose();
    }

    private void OnTaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileOperationTaskItem.Progress))
        {
            double total = 0;
            for (int i = 0; i < Tasks.Count; i++)
            {
                total += Tasks[i].Progress;
            }

            Progress = Math.Clamp(total / Tasks.Count * 100.0, 0, 100);
        }
    }
}
