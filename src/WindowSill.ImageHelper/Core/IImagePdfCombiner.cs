namespace WindowSill.ImageHelper.Core;

/// <summary>
/// Combines several images into a single multi-page PDF.
/// </summary>
internal interface IImagePdfCombiner
{
    /// <summary>
    /// Writes the given images into one PDF, one image per page, in the order supplied.
    /// </summary>
    /// <param name="sourcePaths">The images to combine, in page order.</param>
    /// <param name="outputPath">The PDF to write.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task CombineAsync(IReadOnlyList<string> sourcePaths, string outputPath, CancellationToken cancellationToken);
}
