using WindowSill.API;

namespace WindowSill.Date.Core.Models;

/// <summary>
/// Extension methods for <see cref="VideoCallProvider"/>.
/// </summary>
internal static class VideoCallProviderExtensions
{
    /// <summary>
    /// Gets the user-friendly display name for a video call provider (e.g., "Microsoft Teams").
    /// Returns <see langword="null"/> for <see cref="VideoCallProvider.Other"/>.
    /// </summary>
    public static string? GetDisplayName(this VideoCallProvider provider) => provider switch
    {
        VideoCallProvider.MicrosoftTeams => "Microsoft Teams",
        VideoCallProvider.Zoom => "Zoom",
        VideoCallProvider.GoogleMeet => "Google Meet",
        VideoCallProvider.Webex => "Webex",
        VideoCallProvider.Slack => "Slack",
        VideoCallProvider.FaceTime => "FaceTime",
        _ => null,
    };

    /// <summary>
    /// Gets a localized join button text like "Join on Microsoft Teams" or "Join meeting" for unknown providers.
    /// </summary>
    public static string GetJoinButtonText(this VideoCallProvider provider)
    {
        string? displayName = provider.GetDisplayName();
        return displayName is not null
            ? string.Format("/WindowSill.Date/Meetings/ToastJoinOnProvider".GetLocalizedString(), displayName)
            : "/WindowSill.Date/Meetings/ToastJoinButton".GetLocalizedString();
    }
}
