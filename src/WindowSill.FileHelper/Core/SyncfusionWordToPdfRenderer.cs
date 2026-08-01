using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// Renders any Word-family document Syncfusion DocIO can load (<c>.docx</c>, <c>.doc</c>, <c>.rtf</c>, <c>.html</c>,
/// <c>.md</c>, <c>.txt</c>) to PDF fully in-process. Unlike an HTML-based converter, DocIO lays the document out like
/// Word, so cover pages, headers/footers, tables of contents, tables, lists, shapes and inline images are preserved.
/// </summary>
/// <remarks>
/// The whole conversion is synchronous, CPU-bound work with no UI-thread affinity, so <see cref="DocumentConverter"/>
/// simply runs it on the thread pool. Instances are stateless and cheap to construct — one is built per conversion
/// queue by <see cref="ConversionCatalog"/>.
/// </remarks>
internal sealed class SyncfusionWordToPdfRenderer : IDocumentRenderer
{
    private readonly FormatType _inputFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncfusionWordToPdfRenderer"/> class for the given source format,
    /// and registers the Syncfusion license (if one is available) before any Syncfusion API is used, so output is
    /// watermark-free.
    /// </summary>
    /// <param name="inputFormat">The DocIO format the source files are read as (e.g. <see cref="FormatType.Docx"/>).</param>
    internal SyncfusionWordToPdfRenderer(FormatType inputFormat)
    {
        _inputFormat = inputFormat;
        SyncfusionLicense.EnsureRegistered();
    }

    /// <inheritdoc />
    public void RenderToFile(string sourcePath, string outputFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using FileStream inputStream = File.OpenRead(sourcePath);
        using var wordDocument = new WordDocument(inputStream, _inputFormat);

        cancellationToken.ThrowIfCancellationRequested();

        using var renderer = new DocIORenderer();
        using PdfDocument pdfDocument = renderer.ConvertToPDF(wordDocument);

        cancellationToken.ThrowIfCancellationRequested();

        using FileStream outputStream = File.Create(outputFilePath);
        pdfDocument.Save(outputStream);
    }
}
