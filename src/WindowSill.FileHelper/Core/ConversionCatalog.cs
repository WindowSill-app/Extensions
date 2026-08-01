using Syncfusion.DocIO;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// The single source of truth for what FileHelper can convert: which extensions map to which
/// <see cref="DocumentFileFormat"/>, which targets a given source offers, and which renderer performs each
/// conversion.
/// </summary>
/// <remarks>
/// <para>
/// Conversion is scoped by <see cref="DocumentFamily"/>. Within a family the formats describe the same kind of
/// content, so any of them can be turned into any other; across families they cannot (a spreadsheet is not a
/// Markdown document), with the single exception of PDF, which every family can be laid out and printed to.
/// </para>
/// <para>
/// PDF is deliberately target-only. Recovering a document model from a PDF is a text-reflow problem rather than a
/// format conversion, so a PDF selection offers no conversion targets and gets the PDF actions instead.
/// </para>
/// </remarks>
internal static class ConversionCatalog
{
    /// <summary>
    /// The formats each family can be written to, in the order their buttons are presented. PDF leads because it is
    /// the most commonly requested target.
    /// </summary>
    private static readonly Dictionary<DocumentFamily, DocumentFileFormat[]> WritableFormatsByFamily = new()
    {
        [DocumentFamily.Word] =
        [
            DocumentFileFormat.Pdf,
            DocumentFileFormat.Docx,
            DocumentFileFormat.Markdown,
            DocumentFileFormat.Html,
            DocumentFileFormat.Rtf,
            DocumentFileFormat.Txt,
        ],
        [DocumentFamily.Spreadsheet] =
        [
            DocumentFileFormat.Pdf,
            DocumentFileFormat.Xlsx,
            DocumentFileFormat.Csv,
            DocumentFileFormat.Tsv,
        ],
        [DocumentFamily.Presentation] =
        [
            DocumentFileFormat.Pdf,
        ],
    };

    private static readonly DocumentFileFormatInfo[] Descriptors =
    [
        new(DocumentFileFormat.Docx, DocumentFamily.Word, ".docx", [".docx"], "FormatDocx"),
        new(DocumentFileFormat.Doc, DocumentFamily.Word, ".doc", [".doc"], "FormatDoc"),
        new(DocumentFileFormat.Rtf, DocumentFamily.Word, ".rtf", [".rtf"], "FormatRtf"),
        new(DocumentFileFormat.Html, DocumentFamily.Word, ".html", [".html", ".htm"], "FormatHtml"),
        new(DocumentFileFormat.Markdown, DocumentFamily.Word, ".md", [".md", ".markdown"], "FormatMarkdown"),
        new(DocumentFileFormat.Txt, DocumentFamily.Word, ".txt", [".txt"], "FormatTxt"),
        new(DocumentFileFormat.Csv, DocumentFamily.Spreadsheet, ".csv", [".csv"], "FormatCsv"),
        new(DocumentFileFormat.Tsv, DocumentFamily.Spreadsheet, ".tsv", [".tsv", ".tab"], "FormatTsv"),
        new(DocumentFileFormat.Xlsx, DocumentFamily.Spreadsheet, ".xlsx", [".xlsx", ".xls"], "FormatXlsx"),
        new(DocumentFileFormat.Pptx, DocumentFamily.Presentation, ".pptx", [".pptx", ".ppt"], "FormatPptx"),
        new(DocumentFileFormat.Pdf, DocumentFamily.Pdf, ".pdf", [".pdf"], "FormatPdf"),
    ];

    private static readonly Dictionary<DocumentFileFormat, DocumentFileFormatInfo> InfoByFormat
        = Descriptors.ToDictionary(d => d.Format);

