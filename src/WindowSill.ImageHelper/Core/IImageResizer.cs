using ImageMagick;

namespace WindowSill.ImageHelper.Core;

/// <summary>
/// Defines image resize operations.
/// </summary>
internal interface IImageResizer
{
    /// <summary>
    /// Resizes an image file to the specified geometry.
    /// </summary>
    /// <param name="sourcePath">The full path to the source image file.</param>
    /// <param name="outputPath">The full path for the resized output file.</param>
    /// <param name="geometry">The target size geometry.</param>
    Task ResizeAsync(string sourcePath, string outputPath, MagickGeometry geometry);

    /// <summary>
    /// Reads the dimensions of an image file.
    /// </summary>
    /// <param name="filePath">The full path to the image file.</param>
    /// <returns>A tuple of (width, height) in pixels.</returns>
    (uint Width, uint Height) GetDimensions(string filePath);
}
