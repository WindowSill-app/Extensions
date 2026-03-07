using System.Text.RegularExpressions;

namespace WindowSill.ShortTermReminder.Core;

/// <summary>
/// Provides URI detection and text segmentation for reminder titles.
/// </summary>
internal static partial class UriHelper
{
    /// <summary>
    /// Attempts to extract the first HTTP or HTTPS URI from the given text.
    /// </summary>
    /// <param name="text">The text to search for a URI.</param>
    /// <param name="uri">When this method returns, contains the first URI found, or <c>null</c> if none was found.</param>
    /// <returns><c>true</c> if a URI was found; otherwise, <c>false</c>.</returns>
    internal static bool TryGetFirstUri(string text, out Uri? uri)
    {
        uri = null;

        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        Match match = UriRegex().Match(text);
        if (match.Success && Uri.TryCreate(match.Value, UriKind.Absolute, out Uri? parsed))
        {
            uri = parsed;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Splits the given text into segments of plain text and detected URIs.
    /// </summary>
    /// <param name="text">The text to segment.</param>
    /// <returns>
    /// A list of <see cref="TextSegment"/> instances representing alternating
    /// plain text and URI portions of the input.
    /// </returns>
    internal static IReadOnlyList<TextSegment> GetTextSegments(string text)
    {
        var segments = new List<TextSegment>();

        if (string.IsNullOrEmpty(text))
        {
            return segments;
        }

        int lastIndex = 0;

        foreach (Match match in UriRegex().Matches(text))
        {
            if (match.Index > lastIndex)
            {
                segments.Add(new TextSegment(text[lastIndex..match.Index]));
            }

            if (Uri.TryCreate(match.Value, UriKind.Absolute, out Uri? uri))
            {
                segments.Add(new TextSegment(match.Value, uri));
            }
            else
            {
                segments.Add(new TextSegment(match.Value));
            }

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
        {
            segments.Add(new TextSegment(text[lastIndex..]));
        }

        return segments;
    }

    [GeneratedRegex(@"https?://[^\s<>""')\]]+", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex UriRegex();
}
