using NPSMLib;

namespace WindowSill.MediaControl.Core;

/// <summary>
/// Event data raised when the active media session's information changes.
/// </summary>
internal sealed class MediaInfoChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the song title, or <see langword="null"/> when no session is active.
    /// </summary>
    public required string? SongTitle { get; init; }

    /// <summary>
    /// Gets the artist name, or <see langword="null"/> when no session is active.
    /// </summary>
    public required string? ArtistName { get; init; }

    /// <summary>
    /// Gets the playback info, or <see langword="null"/> when no session is active.
    /// </summary>
    public required MediaPlaybackInfo? PlaybackInfo { get; init; }

    /// <summary>
    /// Gets the thumbnail stream. The consumer is responsible for disposing this stream.
    /// </summary>
    public required Stream? ThumbnailStream { get; init; }
}

/// <summary>
/// Manages the system media session lifecycle, playback commands, and media information updates.
/// </summary>
internal interface IMediaSessionService : IDisposable
{
    /// <summary>
    /// Raised on the UI thread whenever the active media session's information changes.
    /// </summary>
    event EventHandler<MediaInfoChangedEventArgs>? MediaInfoChanged;

    /// <summary>
    /// Gets the window handle of the application owning the current media session.
    /// </summary>
    nint? CurrentSessionWindowHandle { get; }

    /// <summary>
    /// Initializes the session manager and begins monitoring for media sessions.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Forces a re-evaluation of the current media information and raises <see cref="MediaInfoChanged"/>.
    /// </summary>
    void RequestUpdate();

    /// <summary>
    /// Sends a play/pause toggle command to the active media session.
    /// </summary>
    void PlayPause();

    /// <summary>
    /// Sends a next track command to the active media session.
    /// </summary>
    void Next();

    /// <summary>
    /// Sends a previous track command to the active media session.
    /// </summary>
    void Previous();
}
