using WindowSill.API;

using WindowSill.FileHelper.Core.Operations;

using Path = System.IO.Path;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// An adjustment FileHelper can make to a text file without changing what it says.
/// </summary>
internal enum TextAction
{
    /// <summary>Rewrite as UTF-8 without a byte order mark.</summary>
    Utf8,

    /// <summary>Rewrite as UTF-16.</summary>
    Utf16,

    /// <summary>Rewrite with Windows line endings.</summary>
    Crlf,

    /// <summary>Rewrite with Unix line endings.</summary>
    Lf,
}

/// <summary>
/// Describes one <see cref="TextAction"/> for display.
/// </summary>
/// <param name="Action">The action being described.</param>
/// <param name="ResourceKey">Key of the button label in <c>TextActions.resw</c>.</param>
internal sealed record TextActionInfo(TextAction Action, string ResourceKey)
{
    /// <summary>Gets the localized label for this action.</summary>
    internal string DisplayName => $"/WindowSill.FileHelper/TextActions/{ResourceKey}".GetLocalizedString();
}

/// <summary>
/// Decides which text adjustments apply, and turns a chosen one into the operations that carry it out.
/// </summary>
internal static class TextActionCatalog
{
    private static readonly TextActionInfo[] AvailableActions =
    [
        new(TextAction.Utf8, "Utf8"),
        new(TextAction.Utf16, "Utf16"),
        new(TextAction.Crlf, "Crlf"),
        new(TextAction.Lf, "Lf"),
    ];

    /// <summary>
    /// Gets the adjustments available for a text selection, in presentation order.
    /// </summary>
    /// <param name="fileCount">How many text files are selected.</param>
    internal static IReadOnlyList<TextActionInfo> GetActions(int fileCount)
        => fileCount <= 0 ? [] : AvailableActions;

    /// <summary>
    /// Builds one operation per selected file for the chosen adjustment.
    /// </summary>
    /// <param name="action">The adjustment to make.</param>
    /// <param name="sourcePaths">The selected text file paths.</param>
    internal static IReadOnlyList<IFileOperation> CreateOperations(TextAction action, IReadOnlyList<string> sourcePaths)
    {
        (TextEncodingKind? encoding, LineEndingKind? lineEnding, string suffixKey) = action switch
        {
            TextAction.Utf8 => ((TextEncodingKind?)TextEncodingKind.Utf8, (LineEndingKind?)null, "SuffixUtf8"),
            TextAction.Utf16 => (TextEncodingKind.Utf16, null, "SuffixUtf16"),
            TextAction.Crlf => (null, LineEndingKind.Crlf, "SuffixCrlf"),
            TextAction.Lf => (null, LineEndingKind.Lf, "SuffixLf"),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported text action."),
        };

        string suffix = $"/WindowSill.FileHelper/TextActions/{suffixKey}".GetLocalizedString();

        return [.. sourcePaths.Select(path => new RewriteTextFileOperation(path, encoding, lineEnding, suffix))];
    }

    /// <summary>
    /// Builds the label shown next to the queue's progress bar in the sill.
    /// </summary>
    /// <param name="sourcePaths">The selected text file paths.</param>
    internal static string GetProgressText(IReadOnlyList<string> sourcePaths)
        => sourcePaths.Count == 1
            ? string.Format("/WindowSill.FileHelper/TextActions/ProgressOne".GetLocalizedString(), Path.GetFileName(sourcePaths[0]))
            : string.Format("/WindowSill.FileHelper/TextActions/ProgressMany".GetLocalizedString(), sourcePaths.Count);
}
