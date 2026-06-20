using Microsoft.Extensions.Logging;

using NPSMLib;

using WindowSill.API;

namespace WindowSill.MediaControl.Core;

/// <inheritdoc />
internal sealed class MediaSessionService : IMediaSessionService
{
    private readonly Lock _lock = new();
    private readonly ILogger _logger;

    private NowPlayingSessionManager? _sessionManager;
    private NowPlayingSession? _currentSession;
    private MediaPlaybackDataSource? _mediaPlaybackDataSource;

    // Last reported track identity, used to detect track switches so the thumbnail (an expensive
    // decode) is only fetched when it actually changes rather than on every playback tick.
    private string? _lastTitle;
    private string? _lastArtist;
    private bool _forceThumbnailRefresh;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaSessionService"/> class.
    /// </summary>
    public MediaSessionService()
    {
        _logger = this.Log();
    }

    /// <inheritdoc />
    public event EventHandler<MediaInfoChangedEventArgs>? MediaInfoChanged;

    /// <inheritdoc />
    public nint? CurrentSessionWindowHandle
    {
        get
        {
            lock (_lock)
            {
                return _currentSession?.Hwnd;
            }
        }
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            _sessionManager = new NowPlayingSessionManager();
            _sessionManager.SessionListChanged += OnSessionListChanged;
            await UpdateSessionAsync(_sessionManager.CurrentSession);
        });
    }

    /// <inheritdoc />
    public void RequestUpdate()
    {
        // The sill layout (and therefore the required thumbnail size) may have changed, so force a
        // thumbnail refresh on the next update even if the track is unchanged.
        _forceThumbnailRefresh = true;
        UpdateSessionAsync(_sessionManager?.CurrentSession).ForgetSafely();
    }

    /// <inheritdoc />
    public void PlayPause()
    {
        try
        {
            _mediaPlaybackDataSource?.SendMediaPlaybackCommand(MediaPlaybackCommands.PlayPauseToggle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while trying to play or pause media.");
        }
    }

    /// <inheritdoc />
    public void Next()
    {
        try
        {
            _mediaPlaybackDataSource?.SendMediaPlaybackCommand(MediaPlaybackCommands.Next);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while trying to play next media.");
        }
    }

    /// <inheritdoc />
    public void Previous()
    {
        try
        {
            _mediaPlaybackDataSource?.SendMediaPlaybackCommand(MediaPlaybackCommands.Previous);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while trying to play previous media.");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_sessionManager is not null)
        {
            _sessionManager.SessionListChanged -= OnSessionListChanged;
        }

        lock (_lock)
        {
            if (_mediaPlaybackDataSource is not null)
            {
                try
                {
                    _mediaPlaybackDataSource.MediaPlaybackDataChanged -= OnMediaPlaybackDataChanged;
                }
                catch
                {
                    // Best effort cleanup.
                }
            }
        }
    }

    private void OnSessionListChanged(object? sender, NowPlayingSessionManagerEventArgs e)
    {
        UpdateSessionAsync(_sessionManager?.CurrentSession).ForgetSafely();
    }

    private void OnMediaPlaybackDataChanged(object? sender, MediaPlaybackDataChangedArgs e)
    {
        // Fired very frequently (e.g. on every playback position update) by the active data source.
        // Only refresh media info from the already-activated data source. Re-querying
        // NowPlayingSessionManager.CurrentSession here re-materializes COM objects on every tick,
        // which is both CPU-heavy and a major source of finalizer/RCW churn.
        RefreshMediaInfoAsync().ForgetSafely();
    }

    /// <summary>
    /// Determines whether two sessions refer to the same underlying media source. NPSMLib returns a
    /// new wrapper instance on each <c>CurrentSession</c> access, so reference equality is unreliable
    /// and would cause the data source to be torn down and re-activated on every update.
    /// </summary>
    private static bool IsSameSession(NowPlayingSession? a, NowPlayingSession? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.PID == b.PID
            && a.Hwnd == b.Hwnd
            && string.Equals(a.SourceAppId, b.SourceAppId, StringComparison.Ordinal);
    }

    /// <summary>
    /// Resolves the active session, swapping the cached data source only when the session actually
    /// changes, then refreshes the reported media information.
    /// </summary>
    private async Task UpdateSessionAsync(NowPlayingSession? session)
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            lock (_lock)
            {
                if (!IsSameSession(session, _currentSession))
                {
                    if (_mediaPlaybackDataSource is not null)
                    {
                        try
                        {
                            _mediaPlaybackDataSource.MediaPlaybackDataChanged -= OnMediaPlaybackDataChanged;
                        }
                        catch
                        {
                            // Best effort cleanup.
                        }
                    }

                    _currentSession = session;
                    _mediaPlaybackDataSource = null;

                    if (_currentSession is not null)
                    {
                        try
                        {
                            _mediaPlaybackDataSource = _currentSession.ActivateMediaPlaybackDataSource();
                            _mediaPlaybackDataSource.MediaPlaybackDataChanged += OnMediaPlaybackDataChanged;
                        }
                        catch
                        {
                            // Session may have been disposed between enumeration and activation.
                        }
                    }

                    // A new session means new artwork, so force a thumbnail refresh.
                    _forceThumbnailRefresh = true;
                }
            }

            RaiseMediaInfoOnUIThread();
        });
    }

    /// <summary>
    /// Refreshes the reported media information from the already-activated data source without
    /// touching the session manager. Used for high-frequency playback data updates.
    /// </summary>
    private Task RefreshMediaInfoAsync()
    {
        return ThreadHelper.RunOnUIThreadAsync(RaiseMediaInfoOnUIThread);
    }

    private void RaiseMediaInfoOnUIThread()
    {
        NowPlayingSession? currentSession;
        MediaPlaybackDataSource? mediaPlaybackDataSource;
        lock (_lock)
        {
            currentSession = _currentSession;
            mediaPlaybackDataSource = _mediaPlaybackDataSource;
        }

        try
        {
            if (currentSession is not null && mediaPlaybackDataSource is not null)
            {
                try
                {
                    MediaObjectInfo mediaInfo = mediaPlaybackDataSource.GetMediaObjectInfo();
                    MediaPlaybackInfo playbackInfo = mediaPlaybackDataSource.GetMediaPlaybackInfo();

                    // Only fetch and decode the thumbnail when the track actually changed (or a
                    // refresh was explicitly requested), not on every playback position tick.
                    bool thumbnailChanged
                        = _forceThumbnailRefresh
                        || !string.Equals(mediaInfo.Title, _lastTitle, StringComparison.Ordinal)
                        || !string.Equals(mediaInfo.Artist, _lastArtist, StringComparison.Ordinal);
                    _forceThumbnailRefresh = false;
                    _lastTitle = mediaInfo.Title;
                    _lastArtist = mediaInfo.Artist;

                    Stream? thumbnailStream = null;
                    if (thumbnailChanged)
                    {
                        try
                        {
                            thumbnailStream = mediaPlaybackDataSource.GetThumbnailStream();
                        }
                        catch
                        {
                            // Thumbnail retrieval can fail if the media source doesn't provide one.
                        }
                    }

                    MediaInfoChanged?.Invoke(this, new MediaInfoChangedEventArgs
                    {
                        SongTitle = mediaInfo.Title,
                        ArtistName = mediaInfo.Artist,
                        PlaybackInfo = playbackInfo,
                        ThumbnailStream = thumbnailStream,
                        ThumbnailChanged = thumbnailChanged,
                    });

                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while trying to retrieve media information.");
                }
            }

            // No active session or retrieval failed — reset state.
            _lastTitle = null;
            _lastArtist = null;
            _forceThumbnailRefresh = false;
            MediaInfoChanged?.Invoke(this, new MediaInfoChangedEventArgs
            {
                SongTitle = null,
                ArtistName = null,
                PlaybackInfo = null,
                ThumbnailStream = null,
                ThumbnailChanged = true,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while trying to update media information.");
        }
    }
}
