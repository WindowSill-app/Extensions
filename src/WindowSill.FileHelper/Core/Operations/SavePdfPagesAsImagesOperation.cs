using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

using WindowSill.API;

using Path = System.IO.Path;

namespace WindowSill.FileHelper.Core.Operations;

/// <summary>
/// Saves every page of a PDF as a PNG image inside a folder beside the source.
/// </summary>
/// <remarks>
/// Rasterization uses the Windows PDF engine — the same one behind the page previews — so no third-party PDF
/// renderer is needed. Pages are rendered at a fixed width chosen to stay legible when zoomed rather than at screen
/// resolution.
/// </remarks>
internal sealed class SavePdfPagesAsImagesOperation : IFileOperation
{
    /// <summary>
    /// Output width in pixels. Roughly 150 DPI for a Letter/A4 page: sharp enough to read and to re-print, without
    /// producing enormous files for a long document.
    /// </summary>
    private const uint OutputPixelWidth = 1275;

    private readonly string _sourcePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="SavePdfPagesAsImagesOperation"/> class.
    /// </summary>
    /// <param name="sourcePath">The PDF whose pages should be saved as images.</param>
    internal SavePdfPagesAsImagesOperation(string sourcePath)
    {
        _sourcePath = sourcePath;
    }

    /// <inheritdoc />
    public string DisplayName => Path.GetFileName(_sourcePath);

    /// <inheritdoc />
    public async Task<string?> ExecuteAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(0.05);

        string directory = Path.GetDirectoryName(_sourcePath) ?? string.Empty;
        string baseName = Path.GetFileNameWithoutExtension(_sourcePath);

        string finalPath = await SafeOutputWriter.WriteAndRelocateAsync(
            directory,
            baseName,
            ".png",
            outputFilePath => RenderPagesAsync(outputFilePath, baseName, progress, cancellationToken),
            cancellationToken)
            .ConfigureAwait(false);

        progress?.Report(1.0);
        return finalPath;
    }

    private async Task RenderPagesAsync(
        string outputFilePath,
        string baseName,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        string outputDirectory = Path.GetDirectoryName(outputFilePath) ?? string.Empty;
        string pageNameFormat = "/WindowSill.FileHelper/PdfActions/PageFileName".GetLocalizedString();

        StorageFile file = await StorageFile.GetFileFromPathAsync(_sourcePath).AsTask(cancellationToken);
        PdfDocument document = await PdfDocument.LoadFromFileAsync(file).AsTask(cancellationToken);

        int pageCount = (int)document.PageCount;
        for (int i = 0; i < pageCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using PdfPage page = document.GetPage((uint)i);
            string pageFileName = string.Format(pageNameFormat, baseName, i + 1) + ".png";

            using (var stream = new InMemoryRandomAccessStream())
            {
                await page.RenderToStreamAsync(stream, new PdfPageRenderOptions { DestinationWidth = OutputPixelWidth })
                    .AsTask(cancellationToken)
                    .ConfigureAwait(false);

                stream.Seek(0);

                using FileStream output = File.Create(Path.Combine(outputDirectory, pageFileName));
                await stream.AsStreamForRead().CopyToAsync(output, cancellationToken).ConfigureAwait(false);
            }

            // Leave a little headroom so the bar does not sit at 100% while the files are still being relocated.
            progress?.Report(0.05 + (0.9 * (i + 1) / pageCount));
        }
    }
}
