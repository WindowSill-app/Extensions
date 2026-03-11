using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;
using WindowSill.VideoHelper.Core;

namespace WindowSill.VideoHelper.Services;

/// <summary>
/// Represents a queue of video files to compress with the same options.
/// Each queue runs independently and owns its own cancellation lifecycle.
/// </summary>
internal sealed partial class VideoCompressionQueue : ObservableObject, IDisposable
{
    private readonly IVideoCompressor _compressor;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoCompressionQueue"/> class.
    /// </summary>
    /// <param name="files">Source video file paths to compress.</param>
    /// <param name="options">Compression options to apply to all files.</param>
    /// <param name="compressor">The video compressor implementation.</param>
    internal VideoCompressionQueue(
        IReadOnlyList<string> files,
        VideoCompressionOptions options,
        IVideoCompressor compressor)
    {
        _compressor = compressor;
        Options = options;

        for (int i = 0; i < files.Count; i++)
        {
            var task = new VideoCompressionTaskItem(files[i], options);
            task.PropertyChanged += OnTaskPropertyChanged;
            Tasks.Add(task);
        }

        if (files.Count == 1)
        {
            ProgressText = System.IO.Path.GetFileName(files[0]);
        }
        else
        {
            ProgressText = string.Format("/WindowSill.VideoHelper/Misc/CompressingFiles".GetLocalizedString(), files.Count);
        }
    }

    /// <summary>
    /// Gets the collection of individual file compression tasks.
    /// </summary>
    public ObservableCollection<VideoCompressionTaskItem> Tasks { get; } = [];

    /// <summary>
    /// Gets the compression options applied to this queue.
    /// </summary>
    public VideoCompressionOptions Options { get; }

    /// <summary>
    /// Gets or sets the overall compression progress across all tasks, from 0 to 100.
    /// </summary>
    [ObservableProperty]
    public partial double Progress { get; set; }

    /// <summary>
    /// Gets or sets a text displayed next to the progress bar/ring in the sill item.
    /// </summary>
    [ObservableProperty]
    public partial string ProgressText { get; set; }

    /// <summary>
    /// Gets or sets the current state of this compression queue.
    /// </summary>
    [ObservableProperty]
    public partial VideoCompressionQueueState State { get; set; } = VideoCompressionQueueState.Pending;

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
    /// Gets the output file paths for all successfully compressed files.
    /// </summary>
    public IReadOnlyList<string> OutputPaths
        => Tasks
            .Where(t => t.IsSucceeded && t.OutputPath is not null)
            .Select(t => t.OutputPath!)
            .ToList();

    /// <summary>
    /// Runs all compression tasks in the queue sequentially.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal async Task RunAsync()
    {
        CancellationToken cancellationToken = _cancellationTokenSource.Token;

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            State = VideoCompressionQueueState.InProgress;
        });

        try
        {
            for (int i = 0; i < Tasks.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                VideoCompressionTaskItem task = Tasks[i];
                await ThreadHelper.RunOnUIThreadAsync(() =>
                {
                    task.IsRunning = true;
                });

                await task.CompressAsync(_compressor, cancellationToken);

                await ThreadHelper.RunOnUIThreadAsync(() =>
                {
                    if (task.IsSucceeded)
                    {
                        SucceededCount++;
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
            State = cancellationToken.IsCancellationRequested
                ? VideoCompressionQueueState.Failed
                : VideoCompressionQueueState.Completed;

            ProgressText = string.Format("/WindowSill.VideoHelper/Misc/CompressionCompleted".GetLocalizedString());
        });
    }

    /// <summary>
    /// Cancels any remaining compression tasks in the queue.
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
        if (e.PropertyName == nameof(VideoCompressionTaskItem.Progress))
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
