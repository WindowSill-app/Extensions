using FluentAssertions;

using WindowSill.FileHelper.Core;

namespace UnitTests.FileHelper.Core;

public class ZipEntryInfoTests
{
    [Fact]
    internal void CompressionEffectiveness_IsSmaller_WhenCompressedIsLessThanUncompressed()
    {
        var entry = new ZipEntryInfo("readme.txt", "docs/readme.txt", CompressedSizeInBytes: 40, UncompressedSizeInBytes: 100);

        entry.CompressionEffectiveness.Should().Be(ZipCompressionEffectiveness.Smaller);
        entry.CompressionPercentage.Should().Be(60);
        entry.HasMeaningfulCompressionRatio.Should().BeTrue();
    }

    [Fact]
    internal void CompressionEffectiveness_IsLarger_WhenCompressedExceedsUncompressed()
    {
        var entry = new ZipEntryInfo("tiny.bin", "tiny.bin", CompressedSizeInBytes: 130, UncompressedSizeInBytes: 100);

        entry.CompressionEffectiveness.Should().Be(ZipCompressionEffectiveness.Larger);
        entry.CompressionPercentage.Should().Be(30);
    }

    [Fact]
    internal void CompressionEffectiveness_IsEqual_WhenSizesMatch()
    {
        var entry = new ZipEntryInfo("stored.dat", "stored.dat", CompressedSizeInBytes: 100, UncompressedSizeInBytes: 100);

        entry.CompressionEffectiveness.Should().Be(ZipCompressionEffectiveness.Equal);
        entry.CompressionPercentage.Should().Be(0);
    }

    [Fact]
    internal void CompressionEffectiveness_IsNotAvailable_ForZeroByteEntry()
    {
        var entry = new ZipEntryInfo("empty.txt", "empty.txt", CompressedSizeInBytes: 0, UncompressedSizeInBytes: 0);

        entry.HasMeaningfulCompressionRatio.Should().BeFalse();
        entry.CompressionEffectiveness.Should().Be(ZipCompressionEffectiveness.NotAvailable);
        entry.CompressionPercentage.Should().Be(0);
    }
}
