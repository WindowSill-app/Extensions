using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage;
using WindowSill.API;
using WindowSill.VideoHelper.Core;
using WindowSill.VideoHelper.Services;

namespace WindowSill.VideoHelper.ViewModels;

/// <summary>
/// ViewModel for the CompressVideoPopup, managing compression level selection,
/// progress tracking, and result display.
/// </summary>
internal sealed partial class CompressVideoPopupViewModel : ObservableObject
{
    private readonly IVideoCompressionService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompressVideoPopupViewModel"/> class
    /// for starting a new compression.
    /// </summary>
    /// <param name="files">Video files to compress.</param>
    /// <param name="ffmpegDirectory">Directory containing FFmpeg binaries.</param>
    /// <param name="service">The compression service.</param>
    /// <param name="pluginInfo">Plugin info for FFmpeg download dialog access.</param>
    internal CompressVideoPopupViewModel(
        IReadOnlyList<IStorageFile> files,
        string ffmpegDirectory,
        IVideoCompressionService service,
        IPluginInfo pluginInfo)
    {
        Files = files;
        FfmpegDirectory = ffmpegDirectory;
        _service = service;
        PluginInfo = pluginInfo;

        // Custom settings defaults (Balanced preset)
        SelectedCodecIndex = 0; // H.264
        Crf = 23;
        SelectedPresetIndex = 5; // medium
        SelectedAudioBitrateIndex = 0; // Original
        SelectedResolutionIndex = 0; // Original
        SelectedFrameRateIndex = 0; // Original
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompressVideoPopupViewModel"/> class
    /// for viewing an existing compression queue.
    /// </summary>
    /// <param name="queue">An existing compression queue to display.</param>
    /// <param name="service">The compression service.</param>
    internal CompressVideoPopupViewModel(
        VideoCompressionQueue queue,
        IVideoCompressionService service)
    {
        Files = [];
        FfmpegDirectory = string.Empty;
        _service = service;
        Queue = queue;
    }

    /// <summary>
    /// Gets the video files to compress.
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
    /// Gets or sets the compression queue. Null when no compression has started yet.
    /// </summary>
    [ObservableProperty]
    public partial VideoCompressionQueue? Queue { get; set; }

    /// <summary>
    /// Raised when compression has started and the UI should navigate to the progress page.
    /// </summary>
    internal event EventHandler? CompressionStarted;

    /// <summary>
    /// Raised when the compression queue has completed and the UI should navigate to the result page.
    /// </summary>
    internal event EventHandler? CompressionCompleted;

    #region Custom Settings Properties

    /// <summary>
    /// Gets or sets the selected rate control mode index. 0 = CRF, 1 = Constant Bitrate.
    /// </summary>
    [ObservableProperty]
    public partial int SelectedRateControlIndex { get; set; }

    /// <summary>
    /// Gets whether CRF mode is currently selected.
    /// </summary>
    public bool IsCrfMode => SelectedRateControlIndex == 0;

    /// <summary>
    /// Gets whether constant bitrate mode is currently selected.
    /// </summary>
    public bool IsBitrateMode => SelectedRateControlIndex == 1;

    /// <summary>
    /// Gets or sets the target video bitrate in kbps for constant bitrate mode.
    /// </summary>
    [ObservableProperty]
    public partial int VideoBitrateKbps { get; set; } = 5000;

    partial void OnSelectedRateControlIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsCrfMode));
        OnPropertyChanged(nameof(IsBitrateMode));
    }

    /// <summary>
    /// Gets the available video codecs for the custom settings page.
    /// </summary>
    public IReadOnlyList<string> VideoCodecs { get; } = ["H.264", "H.265 (HEVC)", "AV1"];

    /// <summary>
    /// Gets or sets the selected video codec index.
    /// </summary>
    [ObservableProperty]
    public partial int SelectedCodecIndex { get; set; }

    /// <summary>
    /// Gets or sets the CRF (Constant Rate Factor) quality value.
    /// </summary>
    [ObservableProperty]
    public partial int Crf { get; set; }

    /// <summary>
    /// Gets the available encoding speed presets.
    /// </summary>
    public IReadOnlyList<string> EncodingPresets { get; } =
        ["ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow"];

    /// <summary>
    /// Gets or sets the selected encoding preset index.
    /// </summary>
    [ObservableProperty]
    public partial int SelectedPresetIndex { get; set; }

    /// <summary>
    /// Gets the available audio bitrate options.
    /// </summary>
    public IReadOnlyList<string> AudioBitrates { get; } =
        ["Original", "96 kbps", "128 kbps", "192 kbps", "256 kbps"];

    /// <summary>
    /// Gets or sets the selected audio bitrate index.
    /// </summary>
    [ObservableProperty]
    public partial int SelectedAudioBitrateIndex { get; set; }

    /// <summary>
    /// Gets the available resolution options.
    /// </summary>
    public IReadOnlyList<string> Resolutions { get; } =
        ["Original", "2160p", "1440p", "1080p", "720p", "480p", "360p"];

    /// <summary>
    /// Gets or sets the selected resolution index.
    /// </summary>
    [ObservableProperty]
    public partial int SelectedResolutionIndex { get; set; }

    /// <summary>
    /// Gets the available frame rate options.
    /// </summary>
    public IReadOnlyList<string> FrameRates { get; } =
        ["Original", "60 fps", "30 fps", "24 fps", "15 fps"];

    /// <summary>
    /// Gets or sets the selected frame rate index.
    /// </summary>
    [ObservableProperty]
    public partial int SelectedFrameRateIndex { get; set; }

    #endregion

    /// <summary>
    /// Selects a preset and starts compression after ensuring FFmpeg is available.
    /// </summary>
    /// <param name="presetName">Name of the preset to use.</param>
    [RelayCommand]
    private async Task SelectPresetAsync(string presetName)
    {
        (string Name, string Description, VideoCompressionOptions Options)? preset =
            VideoPresets.AllPresets.FirstOrDefault(p => p.Name == presetName);

        if (preset is null)
        {
            return;
        }

        await StartCompressionAsync(preset.Value.Options);
    }

    /// <summary>
    /// Starts compression using the current custom settings after ensuring FFmpeg is available.
    /// </summary>
    [RelayCommand]
    private async Task CompressWithCustomSettingsAsync()
    {
        VideoCompressionOptions options = BuildCustomOptions();
        await StartCompressionAsync(options);
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
    /// Cancels the current compression queue.
    /// </summary>
    [RelayCommand]
    private void CancelCompression()
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
    /// Resets the ViewModel so the user can start a fresh compression.
    /// Called when the original popup is reopened after a previous queue ran.
    /// </summary>
    internal void Reset()
    {
        StopObservingQueue();
        Queue = null;
    }

    private void Queue_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VideoCompressionQueue.State)
            && Queue?.State is VideoCompressionQueueState.Completed or VideoCompressionQueueState.Failed)
        {
            CompressionCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task StartCompressionAsync(VideoCompressionOptions options)
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
        CompressionStarted?.Invoke(this, EventArgs.Empty);
    }

    private VideoCompressionOptions BuildCustomOptions()
    {
        string videoCodec = SelectedCodecIndex switch
        {
            1 => "libx265",
            2 => "libsvtav1",
            _ => "libx264",
        };

        int? audioBitrateKbps = SelectedAudioBitrateIndex switch
        {
            1 => 96,
            2 => 128,
            3 => 192,
            4 => 256,
            _ => null,
        };

        int? resolutionHeight = SelectedResolutionIndex switch
        {
            1 => 2160,
            2 => 1440,
            3 => 1080,
            4 => 720,
            5 => 480,
            6 => 360,
            _ => null,
        };

        int? maxFrameRate = SelectedFrameRateIndex switch
        {
            1 => 60,
            2 => 30,
            3 => 24,
            4 => 15,
            _ => null,
        };

        return new VideoCompressionOptions
        {
            VideoCodec = videoCodec,
            Crf = Crf,
            Preset = EncodingPresets[SelectedPresetIndex],
            AudioBitrateKbps = audioBitrateKbps,
            ResolutionHeight = resolutionHeight,
            MaxFrameRate = maxFrameRate,
            VideoBitrateKbps = SelectedRateControlIndex == 1 ? VideoBitrateKbps : null,
        };
    }
}

