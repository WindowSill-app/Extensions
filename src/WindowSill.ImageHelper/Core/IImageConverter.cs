using ImageMagick;

namespace WindowSill.ImageHelper.Core;

/// <summary>
/// Defines image format conversion operations.
/// </summary>
internal interface IImageConverter
{
    /// <summary>
    /// Converts an image file to the specified format.
    /// </summary>
    /// <param name="sourcePath">The full path to the source image file.</param>
    /// <param name="outputPath">The full path for the converted output file.</param>
    /// <param name="targetFormat">The target image format.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous conversion operation.</returns>
    Task ConvertAsync(string sourcePath, string outputPath, MagickFormat targetFormat, CancellationToken cancellationToken);
}