    private static readonly Dictionary<string, DocumentFileFormat> FormatByInputExtension
        = Descriptors
            .SelectMany(d => d.InputExtensions, (d, extension) => (extension, d.Format))
            .ToDictionary(pair => pair.extension, pair => pair.Format, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Resolves the format a file extension represents.
    /// </summary>
    /// <param name="extension">The file extension, including the leading dot (e.g. <c>.docx</c>). Case-insensitive.</param>
    /// <param name="format">The resolved format when the extension is recognized.</param>
    /// <returns><see langword="true"/> when the extension maps to a known format; otherwise <see langword="false"/>.</returns>
    internal static bool TryGetFormat(string extension, out DocumentFileFormat format)
        => FormatByInputExtension.TryGetValue(extension, out format);

    /// <summary>
    /// Gets the descriptor (family, output extension and localized labels) for a format.
    /// </summary>
    /// <param name="format">The format to describe.</param>
    /// <returns>The descriptor for <paramref name="format"/>.</returns>
    internal static DocumentFileFormatInfo GetInfo(DocumentFileFormat format) => InfoByFormat[format];

    /// <summary>
    /// Gets the formats <paramref name="source"/> can be converted to, in presentation order.
    /// </summary>
    /// <param name="source">The format of the selected file(s).</param>
    /// <returns>
    /// The available targets within the source's family, or an empty list when the format cannot be read at all
    /// (PDF).
    /// </returns>
    internal static IReadOnlyList<DocumentFileFormatInfo> GetTargets(DocumentFileFormat source)
    {
        DocumentFamily family = GetInfo(source).Family;
        if (!WritableFormatsByFamily.TryGetValue(family, out DocumentFileFormat[]? writable))
        {
            return [];
        }

        return [.. writable.Where(target => target != source).Select(GetInfo)];
    }

    /// <summary>
    /// Builds the converter that turns <paramref name="source"/> files into <paramref name="target"/> files.
    /// </summary>
    /// <param name="source">The format of the files being converted.</param>
    /// <param name="target">The format to produce.</param>
    /// <returns>A converter bound to that source/target pair.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the pair is not part of the supported conversion matrix.
    /// </exception>
    internal static IDocumentConverter CreateConverter(DocumentFileFormat source, DocumentFileFormat target)
    {
        if (!GetTargets(source).Any(info => info.Format == target))
        {
            throw new ArgumentOutOfRangeException(
                nameof(target),
                target,
                $"Converting '{source}' to '{target}' is not supported.");
        }

        string outputExtension = GetInfo(target).Extension;
        IDocumentRenderer renderer = GetInfo(source).Family switch
        {
            DocumentFamily.Word => CreateWordRenderer(source, target),
            DocumentFamily.Spreadsheet => CreateSpreadsheetRenderer(source, target),
            DocumentFamily.Presentation => new SyncfusionPresentationToPdfRenderer(),
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, "This format cannot be converted."),
        };

        return new DocumentConverter(renderer, outputExtension);
    }

    /// <summary>
    /// Gets the delimiter a character-separated format uses, or <see langword="null"/> for formats that are not
    /// character-separated.
    /// </summary>
    internal static string? GetDelimiter(DocumentFileFormat format) => format switch
    {
        DocumentFileFormat.Csv => ",",
        DocumentFileFormat.Tsv => "\t",
        _ => null,
    };

    /// <summary>
    /// PDF needs DocIO's page-layout rendering engine; every other Word-family target is written by DocIO's own
    /// document writer straight from the loaded model.
    /// </summary>
    private static IDocumentRenderer CreateWordRenderer(DocumentFileFormat source, DocumentFileFormat target)
    {
        FormatType input = ToDocIOFormat(source);

        return target == DocumentFileFormat.Pdf
            ? new SyncfusionWordToPdfRenderer(input)
            : new SyncfusionWordFormatRenderer(input, ToDocIOFormat(target));
    }

    /// <summary>
    /// Spreadsheets are read by XlsIO, which also writes the workbook and character-separated outputs; PDF again
    /// needs the separate layout renderer.
    /// </summary>
    private static IDocumentRenderer CreateSpreadsheetRenderer(DocumentFileFormat source, DocumentFileFormat target)
    {
        string? sourceDelimiter = GetDelimiter(source);

        return target == DocumentFileFormat.Pdf
            ? new SyncfusionWorkbookToPdfRenderer(sourceDelimiter)
            : new SyncfusionWorkbookRenderer(sourceDelimiter, GetDelimiter(target));
    }

    private static FormatType ToDocIOFormat(DocumentFileFormat format) => format switch
    {
        DocumentFileFormat.Docx => FormatType.Docx,
        DocumentFileFormat.Doc => FormatType.Doc,
        DocumentFileFormat.Rtf => FormatType.Rtf,
        DocumentFileFormat.Html => FormatType.Html,
        DocumentFileFormat.Markdown => FormatType.Markdown,
        DocumentFileFormat.Txt => FormatType.Txt,
        _ => throw new ArgumentOutOfRangeException(
            nameof(format),
            format,
            "This format has no Syncfusion DocIO document-model equivalent."),
    };
}
