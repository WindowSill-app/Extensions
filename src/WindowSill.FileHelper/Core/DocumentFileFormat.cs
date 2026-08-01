namespace WindowSill.FileHelper.Core;

/// <summary>
/// A document format FileHelper understands, either as a conversion source, a conversion target, or both.
/// </summary>
/// <remarks>
/// Which formats can be converted into which is decided by <see cref="ConversionCatalog"/>, not by this enum: a
/// value listed here is merely a format FileHelper can name, not a guarantee that it is usable in both directions.
/// </remarks>
internal enum DocumentFileFormat
{
    /// <summary>Word document (<c>.docx</c>).</summary>
    Docx,

    /// <summary>Legacy binary Word document (<c>.doc</c>). Source-only — FileHelper never writes this format.</summary>
    Doc,

    /// <summary>Rich Text Format (<c>.rtf</c>).</summary>
    Rtf,

    /// <summary>HTML (<c>.html</c>, <c>.htm</c>). As a target, images are inlined as base64 data URIs.</summary>
    Html,

    /// <summary>Markdown (<c>.md</c>, <c>.markdown</c>). As a target, may emit a sibling images folder.</summary>
    Markdown,

    /// <summary>Plain text (<c>.txt</c>).</summary>
    Txt,

    /// <summary>Comma-separated values (<c>.csv</c>).</summary>
    Csv,

    /// <summary>Tab-separated values (<c>.tsv</c>, <c>.tab</c>).</summary>
    Tsv,

    /// <summary>Excel workbook (<c>.xlsx</c>, <c>.xls</c>).</summary>
    Xlsx,

    /// <summary>PowerPoint presentation (<c>.pptx</c>, <c>.ppt</c>).</summary>
    Pptx,

    /// <summary>PDF (<c>.pdf</c>). Target-only — reading PDF back into a document model is not supported.</summary>
    Pdf,
}
