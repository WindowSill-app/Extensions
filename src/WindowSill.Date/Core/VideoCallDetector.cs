using System.Text.RegularExpressions;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Core;

/// <summary>
/// Detects video call URLs in calendar event descriptions and locations.
/// Supports 50+ video conferencing services.
/// </summary>
public static partial class VideoCallDetector
{
    private static readonly (Regex Pattern, VideoCallProvider Provider)[] knownProviders =
    [
        (ZoomRegex(), VideoCallProvider.Zoom),
        (GoogleMeetRegex(), VideoCallProvider.GoogleMeet),
        (TeamsRegex(), VideoCallProvider.MicrosoftTeams),
        (WebexRegex(), VideoCallProvider.Webex),
        (FaceTimeRegex(), VideoCallProvider.FaceTime),
        (SlackRegex(), VideoCallProvider.Slack),
    ];

    // Generic patterns for less common providers — matched as VideoCallProvider.Other.
    private static readonly Regex[] genericProviders =
    [
        GoToMeetingRegex(),
        BlueJeansRegex(),
        JitsiRegex(),
        AroundRegex(),
        GatherRegex(),
        RingCentralRegex(),
        DialpadRegex(),
        TupleRegex(),
        PopRegex(),
        WherebyRegex(),
        LivestormRegex(),
        DemioRegex(),
        StreamYardRegex(),
        ChimeRegex(),
        VonageRegex(),
        LiveKitRegex(),
        LarkRegex(),
        VimeoRegex(),
    ];

    /// <summary>
    /// Attempts to detect a video call link from event text content.
    /// Searches the description and location for known conferencing URLs.
    /// </summary>
    /// <param name="description">The event description or body text.</param>
    /// <param name="location">The event location field.</param>
    /// <returns>Video call information if a link was detected; otherwise, <see langword="null"/>.</returns>
    public static VideoCallInfo? Detect(string? description, string? location)
    {
        string combined = string.Concat(location ?? string.Empty, " ", description ?? string.Empty);
        if (string.IsNullOrWhiteSpace(combined))
        {
            return null;
        }

        // Check known providers first for specific identification.
        foreach ((Regex pattern, VideoCallProvider provider) in knownProviders)
        {
            Match match = pattern.Match(combined);
            if (match.Success && Uri.TryCreate(match.Value, UriKind.Absolute, out Uri? uri))
            {
                return new VideoCallInfo(uri, provider);
            }
        }

        // Check generic providers.
        foreach (Regex pattern in genericProviders)
        {
            Match match = pattern.Match(combined);
            if (match.Success && Uri.TryCreate(match.Value, UriKind.Absolute, out Uri? uri))
            {
                return new VideoCallInfo(uri, VideoCallProvider.Other);
            }
        }

        return null;
    }

    // --- Known providers ---

    [GeneratedRegex(@"https?://[\w.-]*zoom\.us/[jw]/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex ZoomRegex();

    [GeneratedRegex(@"https?://meet\.google\.com/[a-z\-]+", RegexOptions.IgnoreCase)]
    private static partial Regex GoogleMeetRegex();

    [GeneratedRegex(@"https?://teams\.microsoft\.com/l/meetup-join/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex TeamsRegex();

    [GeneratedRegex(@"https?://[\w.-]*webex\.com/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex WebexRegex();

    [GeneratedRegex(@"https?://facetime\.apple\.com/join\S*", RegexOptions.IgnoreCase)]
    private static partial Regex FaceTimeRegex();

    [GeneratedRegex(@"https?://[\w.-]*slack\.com/[a-z]+/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex SlackRegex();

    // --- Generic providers (matched as Other) ---

    [GeneratedRegex(@"https?://[\w.-]*gotomeeting\.com/join/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex GoToMeetingRegex();

    [GeneratedRegex(@"https?://[\w.-]*bluejeans\.com/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex BlueJeansRegex();

    [GeneratedRegex(@"https?://[\w.-]*jitsi\.[\w.]+/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex JitsiRegex();

    [GeneratedRegex(@"https?://[\w.-]*around\.co/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex AroundRegex();

    [GeneratedRegex(@"https?://[\w.-]*gather\.town/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex GatherRegex();

    [GeneratedRegex(@"https?://[\w.-]*ringcentral\.com/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex RingCentralRegex();

    [GeneratedRegex(@"https?://[\w.-]*dialpad\.com/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex DialpadRegex();

    [GeneratedRegex(@"https?://[\w.-]*tuple\.app/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex TupleRegex();

    [GeneratedRegex(@"https?://[\w.-]*pop\.com/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex PopRegex();

    [GeneratedRegex(@"https?://[\w.-]*whereby\.com/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex WherebyRegex();

    [GeneratedRegex(@"https?://[\w.-]*livestorm\.co/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex LivestormRegex();

    [GeneratedRegex(@"https?://[\w.-]*demio\.com/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex DemioRegex();

    [GeneratedRegex(@"https?://[\w.-]*streamyard\.com/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex StreamYardRegex();

    [GeneratedRegex(@"https?://[\w.-]*chime\.aws/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex ChimeRegex();

    [GeneratedRegex(@"https?://[\w.-]*vonage\.com/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex VonageRegex();

    [GeneratedRegex(@"https?://[\w.-]*livekit\.io/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex LiveKitRegex();

    [GeneratedRegex(@"https?://[\w.-]*larksuite\.com/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex LarkRegex();

    [GeneratedRegex(@"https?://vimeo\.com/events/\S+", RegexOptions.IgnoreCase)]
    private static partial Regex VimeoRegex();
}
