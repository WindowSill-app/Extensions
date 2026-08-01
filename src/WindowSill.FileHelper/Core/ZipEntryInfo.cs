namespace WindowSill.FileHelper.Core;

/// <summary>
/// Read-only metadata for a single file entry inside a ZIP archive, computed from the archive's central directory
/// without extracting the entry's content.
/// </summary>
/// <param name="Name">The entry's file name only (the last path segment, e.g. "report.txt").</param>
/// <param name="RelativePath">
/// The entry's full, normalized ('/'-separated) path relative to the archive root (e.g. "docs/report.txt").
/// </param>
/// <param name="CompressedSizeInBytes">The entry's compressed size, in bytes.</param>
/// <param name="UncompressedSizeInBytes">The entry's uncompressed size, in bytes.</param>
internal readonly record struct ZipEntryInfo(
    string Name,
    string RelativePath,
    long CompressedSizeInBytes,
    long UncompressedSizeInBytes)
{
    /// <summary>
    /// Gets a value indicating whether this entry has any uncompressed content to compute a compression ratio from.
    /// When false, the per-entry compression outcome should be shown as "not available" rather than a misleading 0%.
    /// </summary>
    internal bool HasMeaningfulCompressionRatio => UncompressedSizeInBytes > 0;

    /// <summary>
    /// Gets a qualitative classification of how <see cref="CompressedSizeInBytes"/> compares to
    /// <see cref="UncompressedSizeInBytes"/> for this single entry, mirroring
    /// <see cref="ZipArchiveSummary.CompressionEffectiveness"/> at the entry level.
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
    /// <see cref="UncompressedSizeInBytes"/> for this entry, rounded to the nearest whole percent. Pair with
    /// <see cref="CompressionEffectiveness"/> to phrase the direction (smaller/larger). Returns 0 when
    /// <see cref="HasMeaningfulCompressionRatio"/> is false.
    /// </summary>
    internal int CompressionPercentage
        => HasMeaningfulCompressionRatio
            ? (int)Math.Round(Math.Abs(1d - ((double)CompressedSizeInBytes / UncompressedSizeInBytes)) * 100d, MidpointRounding.AwayFromZero)
            : 0;
}
