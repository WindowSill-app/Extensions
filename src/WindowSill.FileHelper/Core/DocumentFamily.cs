namespace WindowSill.FileHelper.Core;

/// <summary>
/// The kind of document a format represents. Conversion only makes sense within a family (plus PDF, which every
/// family can be printed to), so this is what stops the popup offering, say, a spreadsheet as Markdown.
/// </summary>
internal enum DocumentFamily
{
    /// <summary>Flowing text documents, handled by Syncfusion DocIO.</summary>
    Word,

    /// <summary>Tabular data, handled by Syncfusion XlsIO.</summary>
    Spreadsheet,

    /// <summary>Slide decks, handled by Syncfusion Presentation.</summary>
    Presentation,

    /// <summary>PDF, which is a target for every family and a source for none.</summary>
    Pdf,
}
