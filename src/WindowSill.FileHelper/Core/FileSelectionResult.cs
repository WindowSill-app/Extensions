using Windows.Storage;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// The outcome of classifying a file selection: which FileHelper experience applies, the files it applies to, and —
/// for a document selection — the format those files share.
/// </summary>
/// <param name="Kind">The experience the selection activates.</param>
/// <param name="Files">The files relevant to <paramref name="Kind"/>; empty when nothing matched.</param>
/// <param name="DocumentFileFormat">
/// The format every selected file shares, when <paramref name="Kind"/> is <see cref="FileSelectionKind.Document"/>;
/// otherwise <see langword="null"/>.
/// </param>
/// <param name="IsTextSelection">
/// Whether every selected file is plain text. This is independent of <paramref name="Kind"/>: a <c>.txt</c> file is
/// both convertible to other document formats and re-encodable, so it offers both experiences, while a <c>.json</c>
/// file offers only the text one.
/// </param>
internal sealed record FileSelectionResult(
    FileSelectionKind Kind,
    IReadOnlyList<IStorageFile> Files,
    DocumentFileFormat? DocumentFileFormat,
    bool IsTextSelection = false)
{
    /// <summary>
    /// Gets the result representing a selection that activates nothing.
    /// </summary>
    internal static FileSelectionResult None { get; } = new(FileSelectionKind.None, [], null);

    /// <summary>
    /// Gets a value indicating whether the selection offers any FileHelper experience at all.
    /// </summary>
    /// <remarks>
    /// <see cref="Kind"/> alone is not enough: the text experience is independent of it, so a selection of
    /// <c>.json</c> files has <see cref="FileSelectionKind.None"/> yet still has something to offer.
    /// </remarks>
    internal bool HasAnyExperience
        => Kind != FileSelectionKind.None || IsTextSelection;
}
