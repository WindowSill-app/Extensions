using FluentAssertions;

using WindowSill.VideoHelper.Core;

namespace UnitTests.VideoHelper.Core;

public class VideoPresetsTests
{
    [Fact]
    internal void Quality_UsesLibx264_WithLowCrf()
    {
        VideoCompressionOptions preset = VideoPresets.Quality;

        preset.VideoCodec.Should().Be("libx264");
        preset.Crf.Should().Be(18);
        preset.Preset.Should().Be("medium");
        preset.AudioBitrateKbps.Should().BeNull();
    }

    [Fact]
    internal void Balanced_UsesLibx264_WithModerateCrf()
    {
        VideoCompressionOptions preset = VideoPresets.Balanced;

        preset.VideoCodec.Should().Be("libx264");
        preset.Crf.Should().Be(23);
        preset.AudioBitrateKbps.Should().Be(128);
    }

    [Fact]
    internal void SmallFile_UsesHigherCrf()
    {
        VideoCompressionOptions preset = VideoPresets.SmallFile;

        preset.Crf.Should().Be(28);
        preset.Preset.Should().Be("fast");
        preset.AudioBitrateKbps.Should().Be(96);
    }

    [Fact]
    internal void MaxCompression_UsesHevc()
    {
        VideoCompressionOptions preset = VideoPresets.MaxCompression;

        preset.VideoCodec.Should().Be("libx265");
        preset.Crf.Should().Be(28);
    }

    [Fact]
    internal void AllPresets_ContainsFourEntries()
    {
        VideoPresets.AllPresets.Should().HaveCount(4);
        VideoPresets.AllPresets.Select(p => p.Name).Should()
            .ContainInOrder("Quality", "Balanced", "SmallFile", "MaxCompression");
    }
}
