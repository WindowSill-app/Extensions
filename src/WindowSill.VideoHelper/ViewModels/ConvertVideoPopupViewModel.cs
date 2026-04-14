using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage;
using WindowSill.API;
using WindowSill.VideoHelper.Core;
using WindowSill.VideoHelper.Services;

namespace WindowSill.VideoHelper.ViewModels;

/// <summary>
/// ViewModel for the ConvertVideoPopup, managing format selection,
/// progress tracking, and result display.
/// </summary>
internal sealed partial class ConvertVideoPopupViewModel : ObservableObject
{
    private readonly IVideoConversionService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConvertVideoPopupViewModel"/> class
    /// for starting a new conversion.
    /// </summary>
    /// <param name="files">Video files to convert.</param>
    /// <param name="ffmpegDirectory">Directory containing FFmpeg binaries.</param>
    /// <param name="service">The conversion service.</param>
    /// <param name="pluginInfo">Plugin info for FFmpeg download dialog access.</param>
    internal ConvertVideoPopupViewModel(
        IReadOnlyList<IStorageFile> files,
        string ffmpegDirectory,
        IVideoConversionService service,
        IPluginInfo pluginInfo)
    {
        Files = files;
        FfmpegDirectory = ffmpegDirectory;
        _service = service;
        PluginInfo = pluginInfo;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConvertVideoPopupViewModel"/> class
    /// for viewing an existing conversion queue.
    /// </summary>
    /// <param name="queue">An existing conversion queue to display.</param>
    /// <param name="service">The conversion service.</param>
    internal ConvertVideoPopupViewModel(
        VideoConversionQueue queue,
        IVideoConversionService service)
    {
        Files = [];
        FfmpegDirectory = string.Empty;
        _service = service;
        Queue = queue;
    }

    /// <summary>
    /// Gets the video files to convert.
    /// </summary>
    internal IReadOnlyList<IStorageFile> Files { get; }

    /// <summary>
    /// Gets the directory containing FFmpeg binaries.
    /// </summary>
    internal string FfmpegDirectory { get; }

    /// <summary>
    /// Gets the plugin info for FFmpeg download dialog access.
    /// </summary>
    internal IPluginInfo? PluginInfo { get; }

    /// <summary>
    /// Gets or sets a delegate that ensures FFmpeg is available on the machine,
    /// prompting the user to download if necessary. Set by the popup code-behind.
    /// Returns true if FFmpeg is available; false if the user cancelled.
    /// </summary>
    internal Func<Task<bool>>? EnsureFfmpegAvailableAsync { get; set; }

    /// <summary>
    /// Gets or sets the conversion queue. Null when no conversion has started yet.
    /// </summary>
    [ObservableProperty]
    public partial VideoConversionQueue? Queue { get; set; }

    /// <summary>
    /// Raised when conversion has started and the UI should navigate to the progress page.
    /// </summary>
    internal event EventHandler? ConversionStarted;

    /// <summary>
    /// Raised when the conversion queue has completed and the UI should navigate to the result page.
    /// </summary>
    internal event EventHandler? ConversionCompleted;

    /// <summary>
    /// Selects a target format and starts conversion after ensuring FFmpeg is available.
    /// </summary>
    /// <param name="format">Target format (e.g., "mp4", "mkv", "webm", "mov", "avi", "gif").</param>
    [RelayCommand]
    private async Task SelectFormatAsync(string format)
    {
        var options = new VideoConversionOptions { OutputFormat = format };
        await StartConversionAsync(options);
    }

    /// <summary>
    /// Opens the output folder in File Explorer.
    /// </summary>
    [RelayCommand]
    private void OpenOutputFolder()
    {
        if (Queue is null)
        {
            return;
        }

        IReadOnlyList<string> outputPaths = Queue.OutputPaths;
        if (outputPaths.Count == 0)
        {
            return;
        }

        string? directory = System.IO.Path.GetDirectoryName(outputPaths[0]);
        if (directory is not null && Directory.Exists(directory))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = directory,
                UseShellExecute = true,
            });
        }
    }

    /// <summary>
    /// Cancels the current conversion queue.
    /// </summary>
    [RelayCommand]
    private void CancelConversion()
    {
        Queue?.Cancel();
    }

    /// <summary>
    /// Subscribes to queue state changes to detect completion.
    /// Called by the popup code-behind after navigation.
    /// </summary>
    internal void ObserveQueueCompletion()
    {
        if (Queue is not null)
        {
            Queue.PropertyChanged += Queue_PropertyChanged;
        }
    }

    /// <summary>
    /// Unsubscribes from queue events. Called when the popup closes.
    /// </summary>
    internal void StopObservingQueue()
    {
        if (Queue is not null)
        {
            Queue.PropertyChanged -= Queue_PropertyChanged;
        }
    }

    /// <summary>
    /// Resets the ViewModel so the user can start a fresh conversion.
    /// Called when the original popup is reopened after a previous queue ran.
    /// </summary>
    internal void Reset()
    {
        StopObservingQueue();
        Queue = null;
    }

    private void Queue_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VideoConversionQueue.State)
            && Queue?.State is VideoConversionQueueState.Completed or VideoConversionQueueState.Failed)
        {
            ConversionCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task StartConversionAsync(VideoConversionOptions options)
    {
        if (EnsureFfmpegAvailableAsync is not null)
        {
            bool ffmpegReady = await EnsureFfmpegAvailableAsync();
            if (!ffmpegReady)
            {
                return;
            }
        }

        Queue = _service.CreateQueue(Files, options, FfmpegDirectory);
        ObserveQueueCompletion();
        ConversionStarted?.Invoke(this, EventArgs.Empty);
    }
}
