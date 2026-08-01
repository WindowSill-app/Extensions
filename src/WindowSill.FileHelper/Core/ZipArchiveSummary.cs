namespace WindowSill.FileHelper.Core;

/// <summary>
/// Read-only metadata summary computed from a ZIP archive's central directory, without extracting any entry content.
/// </summary>
/// <param name="FileCount">The number of file entries (i.e. entries whose name does not end with a path separator).</param>
/// <param name="FolderCount">
/// The number of distinct folder paths in the archive, counting both explicit folder entries (entries whose name
/// ends with a path separator) and folders implied only by nested file paths (e.g. "a/b.txt" without an "a/"
/// entry). A folder implied by a file path and also present as an explicit entry is counted once.
/// </param>
/// <param name="CompressedSizeInBytes">The sum of the compressed size, in bytes, of every file entry.</param>
/// <param name="UncompressedSizeInBytes">The sum of the uncompressed size, in bytes, of every file entry.</param>
internal readonly record struct ZipArchiveSummary(
    int FileCount,
    int FolderCount,
    long CompressedSizeInBytes,
    long UncompressedSizeInBytes)
{
    /// <summary>
    /// Gets the ratio of <see cref="CompressedSizeInBytes"/> to <see cref="UncompressedSizeInBytes"/>, as a value
    /// between 0.0 (compressed away entirely) and 1.0+ (no compression gain, or negative gain).
    /// </summary>
    /// <remarks>
    /// Returns 0 when <see cref="UncompressedSizeInBytes"/> is 0 (empty archive, or an archive containing only
    /// zero-byte files/folders), since there is no meaningful ratio to compute in that case.
    /// </remarks>
    internal double CompressionRatio
        => UncompressedSizeInBytes == 0 ? 0d : (double)CompressedSizeInBytes / UncompressedSizeInBytes;

    /// <summary>
    /// Gets a value indicating whether the archive contains no entries at all.
    /// </summary>
    internal bool IsEmpty => FileCount == 0 && FolderCount == 0;

    /// <summary>
    /// Gets a value indicating whether <see cref="CompressionRatio"/> is meaningful (i.e. the archive contains
    /// at least one byte of uncompressed content). When false, the ratio should be displayed as "not applicable"
    /// rather than a misleading 0%.
    /// </summary>
    internal bool HasMeaningfulCompressionRatio => UncompressedSizeInBytes > 0;

    /// <summary>
    /// Gets a qualitative classification of how <see cref="CompressedSizeInBytes"/> compares to
    /// <see cref="UncompressedSizeInBytes"/>, so the UI can phrase the outcome accurately (e.g. "smaller" vs.
    /// "larger") instead of always presenting a raw percentage-of-original that reads oddly when compression made
    /// the archive bigger (e.g. "129% of original size" instead of "29% larger").
    /// </summary>
    internal ZipCompressionEffectiveness CompressionEffectiveness
    {
        get
        {
            if (!HasMeaningfulCompressionRatio)
            {
                return ZipCompressionEffectiveness.NotAvailable;
            }

            if (CompressedSizeInBytes < UncompressedSizeInBytes)
            {
                return ZipCompressionEffectiveness.Smaller;
            }

            if (CompressedSizeInBytes > UncompressedSizeInBytes)
            {
                return ZipCompressionEffectiveness.Larger;
            }

            return ZipCompressionEffectiveness.Equal;
        }
    }

    /// <summary>
    /// Gets the absolute percentage difference between <see cref="CompressedSizeInBytes"/> and
    /// <see cref="UncompressedSizeInBytes"/>, rounded to the nearest whole percent. Pair with
    /// <see cref="CompressionEffectiveness"/> to phrase the direction (smaller/larger) correctly. Returns 0 when
    /// <see cref="HasMeaningfulCompressionRatio"/> is false.
    /// </summary>
    internal int CompressionPercentage
        => HasMeaningfulCompressionRatio
            ? (int)Math.Round(Math.Abs(1d - CompressionRatio) * 100d, MidpointRounding.AwayFromZero)
            : 0;
}

/// <summary>
/// Qualitative outcome of comparing a ZIP archive's compressed size against its uncompressed size.
/// </summary>
internal enum ZipCompressionEffectiveness
{
    /// <summary>No meaningful ratio could be computed (e.g. the archive contains no uncompressed content).</summary>
    NotAvailable,

    /// <summary>The compressed size is smaller than the uncompressed size (the expected, beneficial case).</summary>
    Smaller,

    /// <summary>The compressed size is exactly equal to the uncompressed size.</summary>
    Equal,

    /// <summary>The compressed size is larger than the uncompressed size (e.g. small/incompressible/pre-compressed content plus ZIP overhead).</summary>
    Larger,
}

