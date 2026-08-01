namespace WindowSill.FileHelper.Core;

/// <summary>
/// A single unit of work in a <see cref="Services.FileOperationQueue"/>: it consumes one or more selected files and
/// produces a result beside them.
/// </summary>
/// <remarks>
/// This is the abstraction that lets one queue drive both per-file work (converting or compressing each selected
/// file independently) and whole-selection work (merging several PDFs into one document), without the queue itself
/// needing to know which kind it is running.
/// </remarks>
internal interface IFileOperation
{
    /// <summary>
    /// Gets the label shown for this operation in the progress list — a file name for per-file work, or a summary
    /// such as "3 files" for work that consumes the whole selection.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Runs the operation.
    /// </summary>
    /// <param name="progress">Optional progress reporter (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The path of the primary file produced, or <see langword="null"/> if nothing was produced. Throws when the
    /// operation fails, so the caller can surface an actionable reason.
    /// </returns>
    Task<string?> ExecuteAsync(IProgress<double>? progress, CancellationToken cancellationToken);
}
