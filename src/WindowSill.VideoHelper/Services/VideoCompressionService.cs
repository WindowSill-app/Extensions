using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Windows.Storage;
using WindowSill.API;
using WindowSill.VideoHelper.Core;

namespace WindowSill.VideoHelper.Services;

/// <summary>
/// Manages video compression queues that run independently of any UI popup.
/// Exported as a MEF singleton so all components share the same service instance.
/// </summary>
[Export(typeof(IVideoCompressionService))]
internal sealed class VideoCompressionService : IVideoCompressionService
{
    /// <inheritdoc />
    public ObservableCollection<VideoCompressionQueue> Queues { get; } = [];

    /// <inheritdoc />
    public VideoCompressionQueue CreateQueue(
        IReadOnlyList<IStorageFile> files,
        VideoCompressionOptions options,
        string ffmpegDirectory)
    {
        var compressor = new FFmpegVideoCompressor(ffmpegDirectory);
        var filePaths = files.Select(f => f.Path).ToList();

        var queue = new VideoCompressionQueue(filePaths, options, compressor);
        Queues.Add(queue);

        // Start compression in the background without blocking
        RunQueueAsync(queue).ForgetSafely();

        return queue;
    }

    private static async Task RunQueueAsync(VideoCompressionQueue queue)
    {
        try
        {
            await queue.RunAsync();
        }
        catch (Exception)
        {
            // Queue handles its own state transitions; this is a safety net
            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                if (queue.State != VideoCompressionQueueState.Completed)
                {
                    queue.State = VideoCompressionQueueState.Failed;
                }
            });
        }
    }
}
