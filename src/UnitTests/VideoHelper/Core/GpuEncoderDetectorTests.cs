using FluentAssertions;

using WindowSill.VideoHelper.Core;

namespace UnitTests.VideoHelper.Core;

public class GpuEncoderDetectorTests
{
    [Theory]
    [InlineData("h264_nvenc", true)]
    [InlineData("hevc_nvenc", true)]
    [InlineData("av1_nvenc", true)]
    [InlineData("h264_amf", true)]
    [InlineData("hevc_amf", true)]
    [InlineData("h264_qsv", true)]
    [InlineData("libx264", false)]
    [InlineData("libx265", false)]
    [InlineData("libsvtav1", false)]
    [InlineData("gif", false)]
    internal void IsGpuEncoder_ReturnsExpected(string encoder, bool expected)
    {
        GpuEncoderDetector.IsGpuEncoder(encoder).Should().Be(expected);
    }

    [Theory]
    [InlineData("libx264", "h264_nvenc")]
    [InlineData("libx265", "hevc_nvenc")]
    [InlineData("libsvtav1", "av1_nvenc")]
    internal void GetGpuEncoder_PrefersNvenc_WhenAllAvailable(string softwareCodec, string expected)
    {
        HashSet<string> available = new(StringComparer.OrdinalIgnoreCase)
        {
            "h264_nvenc", "h264_amf", "h264_qsv",
            "hevc_nvenc", "hevc_amf", "hevc_qsv",
            "av1_nvenc", "av1_amf", "av1_qsv",
        };

        GpuEncoderDetector.GetGpuEncoder(softwareCodec, available).Should().Be(expected);
    }

    [Fact]
    internal void GetGpuEncoder_FallsBackToSoftware_WhenNoneAvailable()
    {
        HashSet<string> empty = [];

        GpuEncoderDetector.GetGpuEncoder("libx264", empty).Should().Be("libx264");
    }

    [Fact]
    internal void GetGpuEncoder_PicksAmf_WhenNvencUnavailable()
    {
        HashSet<string> available = new(StringComparer.OrdinalIgnoreCase) { "h264_amf", "h264_qsv" };

        GpuEncoderDetector.GetGpuEncoder("libx264", available).Should().Be("h264_amf");
    }

    [Fact]
    internal void GetGpuEncoder_PicksQsv_WhenOnlyQsvAvailable()
    {
        HashSet<string> available = new(StringComparer.OrdinalIgnoreCase) { "hevc_qsv" };

        GpuEncoderDetector.GetGpuEncoder("libx265", available).Should().Be("hevc_qsv");
    }

    [Fact]
    internal void GetGpuEncoder_ReturnsOriginal_ForUnknownCodec()
    {
        HashSet<string> available = new(StringComparer.OrdinalIgnoreCase) { "h264_nvenc" };

        GpuEncoderDetector.GetGpuEncoder("gif", available).Should().Be("gif");
    }

    // BuildQualityArgs tests

    [Theory]
    [InlineData("h264_nvenc", 23, "medium", "-rc constqp -qp 23 -preset p4")]
    [InlineData("hevc_nvenc", 18, "ultrafast", "-rc constqp -qp 18 -preset p1")]
    [InlineData("av1_nvenc", 28, "veryslow", "-rc constqp -qp 28 -preset p7")]
    [InlineData("h264_nvenc", 23, "fast", "-rc constqp -qp 23 -preset p2")]
    [InlineData("h264_nvenc", 23, "slow", "-rc constqp -qp 23 -preset p5")]
    internal void BuildQualityArgs_Nvenc(string encoder, int crf, string preset, string expected)
    {
        GpuEncoderDetector.BuildQualityArgs(encoder, crf, preset).Should().Be(expected);
    }

    [Theory]
    [InlineData("h264_amf", 23, "medium", "-rc cqp -qp_i 23 -qp_p 23 -quality balanced")]
    [InlineData("hevc_amf", 18, "fast", "-rc cqp -qp_i 18 -qp_p 18 -quality speed")]
    [InlineData("h264_amf", 28, "slow", "-rc cqp -qp_i 28 -qp_p 28 -quality quality")]
    internal void BuildQualityArgs_Amf(string encoder, int crf, string preset, string expected)
    {
        GpuEncoderDetector.BuildQualityArgs(encoder, crf, preset).Should().Be(expected);
    }

    [Theory]
    [InlineData("h264_qsv", 23, "medium", "-global_quality 23 -preset medium")]
    [InlineData("hevc_qsv", 18, "ultrafast", "-global_quality 18 -preset veryfast")]
    internal void BuildQualityArgs_Qsv(string encoder, int crf, string preset, string expected)
    {
        GpuEncoderDetector.BuildQualityArgs(encoder, crf, preset).Should().Be(expected);
    }

    [Theory]
    [InlineData("libsvtav1", 30, "medium", "-crf 30 -preset 5")]
    [InlineData("libsvtav1", 20, "ultrafast", "-crf 20 -preset 12")]
    [InlineData("libsvtav1", 25, "veryslow", "-crf 25 -preset 2")]
    internal void BuildQualityArgs_SvtAv1(string encoder, int crf, string preset, string expected)
    {
        GpuEncoderDetector.BuildQualityArgs(encoder, crf, preset).Should().Be(expected);
    }

    [Theory]
    [InlineData("libx264", 23, "medium", "-crf 23 -preset medium")]
    [InlineData("libx265", 28, "fast", "-crf 28 -preset fast")]
    internal void BuildQualityArgs_Software(string encoder, int crf, string preset, string expected)
    {
        GpuEncoderDetector.BuildQualityArgs(encoder, crf, preset).Should().Be(expected);
    }
}
