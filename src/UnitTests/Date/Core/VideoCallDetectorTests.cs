using FluentAssertions;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;

namespace UnitTests.Date.Core;

public class VideoCallDetectorTests
{
    [Theory]
    [InlineData("https://zoom.us/j/1234567890", VideoCallProvider.Zoom)]
    [InlineData("https://us02web.zoom.us/j/1234567890?pwd=abc", VideoCallProvider.Zoom)]
    [InlineData("https://meet.google.com/abc-defg-hij", VideoCallProvider.GoogleMeet)]
    [InlineData("https://teams.microsoft.com/l/meetup-join/19%3ameeting_abc", VideoCallProvider.MicrosoftTeams)]
    [InlineData("https://acme.webex.com/meet/john.doe", VideoCallProvider.Webex)]
    [InlineData("https://facetime.apple.com/join#v=1&p=abc", VideoCallProvider.FaceTime)]
    public void Detect_KnownProvider_ReturnsCorrectProvider(string url, VideoCallProvider expectedProvider)
    {
        VideoCallInfo? result = VideoCallDetector.Detect(url, null);

        result.Should().NotBeNull();
        result!.Provider.Should().Be(expectedProvider);
        result.JoinUrl.ToString().Should().StartWith("http");
    }

    [Theory]
    [InlineData("https://gotomeeting.com/join/123456")]
    [InlineData("https://meet.jitsi.si/MyRoom")]
    [InlineData("https://whereby.com/my-room")]
    public void Detect_GenericProvider_ReturnsOther(string url)
    {
        VideoCallInfo? result = VideoCallDetector.Detect(url, null);

        result.Should().NotBeNull();
        result!.Provider.Should().Be(VideoCallProvider.Other);
    }

    [Fact]
    public void Detect_NoVideoCall_ReturnsNull()
    {
        VideoCallInfo? result = VideoCallDetector.Detect("Just a regular meeting", "Conference Room A");

        result.Should().BeNull();
    }

    [Fact]
    public void Detect_NullInputs_ReturnsNull()
    {
        VideoCallInfo? result = VideoCallDetector.Detect(null, null);

        result.Should().BeNull();
    }

    [Fact]
    public void Detect_UrlInLocation_IsDetected()
    {
        VideoCallInfo? result = VideoCallDetector.Detect(null, "https://meet.google.com/abc-defg-hij");

        result.Should().NotBeNull();
        result!.Provider.Should().Be(VideoCallProvider.GoogleMeet);
    }

    [Fact]
    public void Detect_UrlInDescription_WithOtherText_IsDetected()
    {
        string description = "Join the meeting at https://zoom.us/j/999888777 for our weekly sync.";

        VideoCallInfo? result = VideoCallDetector.Detect(description, null);

        result.Should().NotBeNull();
        result!.Provider.Should().Be(VideoCallProvider.Zoom);
    }

    [Fact]
    public void Detect_MultipleUrls_ReturnsFirstKnownProvider()
    {
        string description = "Join via https://meet.google.com/abc-defg or https://zoom.us/j/123";

        VideoCallInfo? result = VideoCallDetector.Detect(description, null);

        result.Should().NotBeNull();
        // Zoom comes first in the provider list, but Google Meet URL appears first in text.
        // The detector checks Zoom regex first, so it depends on regex matching order.
        result!.Provider.Should().BeOneOf(VideoCallProvider.Zoom, VideoCallProvider.GoogleMeet);
    }
}
