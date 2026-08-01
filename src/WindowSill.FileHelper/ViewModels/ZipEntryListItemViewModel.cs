namespace WindowSill.FileHelper.ViewModels;

/// <summary>
/// Immutable display item for a single ZIP entry shown in the details popup's entry list. All strings are already
/// localized and formatted, so the view can bind directly without further conversion. The compression-outcome
/// booleans let the view color the outcome text consistently with the inline summary's headline.
/// </summary>
/// <param name="Name">The entry's file name (last path segment).</param>
/// <param name="RelativePath">The entry's full relative path, used as an accessible/hover description.</param>
/// <param name="SizesText">The formatted "uncompressed → compressed" size line (e.g. "12.3 MB → 4.1 MB").</param>
/// <param name="CompressionText">
/// The formatted per-entry compression outcome (e.g. "67% smaller", "3% larger", "Same size", or "—" when not
/// applicable to a zero-byte entry).
/// </param>
/// <param name="IsSmaller">Whether this entry compressed smaller than its original size (positive outcome).</param>
/// <param name="IsLarger">Whether this entry ended up larger than its original size (counter-intuitive outcome).</param>
internal sealed record ZipEntryListItemViewModel(
    string Name,
    string RelativePath,
    string SizesText,
    string CompressionText,
    bool IsSmaller,
    bool IsLarger);
