namespace WindowSill.FileHelper.Core;

/// <summary>
/// Identifies which of the FileHelper experiences (if any) a given file selection should activate.
/// </summary>
internal enum FileSelectionKind
{
    /// <summary>
    /// The selection does not match any supported FileHelper experience (empty, mixed, or unsupported types).
    /// </summary>
    None,

    /// <summary>
    /// The selection is exactly one ZIP archive: activates the instant ZIP metadata summary.
    /// </summary>
    Zip,

    /// <summary>
    /// The selection is one or more convertible documents that all share the same format: activates the
    /// "Convert" workflow.
    /// </summary>
    Document,

    /// <summary>
    /// The selection is one or more PDFs: activates the PDF actions (merge, split, compress).
    /// </summary>
    Pdf,
}
