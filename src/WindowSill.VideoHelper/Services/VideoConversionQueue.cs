using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;
using WindowSill.VideoHelper.Core;

namespace WindowSill.VideoHelper.Services;

/// <summary>
/// Represents a queue of video files to convert with the same options.
/// Each queue runs independently and owns its own cancellation lifecycle.
/// </summary>
internal sealed partial class VideoConversionQueue : ObservableObject, IDisposable
{
    private readonly IVideoConverter _converter;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoConversionQueue"/> class.
    /// </summary>
    /// <param name="files">Source video file paths to convert.</param>
    /// <param name="options">Conversion options to apply to all files.</param>
    /// <param name="converter">The video converter implementation.</param>
    internal VideoConversionQueue(
        IReadOnlyList<string> files,
        VideoConversionOptions options,
        IVideoConverter converter)
    {
        _converter = converter;
        Options = options;

        for (int i = 0; i < files.Count; i++)
        {
            var task = new VideoConversionTaskItem(files[i], options);
            task.PropertyChanged += OnTaskPropertyChanged;
            Tasks.Add(task);
        }

        if (files.Count == 1)
        {
            ProgressText = System.IO.Path.GetFileName(files[0]);
        }
        else
        {
            ProgressText = string.Format("/WindowSill.VideoHelper/Misc/ConvertingFiles".GetLocalizedString(), files.Count);
        }
    }

    /// <summary>
    /// Gets the collection of individual file conversion tasks.
    /// </summary>
    public ObservableCollection<VideoConversionTaskItem> Tasks { get; } = [];

    /// <summary>
    /// Gets the conversion options applied to this queue.
    /// </summary>
    public VideoConversionOptions Options { get; }

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
    public partial VideoConversionQueueState State { get; set; } = VideoConversionQueueState.Pending;

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
            State = VideoConversionQueueState.InProgress;
        });

        try
        {
            for (int i = 0; i < Tasks.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                VideoConversionTaskItem task = Tasks[i];
                await ThreadHelper.RunOnUIThreadAsync(() =>
                {
                    task.IsRunning = true;
                });

                await task.ConvertAsync(_converter, cancellationToken);

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
                ? VideoConversionQueueState.Failed
                : VideoConversionQueueState.Completed;

            ProgressText = string.Format("/WindowSill.VideoHelper/Misc/ConversionCompleted".GetLocalizedString());
        });
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
        if (e.PropertyName == nameof(VideoConversionTaskItem.Progress))
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
