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
    /// Gets a value indicating whether the media is currently playing.
    /// </summary>
    public required bool IsPlaying { get; init; }

    /// <summary>
    /// Gets a value indicating whether the next command is available.
    /// </summary>
    public required bool IsNextEnabled { get; init; }

    /// <summary>
    /// Gets a value indicating whether the previous command is available.
    /// </summary>
    public required bool IsPreviousEnabled { get; init; }

    /// <summary>
    /// Gets a value indicating whether the play/pause toggle command is available.
    /// </summary>
    public required bool IsPlayPauseToggleEnabled { get; init; }

    /// <summary>
    /// Gets the thumbnail stream. The consumer is responsible for disposing this stream.
    /// </summary>
    public required Stream? ThumbnailStream { get; init; }

    /// <summary>
    /// Gets a value indicating whether the thumbnail changed since the last update. When
    /// <see langword="false"/>, the consumer should keep its existing thumbnail and avoid the
    /// expensive decode pipeline. This guards against re-decoding artwork on every playback
    /// position tick, when only the track switch actually changes the thumbnail.
    /// </summary>
    public required bool ThumbnailChanged { get; init; }
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
