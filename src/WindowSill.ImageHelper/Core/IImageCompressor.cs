namespace WindowSill.ImageHelper.Core;

/// <summary>
/// Defines lossless image compression operations.
/// </summary>
internal interface IImageCompressor
{
    /// <summary>
    /// Performs lossless compression on a copy of the source file.
    /// </summary>
    /// <param name="sourcePath">The full path to the source image file.</param>
    /// <param name="outputPath">The full path for the compressed output file.</param>
    /// <returns>The size in bytes of the compressed file.</returns>
    long LosslessCompress(string sourcePath, string outputPath);
}
