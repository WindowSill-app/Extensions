namespace WindowSill.FileHelper.Core;

/// <summary>
/// Defines the contract for converting a document to a single target format.
/// </summary>
internal interface IDocumentConverter
{
    /// <summary>
    /// Gets the file extension (including the leading dot, e.g. <c>.pdf</c>) this converter produces.
    /// </summary>
    string OutputExtension { get; }

    /// <summary>
    /// Converts a document to <see cref="OutputExtension"/>.
    /// </summary>
    /// <param name="sourcePath">Path to the source file. Its format is fixed by the converter instance.</param>
    /// <param name="outputPath">Requested path for the converted output file. The actual final path may differ (see
    /// the return value) if another file was concurrently created at this path, or if the conversion produced
    /// sibling resource files that had to be relocated into a dedicated subfolder.</param>
    /// <param name="progress">Optional progress reporter (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The actual path the primary converted file was written to, if conversion succeeded; otherwise
    /// <see langword="null"/>.</returns>
    Task<string?> ConvertAsync(
        string sourcePath,
        string outputPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
