using FluentAssertions;
using UnitTests.Date.Core.Fakes;
using WindowSill.Date.Core.Models;

namespace UnitTests.Date.Core;

public class VideoCallProviderExtensionsTests
{
    public VideoCallProviderExtensionsTests()
    {
        LocalizerSetup.EnsureInitialized();
    }

    [Theory]
    [InlineData(VideoCallProvider.MicrosoftTeams, "Microsoft Teams")]
    [InlineData(VideoCallProvider.Zoom, "Zoom")]
    [InlineData(VideoCallProvider.GoogleMeet, "Google Meet")]
    [InlineData(VideoCallProvider.Webex, "Webex")]
    [InlineData(VideoCallProvider.Slack, "Slack")]
    [InlineData(VideoCallProvider.FaceTime, "FaceTime")]
    public void GetDisplayName_KnownProvider_ReturnsName(VideoCallProvider provider, string expected)
    {
        provider.GetDisplayName().Should().Be(expected);
    }

    [Fact]
    public void GetDisplayName_Other_ReturnsNull()
    {
        VideoCallProvider.Other.GetDisplayName().Should().BeNull();
    }

    [Theory]
    [InlineData(VideoCallProvider.MicrosoftTeams)]
    [InlineData(VideoCallProvider.Zoom)]
    [InlineData(VideoCallProvider.GoogleMeet)]
    public void GetJoinButtonText_KnownProvider_DoesNotThrow(VideoCallProvider provider)
    {
        // Localized strings may not load in tests — verify it doesn't throw.
        Action act = () => provider.GetJoinButtonText();

        act.Should().NotThrow();
    }

    [Fact]
    public void GetJoinButtonText_Other_DoesNotThrow()
    {
        Action act = () => VideoCallProvider.Other.GetJoinButtonText();

        act.Should().NotThrow();
    }
}
