namespace WindowSill.VideoHelper.Services;

/// <summary>
/// Represents the state of a video conversion queue.
/// </summary>
internal enum VideoConversionQueueState
{
    /// <summary>
    /// The queue has been created but not yet started.
    /// </summary>
    Pending,

    /// <summary>
    /// The queue is actively converting files.
    /// </summary>
    InProgress,

    /// <summary>
    /// All files in the queue have been processed.
    /// </summary>
    Completed,

    /// <summary>
    /// The queue was cancelled or encountered an unrecoverable error.
    /// </summary>
    Failed,
}
