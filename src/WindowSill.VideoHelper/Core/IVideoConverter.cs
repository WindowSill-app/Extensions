namespace WindowSill.VideoHelper.Core;

/// <summary>
/// Defines the contract for video conversion operations.
/// </summary>
internal interface IVideoConverter
{
    /// <summary>
    /// Converts a video file to a different format with the specified options.
    /// </summary>
    /// <param name="sourcePath">Path to the source video file.</param>
    /// <param name="outputPath">Path for the converted output file.</param>
    /// <param name="options">Conversion options to apply.</param>
    /// <param name="progress">Optional progress reporter (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if conversion succeeded; otherwise false.</returns>
    Task<bool> ConvertAsync(
        string sourcePath,
        string outputPath,
        VideoConversionOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
