namespace WindowSill.Teams.Core.Models;

/// <param name="IsMuted"> Currently muted </param>
/// <param name="IsVideoOn"> Video is on </param>
/// <param name="IsHandRaised"> Hand raised </param>
/// <param name="IsInMeeting"> Currently in a meeting </param>
/// <param name="IsRecordingOn"> Current call is recorded </param>
/// <param name="IsBackgroundBlurred"> Background blur is on </param>
/// <param name="IsSharing"> User is sharing content </param>
/// <param name="HasUnreadMessages"> User has unread messages </param>
internal sealed record MeetingState(
    bool IsMuted,
    bool IsVideoOn,
    bool IsHandRaised,
    bool IsInMeeting,
    bool IsRecordingOn,
    bool IsBackgroundBlurred,
    bool IsSharing,
    bool HasUnreadMessages);
