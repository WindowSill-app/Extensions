namespace WindowSill.Date.Core.Models;

/// <summary>
/// Contains video call join information extracted from a calendar event.
/// </summary>
/// <param name="JoinUrl">The URL to join the video call.</param>
/// <param name="Provider">The detected video call service provider.</param>
public sealed record VideoCallInfo(Uri JoinUrl, VideoCallProvider Provider);
