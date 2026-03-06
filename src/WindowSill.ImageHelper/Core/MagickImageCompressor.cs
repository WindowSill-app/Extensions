using ImageMagick;

namespace WindowSill.ImageHelper.Core;

/// <summary>
/// Performs lossless image compression using ImageMagick.
/// </summary>
internal sealed class MagickImageCompressor : IImageCompressor
{
    /// <inheritdoc />
    public long LosslessCompress(string sourcePath, string outputPath)
    {
        File.Copy(sourcePath, outputPath, overwrite: false);

        var outputFileInfo = new FileInfo(outputPath);
        var optimizer = new ImageOptimizer
        {
            IgnoreUnsupportedFormats = true,
            OptimalCompression = true
        };
        optimizer.LosslessCompress(outputFileInfo);

        outputFileInfo.Refresh();
        return outputFileInfo.Length;
    }
}
