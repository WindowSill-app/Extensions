using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// Converts between Syncfusion DocIO's native Word-family formats (Word, Markdown, HTML, plain text, RTF) fully
/// in-process, by loading the source into a document model and re-saving it via
/// <see cref="WordDocument.Save(string, FormatType)"/>.
/// </summary>
/// <remarks>
/// Unlike PDF (which needs the separate DocIO rendering engine), these formats are produced by DocIO's own document
/// writer. HTML output inlines images as base64 data URIs (single self-contained file); plain text and RTF are always
/// single files; Markdown for a document that contains images additionally emits a sibling images folder next to the
/// output path — the surrounding <see cref="DocumentConverter"/> takes care of relocating that as a set.
/// <para>
/// Fidelity is bounded by what the source format can express: an HTML source is imported without a CSS layout engine
/// and without fetching remote resources, and Markdown is read as a lightweight structural format. Converting from a
/// leaner format to a richer one therefore adds no styling that the source never carried.
/// </para>
/// </remarks>
internal sealed class SyncfusionWordFormatRenderer : IDocumentRenderer
{
    private readonly FormatType _inputFormat;
    private readonly FormatType _outputFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncfusionWordFormatRenderer"/> class for the given source and
    /// target formats, registering the Syncfusion license (if one is available) before any Syncfusion API is used so
    /// output is free of any trial notice.
    /// </summary>
    /// <param name="inputFormat">The DocIO format the source files are read as (e.g. <see cref="FormatType.Markdown"/>).</param>
    /// <param name="outputFormat">The DocIO save format (e.g. <see cref="FormatType.Html"/>).</param>
    internal SyncfusionWordFormatRenderer(FormatType inputFormat, FormatType outputFormat)
    {
        _inputFormat = inputFormat;
        _outputFormat = outputFormat;
        SyncfusionLicense.EnsureRegistered();
    }

    /// <inheritdoc />
    public void RenderToFile(string sourcePath, string outputFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using FileStream inputStream = File.OpenRead(sourcePath);
        using var wordDocument = new WordDocument(inputStream, _inputFormat);

        cancellationToken.ThrowIfCancellationRequested();

        // Path-based save so DocIO derives any sibling resource folder (e.g. Markdown's "<name>_images") from the
        // output file name and writes it alongside, within the converter's isolated temp directory.
        wordDocument.Save(outputFilePath, _outputFormat);
    }
}
