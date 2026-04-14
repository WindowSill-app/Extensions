using System.Collections.ObjectModel;
using Windows.Storage;
using WindowSill.VideoHelper.Core;

namespace WindowSill.VideoHelper.Services;

/// <summary>
/// Defines the contract for managing video conversion queues that persist
/// independently of the UI lifecycle.
/// </summary>
internal interface IVideoConversionService
{
    /// <summary>
    /// Gets the observable collection of all active and completed conversion queues.
    /// </summary>
    ObservableCollection<VideoConversionQueue> Queues { get; }

    /// <summary>
    /// Creates a new conversion queue for the specified files and starts it immediately.
    /// </summary>
    /// <param name="files">Video files to convert.</param>
    /// <param name="options">Conversion options to apply.</param>
    /// <param name="ffmpegDirectory">Directory containing FFmpeg binaries.</param>
    /// <returns>The created and running conversion queue.</returns>
    VideoConversionQueue CreateQueue(
        IReadOnlyList<IStorageFile> files,
        VideoConversionOptions options,
        string ffmpegDirectory);
}
