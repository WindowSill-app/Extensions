namespace WindowSill.VideoHelper.Core;

/// <summary>
/// Defines the contract for video compression operations.
/// </summary>
internal interface IVideoCompressor
{
    /// <summary>
    /// Compresses a video file with the specified options.
    /// </summary>
    /// <param name="sourcePath">Path to the source video file.</param>
    /// <param name="outputPath">Path for the compressed output file.</param>
    /// <param name="options">Compression options to apply.</param>
    /// <param name="progress">Optional progress reporter (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if compression succeeded; otherwise false.</returns>
    Task<bool> CompressAsync(
        string sourcePath,
        string outputPath,
        VideoCompressionOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
