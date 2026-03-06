using ImageMagick;

namespace WindowSill.ImageHelper.Core;

/// <summary>
/// Converts image formats using ImageMagick.
/// </summary>
internal sealed class MagickImageConverter : IImageConverter
{
    /// <inheritdoc />
    public async Task ConvertAsync(string sourcePath, string outputPath, MagickFormat targetFormat, CancellationToken cancellationToken)
    {
        using var originalImage = new MagickImage(sourcePath);
        originalImage.Format = targetFormat;
        byte[] data = originalImage.ToByteArray();
        await File.WriteAllBytesAsync(outputPath, data, cancellationToken);
    }
}
