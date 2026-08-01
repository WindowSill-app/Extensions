using Syncfusion.XlsIO;

using WindowSill.API;

using Path = System.IO.Path;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// Converts between the spreadsheet formats XlsIO can write: workbooks (<c>.xlsx</c>) and character-separated text
/// (<c>.csv</c>, <c>.tsv</c>).
/// </summary>
/// <remarks>
/// Character-separated formats hold a single grid, so a multi-sheet workbook cannot become one CSV. Rather than
/// silently dropping every sheet but the first, each worksheet is written to its own file; they are emitted as
/// siblings of the requested output path so <see cref="SafeOutputWriter"/> gathers them into one folder beside the
/// source.
/// </remarks>
internal sealed class SyncfusionWorkbookRenderer : IDocumentRenderer
{
    private readonly string? _inputDelimiter;
    private readonly string? _outputDelimiter;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncfusionWorkbookRenderer"/> class.
    /// </summary>
    /// <param name="inputDelimiter">Delimiter of the source, or <see langword="null"/> when it is a workbook.</param>
    /// <param name="outputDelimiter">Delimiter of the target, or <see langword="null"/> to write a workbook.</param>
    internal SyncfusionWorkbookRenderer(string? inputDelimiter, string? outputDelimiter)
    {
        _inputDelimiter = inputDelimiter;
        _outputDelimiter = outputDelimiter;
        SyncfusionLicense.EnsureRegistered();
    }

    /// <inheritdoc />
    public void RenderToFile(string sourcePath, string outputFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var engine = new ExcelEngine();
        IWorkbook workbook = WorkbookLoader.Open(engine, sourcePath, _inputDelimiter);

        cancellationToken.ThrowIfCancellationRequested();

        if (_outputDelimiter is null)
        {
            using FileStream output = File.Create(outputFilePath);
            workbook.SaveAs(output);
            return;
        }

        if (workbook.Worksheets.Count == 1)
        {
            using FileStream output = File.Create(outputFilePath);
            workbook.Worksheets[0].SaveAs(output, _outputDelimiter);
            return;
        }

        WriteEachWorksheet(workbook, sourcePath, outputFilePath, cancellationToken);
    }

    private void WriteEachWorksheet(
        IWorkbook workbook,
        string sourcePath,
        string outputFilePath,
        CancellationToken cancellationToken)
    {
        string directory = Path.GetDirectoryName(outputFilePath) ?? string.Empty;
        string extension = Path.GetExtension(outputFilePath);

        // Sheet files are named after the SOURCE document, so they read naturally ("budget - Q1.csv") inside the
        // folder the writer creates for them.
        string baseName = Path.GetFileNameWithoutExtension(sourcePath);
        string sheetNameFormat = "/WindowSill.FileHelper/ConvertDocument/SheetFileName".GetLocalizedString();

        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (IWorksheet worksheet in workbook.Worksheets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fileName = MakeUnique(
                string.Format(sheetNameFormat, baseName, Sanitize(worksheet.Name)),
                usedNames);

            using FileStream output = File.Create(Path.Combine(directory, fileName + extension));
            worksheet.SaveAs(output, _outputDelimiter);
        }
    }

    /// <summary>
    /// Strips characters a worksheet may legally contain but a file name may not.
    /// </summary>
    private static string Sanitize(string sheetName)
    {
        string sanitized = string.Concat(sheetName.Split(Path.GetInvalidFileNameChars())).Trim();
        return sanitized.Length == 0 ? "sheet" : sanitized;
    }

    /// <summary>
    /// Disambiguates sheets whose names collapse to the same file name once sanitized.
    /// </summary>
    private static string MakeUnique(string candidate, HashSet<string> usedNames)
    {
        if (usedNames.Add(candidate))
        {
            return candidate;
        }

        for (int suffix = 2; ; suffix++)
        {
            string next = $"{candidate} ({suffix})";
            if (usedNames.Add(next))
            {
                return next;
            }
        }
    }
}
