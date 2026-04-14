using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;
using WindowSill.VideoHelper.Core;
using WindowSill.VideoHelper.Helpers;

namespace WindowSill.VideoHelper.Services;

/// <summary>
/// Represents a single file compression task with observable progress.
/// </summary>
internal sealed partial class VideoCompressionTaskItem : ObservableObject
{
    private readonly string _sourcePath;
    private readonly VideoCompressionOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoCompressionTaskItem"/> class.
    /// </summary>
    /// <param name="sourcePath">Path to the source video file.</param>
    /// <param name="options">Compression options to apply.</param>
    internal VideoCompressionTaskItem(string sourcePath, VideoCompressionOptions options)
    {
        _sourcePath = sourcePath;
        _options = options;
        FileName = System.IO.Path.GetFileName(sourcePath);
    }

    /// <summary>
    /// Gets the display name of the file being compressed.
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
    /// Gets or sets the compression progress from 0.0 to 1.0.
    /// </summary>
    [ObservableProperty]
    public partial double Progress { get; set; }

    /// <summary>
    /// Gets the output file path after compression completes. Null if not yet completed.
    /// </summary>
    public string? OutputPath { get; private set; }

    /// <summary>
    /// Compresses the video file using the specified compressor.
    /// </summary>
    /// <param name="compressor">The video compressor to use.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal async Task CompressAsync(IVideoCompressor compressor, CancellationToken cancellationToken)
    {
        string outputPath = FilePathHelper.GetUniqueOutputPath(_sourcePath, "_compressed");
        bool isSucceeded = false;

        try
        {
            var progress = new Progress<double>(p =>
            {
                ThreadHelper.RunOnUIThreadAsync(() =>
                {
                    Progress = p;
                }).ForgetSafely();
            });

            isSucceeded
                = await compressor.CompressAsync(
                    _sourcePath,
                    outputPath,
                    _options,
                    progress,
                    cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Cancelled — not a failure, just stopped
        }
        catch (Exception)
        {
            // Compression failed for this file
        }

        if (isSucceeded)
        {
            OutputPath = outputPath;
        }

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            IsRunning = false;
            IsSucceeded = isSucceeded;
            IsFailed = !isSucceeded && !cancellationToken.IsCancellationRequested;
        });
    }
}
