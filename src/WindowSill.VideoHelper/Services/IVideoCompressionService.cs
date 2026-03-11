using System.Collections.ObjectModel;
using Windows.Storage;
using WindowSill.VideoHelper.Core;

namespace WindowSill.VideoHelper.Services;

/// <summary>
/// Defines the contract for managing video compression queues that persist
/// independently of the UI lifecycle.
/// </summary>
internal interface IVideoCompressionService
{
    /// <summary>
    /// Gets the observable collection of all active and completed compression queues.
    /// </summary>
    ObservableCollection<VideoCompressionQueue> Queues { get; }

    /// <summary>
    /// Creates a new compression queue for the specified files and starts it immediately.
    /// </summary>
    /// <param name="files">Video files to compress.</param>
    /// <param name="options">Compression options to apply.</param>
    /// <param name="ffmpegDirectory">Directory containing FFmpeg binaries.</param>
    /// <returns>The created and running compression queue.</returns>
    VideoCompressionQueue CreateQueue(
        IReadOnlyList<IStorageFile> files,
        VideoCompressionOptions options,
        string ffmpegDirectory);
}
