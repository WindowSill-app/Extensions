using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;

using WindowSill.API;

using Path = System.IO.Path;

namespace WindowSill.FileHelper.Core.Operations;

/// <summary>
/// Copies a chosen set of pages out of a PDF into a new document beside the source.
/// </summary>
/// <remarks>
/// Pages are imported in the order given, so the caller decides both which pages appear and in what sequence.
/// Output safety (temp directory, atomic non-overwriting move, "(n)" collision naming) is shared with the other
/// operations via <see cref="SafeOutputWriter"/>.
/// </remarks>
internal sealed class ExtractPdfPagesOperation : IFileOperation
{
    private readonly string _sourcePath;
    private readonly IReadOnlyList<int> _pageIndices;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractPdfPagesOperation"/> class.
    /// </summary>
    /// <param name="sourcePath">The PDF to take pages from.</param>
    /// <param name="pageIndices">Zero-based indices of the pages to copy, in output order.</param>
    internal ExtractPdfPagesOperation(string sourcePath, IReadOnlyList<int> pageIndices)
    {
        _sourcePath = sourcePath;
        _pageIndices = pageIndices;
        SyncfusionLicense.EnsureRegistered();
    }

    /// <inheritdoc />
    public string DisplayName => Path.GetFileName(_sourcePath);

    /// <inheritdoc />
    public async Task<string?> ExecuteAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(0.05);

        string directory = Path.GetDirectoryName(_sourcePath) ?? string.Empty;
        string sourceName = Path.GetFileNameWithoutExtension(_sourcePath);

        // A single extracted page reads far better named after that page than as a generic "pages" file.
        string baseName = _pageIndices.Count == 1
            ? string.Format(
                "/WindowSill.FileHelper/PdfActions/ExtractedSinglePageFileName".GetLocalizedString(),
                sourceName,
                _pageIndices[0] + 1)
            : string.Format(
                "/WindowSill.FileHelper/PdfActions/ExtractedFileName".GetLocalizedString(),
                sourceName);

        string finalPath = await Task.Run(
            () => SafeOutputWriter.WriteAndRelocate(
                directory,
                baseName,
                ConversionCatalog.GetInfo(DocumentFileFormat.Pdf).Extension,
                outputFilePath => ExtractInto(outputFilePath, cancellationToken),
                cancellationToken),
            cancellationToken)
            .ConfigureAwait(false);

        progress?.Report(1.0);
        return finalPath;
    }

    private void ExtractInto(string outputFilePath, CancellationToken cancellationToken)
    {
        using PdfLoadedDocument loaded = PdfDocumentLoader.Load(_sourcePath);
        using var extracted = new PdfDocument();

        int pageCount = loaded.Pages.Count;
        foreach (int pageIndex in _pageIndices)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Guard against a selection made before the document changed on disk.
            if (pageIndex >= 0 && pageIndex < pageCount)
            {
                extracted.ImportPage(loaded, pageIndex);
            }
        }

        if (extracted.Pages.Count == 0)
        {
            throw new InvalidOperationException(
                "/WindowSill.FileHelper/PdfActions/ErrorNoPagesExtracted".GetLocalizedString());
        }

        cancellationToken.ThrowIfCancellationRequested();

        using FileStream outputStream = File.Create(outputFilePath);
        extracted.Save(outputStream);
    }
}
