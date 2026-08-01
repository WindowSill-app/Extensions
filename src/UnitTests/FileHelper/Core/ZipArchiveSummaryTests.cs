using FluentAssertions;

using WindowSill.FileHelper.Core;

namespace UnitTests.FileHelper.Core;

public class ZipArchiveSummaryTests
{
    [Fact]
    internal void CompressionRatio_ComputesRatio_WhenUncompressedSizeIsPositive()
    {
        var summary = new ZipArchiveSummary(FileCount: 2, FolderCount: 0, CompressedSizeInBytes: 50, UncompressedSizeInBytes: 200);

        summary.CompressionRatio.Should().Be(0.25d);
        summary.HasMeaningfulCompressionRatio.Should().BeTrue();
    }

    [Fact]
    internal void CompressionRatio_IsZero_AndNotMeaningful_ForEmptyArchive()
    {
        var summary = new ZipArchiveSummary(FileCount: 0, FolderCount: 0, CompressedSizeInBytes: 0, UncompressedSizeInBytes: 0);

        summary.CompressionRatio.Should().Be(0d);
        summary.HasMeaningfulCompressionRatio.Should().BeFalse();
        summary.IsEmpty.Should().BeTrue();
    }

    [Fact]
    internal void CompressionRatio_IsZero_AndNotMeaningful_ForArchiveOfOnlyZeroByteEntries()
    {
        // An archive containing only empty files and/or folders has nothing to compute a ratio from.
        var summary = new ZipArchiveSummary(FileCount: 1, FolderCount: 2, CompressedSizeInBytes: 0, UncompressedSizeInBytes: 0);

        summary.HasMeaningfulCompressionRatio.Should().BeFalse();
        summary.IsEmpty.Should().BeFalse();
    }

    [Fact]
    internal void IsEmpty_IsFalse_WhenArchiveContainsOnlyFolders()
    {
        var summary = new ZipArchiveSummary(FileCount: 0, FolderCount: 3, CompressedSizeInBytes: 0, UncompressedSizeInBytes: 0);

        summary.IsEmpty.Should().BeFalse();
    }

    [Fact]
    internal void CompressionEffectiveness_IsSmaller_WhenCompressedSizeIsLessThanUncompressedSize()
    {
        var summary = new ZipArchiveSummary(FileCount: 2, FolderCount: 0, CompressedSizeInBytes: 50, UncompressedSizeInBytes: 200);

        summary.CompressionEffectiveness.Should().Be(ZipCompressionEffectiveness.Smaller);
        summary.CompressionPercentage.Should().Be(75);
    }

    [Fact]
    internal void CompressionEffectiveness_IsLarger_WhenCompressedSizeExceedsUncompressedSize()
    {
        // Small or already-compressed content plus ZIP container overhead can make the compressed size bigger
        // than the original — the UI must phrase this as "X% larger", not a misleading "129% of original size".
        var summary = new ZipArchiveSummary(FileCount: 1, FolderCount: 0, CompressedSizeInBytes: 129, UncompressedSizeInBytes: 100);

        summary.CompressionEffectiveness.Should().Be(ZipCompressionEffectiveness.Larger);
        summary.CompressionPercentage.Should().Be(29);
    }

    [Fact]
    internal void CompressionEffectiveness_IsEqual_WhenCompressedSizeMatchesUncompressedSize()
    {
        var summary = new ZipArchiveSummary(FileCount: 1, FolderCount: 0, CompressedSizeInBytes: 100, UncompressedSizeInBytes: 100);

        summary.CompressionEffectiveness.Should().Be(ZipCompressionEffectiveness.Equal);
        summary.CompressionPercentage.Should().Be(0);
    }

    [Fact]
    internal void CompressionEffectiveness_IsNotAvailable_WhenRatioIsNotMeaningful()
    {
        var summary = new ZipArchiveSummary(FileCount: 0, FolderCount: 0, CompressedSizeInBytes: 0, UncompressedSizeInBytes: 0);

        summary.CompressionEffectiveness.Should().Be(ZipCompressionEffectiveness.NotAvailable);
        summary.CompressionPercentage.Should().Be(0);
    }
}
