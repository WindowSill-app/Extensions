using FluentAssertions;

using WindowSill.VideoHelper.Core;

namespace UnitTests.VideoHelper.Core;

public class VideoConversionOptionsTests
{
    [Theory]
    [InlineData("mp4", null, "libx264")]
    [InlineData("mkv", null, "libx264")]
    [InlineData("webm", null, "libvpx-vp9")]
    [InlineData("gif", null, "gif")]
    [InlineData("avi", null, "libx264")]
    [InlineData("mov", null, "libx264")]
    [InlineData("mp4", "libx265", "libx265")]
    [InlineData("webm", "libx264", "libx264")]
    internal void GetEffectiveVideoCodec_ReturnsExpected(string format, string? explicitCodec, string expected)
    {
        var options = new VideoConversionOptions
        {
            OutputFormat = format,
            VideoCodec = explicitCodec,
        };

        options.GetEffectiveVideoCodec().Should().Be(expected);
    }

    [Theory]
    [InlineData("mp4", true, null, "aac")]
    [InlineData("webm", true, null, "libopus")]
    [InlineData("mkv", true, null, "aac")]
    [InlineData("avi", true, null, "mp3")]
    [InlineData("mov", true, null, "aac")]
    [InlineData("gif", true, null, null)]
    [InlineData("mp4", false, null, null)]
    [InlineData("mp4", true, "libopus", "libopus")]
    internal void GetEffectiveAudioCodec_ReturnsExpected(
        string format, bool keepAudio, string? explicitCodec, string? expected)
    {
        var options = new VideoConversionOptions
        {
            OutputFormat = format,
            KeepAudio = keepAudio,
            AudioCodec = explicitCodec,
        };

        options.GetEffectiveAudioCodec().Should().Be(expected);
    }
}
