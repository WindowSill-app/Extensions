using ImageMagick;

namespace WindowSill.ImageHelper.Core;

/// <summary>
/// Combines images into a single multi-page PDF using ImageMagick.
/// </summary>
/// <remarks>
/// A <see cref="MagickImageCollection"/> written as PDF produces one page per image, sized to that image, so a mixed
/// selection keeps every original aspect ratio instead of being letterboxed onto a fixed paper size.
/// </remarks>
internal sealed class MagickImagePdfCombiner : IImagePdfCombiner
{
    /// <inheritdoc />
    public async Task CombineAsync(
        IReadOnlyList<string> sourcePaths,
        string outputPath,
        CancellationToken cancellationToken)
    {
        using var collection = new MagickImageCollection();

        for (int i = 0; i < sourcePaths.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            collection.Add(new MagickImage(sourcePaths[i]));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Written to memory first so a cancellation or failure never leaves a half-written PDF behind.
        using var buffer = new MemoryStream();
        collection.Write(buffer, MagickFormat.Pdf);

        await File.WriteAllBytesAsync(outputPath, buffer.ToArray(), cancellationToken);
    }
}
