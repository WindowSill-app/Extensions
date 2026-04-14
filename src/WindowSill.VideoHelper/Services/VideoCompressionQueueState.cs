namespace WindowSill.VideoHelper.Services;

/// <summary>
/// Represents the state of a video compression queue.
/// </summary>
internal enum VideoCompressionQueueState
{
    /// <summary>
    /// The queue has been created but not yet started.
    /// </summary>
    Pending,

    /// <summary>
    /// The queue is actively compressing files.
    /// </summary>
    InProgress,

    /// <summary>
    /// All files in the queue have been compressed.
    /// </summary>
    Completed,

    /// <summary>
    /// The queue was cancelled or encountered an unrecoverable error.
    /// </summary>
    Failed,
}
