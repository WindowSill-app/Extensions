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
            await UpdateAsync(_sessionManager.CurrentSession);
        });
    }

    /// <inheritdoc />
    public void RequestUpdate()
    {
        UpdateAsync(_sessionManager?.CurrentSession).ForgetSafely();
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
        UpdateAsync(_sessionManager?.CurrentSession).ForgetSafely();
    }

    private void OnMediaPlaybackDataChanged(object? sender, MediaPlaybackDataChangedArgs e)
    {
        UpdateAsync(_sessionManager?.CurrentSession).ForgetSafely();
    }

    private async Task UpdateAsync(NowPlayingSession? session)
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            lock (_lock)
            {
                if (session != _currentSession)
                {
                    if (_currentSession is not null && _mediaPlaybackDataSource is not null)
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
                }
            }

            NowPlayingSession? currentSession = _currentSession;
            MediaPlaybackDataSource? mediaPlaybackDataSource = _mediaPlaybackDataSource;

            try
            {
                if (currentSession is not null && mediaPlaybackDataSource is not null)
                {
                    try
                    {
                        MediaObjectInfo mediaInfo = mediaPlaybackDataSource.GetMediaObjectInfo();
                        MediaPlaybackInfo playbackInfo = mediaPlaybackDataSource.GetMediaPlaybackInfo();

                        Stream? thumbnailStream = null;
                        try
                        {
                            thumbnailStream = mediaPlaybackDataSource.GetThumbnailStream();
                        }
                        catch
                        {
                            // Thumbnail retrieval can fail if the media source doesn't provide one.
                        }

                        MediaInfoChanged?.Invoke(this, new MediaInfoChangedEventArgs
                        {
                            SongTitle = mediaInfo.Title,
                            ArtistName = mediaInfo.Artist,
                            PlaybackInfo = playbackInfo,
                            ThumbnailStream = thumbnailStream,
                        });

                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while trying to retrieve media information.");
                    }
                }

                // No active session or retrieval failed — reset state.
                MediaInfoChanged?.Invoke(this, new MediaInfoChangedEventArgs
                {
                    SongTitle = null,
                    ArtistName = null,
                    PlaybackInfo = null,
                    ThumbnailStream = null,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while trying to update media information.");
            }
        });
    }
}
