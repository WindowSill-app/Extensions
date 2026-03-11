using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Windows.Storage;
using WindowSill.API;
using WindowSill.VideoHelper.Core;

namespace WindowSill.VideoHelper.Services;

/// <summary>
/// Manages video conversion queues that run independently of any UI popup.
/// Exported as a MEF singleton so all components share the same service instance.
/// </summary>
[Export(typeof(IVideoConversionService))]
internal sealed class VideoConversionService : IVideoConversionService
{
    /// <inheritdoc />
    public ObservableCollection<VideoConversionQueue> Queues { get; } = [];

    /// <inheritdoc />
    public VideoConversionQueue CreateQueue(
        IReadOnlyList<IStorageFile> files,
        VideoConversionOptions options,
        string ffmpegDirectory)
    {
        var converter = new FFmpegVideoConverter(ffmpegDirectory);
        var filePaths = files.Select(f => f.Path).ToList();

        var queue = new VideoConversionQueue(filePaths, options, converter);
        Queues.Add(queue);

        // Start conversion in the background without blocking
        RunQueueAsync(queue).ForgetSafely();

        return queue;
    }

    private static async Task RunQueueAsync(VideoConversionQueue queue)
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
                if (queue.State != VideoConversionQueueState.Completed)
                {
                    queue.State = VideoConversionQueueState.Failed;
                }
            });
        }
    }
}
