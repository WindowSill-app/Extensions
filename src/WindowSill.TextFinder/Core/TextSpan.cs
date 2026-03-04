namespace WindowSill.TextFinder.Core;

/// <summary>
/// Represents a text span with its position and length.
/// </summary>
/// <param name="Index">The zero-based starting position of the span in the source text.</param>
/// <param name="Length">The length of the span.</param>
internal readonly record struct TextSpan(int Index, int Length);
