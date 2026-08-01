namespace WindowSill.FileHelper.Core;

/// <summary>
/// Recognizes files whose content is plain text, so FileHelper can offer encoding and line-ending adjustments.
/// </summary>
/// <remarks>
/// This is deliberately broader than <see cref="DocumentFileFormat"/>: source code, config and log files are not
/// convertible to other document formats, but they are exactly the files whose encoding and line endings people
/// need to fix.
/// </remarks>
internal static class TextFileTypes
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".markdown", ".log", ".csv", ".tsv", ".tab",
        ".json", ".xml", ".yml", ".yaml", ".toml", ".ini", ".cfg", ".conf", ".env",
        ".html", ".htm", ".css", ".js", ".ts", ".jsx", ".tsx",
        ".cs", ".java", ".py", ".go", ".rs", ".cpp", ".c", ".h", ".sql",
        ".ps1", ".bat", ".cmd", ".sh",
    };

    /// <summary>
    /// Determines whether a file extension names a plain-text file.
    /// </summary>
    /// <param name="extension">The extension, including the leading dot. Case-insensitive.</param>
    internal static bool IsTextExtension(string extension) => Extensions.Contains(extension);
}
