using Syncfusion.Pdf;
using Syncfusion.XlsIO;
using Syncfusion.XlsIORenderer;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// Lays a spreadsheet out and renders it to PDF, so a workbook or CSV can be shared with someone who has no
/// spreadsheet application.
/// </summary>
internal sealed class SyncfusionWorkbookToPdfRenderer : IDocumentRenderer
{
    private readonly string? _inputDelimiter;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncfusionWorkbookToPdfRenderer"/> class.
    /// </summary>
    /// <param name="inputDelimiter">Delimiter of the source, or <see langword="null"/> when it is a workbook.</param>
    internal SyncfusionWorkbookToPdfRenderer(string? inputDelimiter)
    {
        _inputDelimiter = inputDelimiter;
        SyncfusionLicense.EnsureRegistered();
    }

    /// <inheritdoc />
    public void RenderToFile(string sourcePath, string outputFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var engine = new ExcelEngine();
        IWorkbook workbook = WorkbookLoader.Open(engine, sourcePath, _inputDelimiter);

        cancellationToken.ThrowIfCancellationRequested();

        var renderer = new XlsIORenderer();
        using PdfDocument pdf = renderer.ConvertToPDF(workbook);

        cancellationToken.ThrowIfCancellationRequested();

        using FileStream output = File.Create(outputFilePath);
        pdf.Save(output);
    }
}
