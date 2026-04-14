namespace WindowSill.InlineTerminal.Core.Parsers;

/// <summary>
/// Detects and extracts language identifiers from markdown fenced code blocks.
/// </summary>
internal static class MarkdownCodeBlockParser
{
    /// <summary>
    /// Attempts to find the first markdown code block language definition without allocating.
    /// </summary>
    internal static bool TryGetLanguage(ReadOnlySpan<char> selectedText, out ReadOnlySpan<char> language)
    {
        ReadOnlySpan<char> delimiter = "```";

        foreach (ReadOnlySpan<char> line in selectedText.EnumerateLines())
        {
            int idx = line.IndexOf(delimiter);
            if (idx >= 0)
            {
                language = line[(idx + delimiter.Length)..];
                return true;
            }
        }

        language = default;
        return false;
    }

    /// <summary>
    /// Returns whether the given line is a markdown code block fence (e.g. <c>```</c> or <c>```language</c>).
    /// </summary>
    internal static bool IsFenceLine(ReadOnlySpan<char> line)
    {
        return line.TrimStart().StartsWith("```");
    }
}
