using ImageMagick;

namespace WindowSill.ImageHelper.Core;

/// <summary>
/// Resizes images using ImageMagick, with support for animated GIFs.
/// </summary>
internal sealed class MagickImageResizer : IImageResizer
{
    /// <inheritdoc />
    public async Task ResizeAsync(string sourcePath, string outputPath, MagickGeometry geometry)
    {
        await Task.Run(() =>
        {
            using var image = new MagickImage(sourcePath);

            if (image.Format == MagickFormat.Gif)
            {
                image.Dispose();
                using var collection = new MagickImageCollection(sourcePath);
                collection.Coalesce();

                foreach (IMagickImage<ushort> frame in collection)
                {
                    frame.Resize(geometry);
                }

                collection.Write(outputPath);
            }
            else
            {
                image.Resize(geometry);
                image.Write(outputPath);
            }
        });
    }

    /// <inheritdoc />
    public (uint Width, uint Height) GetDimensions(string filePath)
    {
        using var image = new MagickImage(filePath);
        return (image.Width, image.Height);
    }
}
