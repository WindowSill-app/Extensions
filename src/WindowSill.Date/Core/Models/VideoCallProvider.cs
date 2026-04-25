namespace WindowSill.Date.Core.Models;

/// <summary>
/// Identifies the video conferencing service for a calendar event.
/// </summary>
public enum VideoCallProvider
{
    /// <summary>Zoom meeting.</summary>
    Zoom,

    /// <summary>Google Meet.</summary>
    GoogleMeet,

    /// <summary>Microsoft Teams.</summary>
    MicrosoftTeams,

    /// <summary>Cisco Webex.</summary>
    Webex,

    /// <summary>FaceTime call.</summary>
    FaceTime,

    /// <summary>Slack huddle or call.</summary>
    Slack,

    /// <summary>A video call provider not explicitly recognized.</summary>
    Other,
}
