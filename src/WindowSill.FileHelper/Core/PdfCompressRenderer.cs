using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;

using WindowSill.API;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// Rewrites a PDF with Syncfusion's optimizations applied (recompressed images, subset fonts, stripped metadata,
/// optimized page content streams).
/// </summary>
/// <remarks>
/// Compression is not guaranteed to help: a PDF that is already optimized, or one made only of vector/text content,
/// can come out <em>larger</em> than it went in. Rather than replace the user's file with a bigger "compressed"
/// copy, this renderer fails with an explanation in that case, and because the output only ever exists inside
/// <see cref="SafeOutputWriter"/>'s temporary directory until it succeeds, nothing is left behind.
/// </remarks>
internal sealed class PdfCompressRenderer : IDocumentRenderer
{
    /// <summary>
    /// Quality applied when recompressing embedded images. High enough to stay visually faithful for documents and
    /// scans, low enough that image-heavy PDFs actually shrink.
    /// </summary>
    private const int ImageQuality = 75;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfCompressRenderer"/> class, registering the Syncfusion license
    /// (if one is available) before any Syncfusion API is used.
    /// </summary>
    internal PdfCompressRenderer()
    {
        SyncfusionLicense.EnsureRegistered();
    }

    /// <inheritdoc />
    public void RenderToFile(string sourcePath, string outputFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using (PdfLoadedDocument loaded = PdfDocumentLoader.Load(sourcePath))
        {
            loaded.Compress(new PdfCompressionOptions
            {
                CompressImages = true,
                ImageQuality = ImageQuality,
                OptimizeFont = true,
                RemoveMetadata = true,
                OptimizePageContents = true,
            });

            cancellationToken.ThrowIfCancellationRequested();

            // Scoped so the stream is flushed and closed before the size below is measured.
            using FileStream outputStream = File.Create(outputFilePath);
            loaded.Save(outputStream);
        }

        long sourceLength = new FileInfo(sourcePath).Length;
        long outputLength = new FileInfo(outputFilePath).Length;
        if (outputLength >= sourceLength)
        {
            throw new InvalidOperationException(
                "/WindowSill.FileHelper/PdfActions/ErrorAlreadyOptimized".GetLocalizedString());
        }
    }
}
