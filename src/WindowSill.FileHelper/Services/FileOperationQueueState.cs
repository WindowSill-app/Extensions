namespace WindowSill.FileHelper.Services;

/// <summary>
/// Represents the state of a document conversion queue.
/// </summary>
internal enum FileOperationQueueState
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
    /// The queue was stopped by a cancellation request before all files could be processed. Canceled work is
    /// tracked separately from failed work (see <see cref="FileOperationQueue.CanceledCount"/>) and is not
    /// counted as a failure.
    /// </summary>
    Canceled,

    /// <summary>
    /// The queue encountered an unrecoverable error.
    /// </summary>
    Failed,
}
