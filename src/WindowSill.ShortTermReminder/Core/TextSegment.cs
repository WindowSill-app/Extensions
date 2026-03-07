namespace WindowSill.ShortTermReminder.Core;

/// <summary>
/// Represents a segment of text that may optionally be a URI.
/// </summary>
/// <param name="Text">The raw text of this segment.</param>
/// <param name="Uri">The parsed URI if this segment is a link; otherwise, <c>null</c>.</param>
internal readonly record struct TextSegment(string Text, Uri? Uri = null)
{
    /// <summary>
    /// Gets a value indicating whether this segment represents a clickable URI.
    /// </summary>
    internal bool IsUri => Uri is not null;
}
