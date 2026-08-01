using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;
using WindowSill.FileHelper.Core;

namespace WindowSill.FileHelper.Services;

/// <summary>
/// Represents a single unit of work in a queue, with observable progress and outcome.
/// </summary>
internal sealed partial class FileOperationTaskItem : ObservableObject
{
    private readonly IFileOperation _operation;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileOperationTaskItem"/> class.
    /// </summary>
    /// <param name="operation">The work this task runs.</param>
    internal FileOperationTaskItem(IFileOperation operation)
    {
        _operation = operation;
        FileName = operation.DisplayName;
    }

    /// <summary>
    /// Gets the display name of this task — a file name for per-file work, or a summary of the whole selection.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this task is currently running.
    /// </summary>
    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this task completed successfully.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSucceeded { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this task failed.
    /// </summary>
    [ObservableProperty]
    public partial bool IsFailed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this task was canceled before it could complete.
    /// </summary>
    [ObservableProperty]
    public partial bool IsCanceled { get; set; }

    /// <summary>
    /// Gets or sets the conversion progress from 0.0 to 1.0.
    /// </summary>
    [ObservableProperty]
    public partial double Progress { get; set; }

    /// <summary>
    /// Gets the output file path after the operation completes. Null if not yet completed successfully.
    /// </summary>
    public string? OutputPath { get; private set; }

    /// <summary>
    /// Gets or sets an actionable, user-facing description of why this task failed. Null when the task hasn't
    /// failed, or was canceled rather than failed (cancellation is a deliberate, non-error outcome and doesn't
    /// need an explanation).
    /// </summary>
    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    /// <summary>
    /// Marks this task as canceled without ever having started, e.g. because the owning queue stopped processing
    /// further tasks after a cancellation request. Forces <see cref="Progress"/> to 1.0 so the queue's aggregate
    /// progress can reach 100% once the queue reaches a terminal state.
    /// </summary>
    internal void MarkCanceled()
    {
        IsRunning = false;
        IsSucceeded = false;
        IsFailed = false;
        IsCanceled = true;
        Progress = 1.0;
    }

    /// <summary>
    /// Runs the operation this task represents.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal async Task RunAsync(CancellationToken cancellationToken)
    {
        string? actualOutputPath = null;
        string? errorMessage = null;
        bool wasCanceled = false;

        try
        {
            var progress = new Progress<double>(p =>
            {
                ThreadHelper.RunOnUIThreadAsync(() =>
                {
                    Progress = p;
                }).ForgetSafely();
            });

            actualOutputPath = await _operation.ExecuteAsync(progress, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cancelled — not a failure, just stopped
            wasCanceled = true;
        }
        catch (Exception ex)
        {
            // The operation failed for this item (e.g. a corrupt, password-protected, or already-optimized
            // document). Capture an actionable reason so the UI can surface it per-file instead of the task
            // silently/inertly disappearing into a generic failure with no explanation.
            errorMessage = ex.Message;
        }

        bool isSucceeded = actualOutputPath is not null;
        if (isSucceeded)
        {
            OutputPath = actualOutputPath;
        }

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            IsRunning = false;
            IsSucceeded = isSucceeded;
            IsCanceled = !isSucceeded && (wasCanceled || cancellationToken.IsCancellationRequested);
            IsFailed = !isSucceeded && !IsCanceled;
            ErrorMessage = IsFailed ? errorMessage : null;

            // Regardless of outcome, this task has reached a terminal state: force its progress to 100% so the
            // queue's aggregate progress (sum of all task progresses / task count) can reach 100% once every
            // task is done, instead of stalling below 100% whenever any task fails or is canceled without ever
            // reporting Progress == 1.0 itself.
            Progress = 1.0;
        });
    }
}
