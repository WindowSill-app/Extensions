using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WindowSill.TextFinder.Core;

/// <summary>
/// Represents a search result with match location and surrounding context.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal sealed partial class FindResult : ObservableObject
{
    /// <summary>
    /// Gets or sets the match position and length in the source text.
    /// </summary>
    [ObservableProperty]
    internal partial TextSpan Match { get; set; }

    /// <summary>
    /// Gets or sets a snippet of text surrounding the match for display.
    /// </summary>
    [ObservableProperty]
    internal partial string PreviewText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the match position and length within the preview text.
    /// </summary>
    [ObservableProperty]
    internal partial TextSpan MatchInPreview { get; set; }

    private string GetDebuggerDisplay()
    {
        return PreviewText;
    }

    public override string ToString()
    {
        return PreviewText;
    }
}
