using Microsoft.Extensions.Logging;

using Windows.Media.Control;
using Windows.Storage.Streams;

using WindowSill.API;

namespace WindowSill.MediaControl.Core;

/// <inheritdoc />
internal sealed class MediaSessionService : IMediaSessionService
{
    private readonly ILogger _logger;

    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private bool _initialized;

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
    public nint? CurrentSessionWindowHandle => null;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        try
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;
            UpdateSession(_sessionManager.GetCurrentSession());
        }
        catch (Exception ex)
        {
            _sessionManager = null;
            _logger.LogError(ex, "Failed to initialize the media session manager. Media controls will be unavailable.");
            RaiseNoMedia();
        }
    }

    /// <inheritdoc />
    public void RequestUpdate()
    {
        _forceThumbnailRefresh = true;
        RefreshMediaInfoAsync().ForgetSafely();
    }

    /// <inheritdoc />
    public async void PlayPause()
    {
        try
        {
            if (_currentSession is not null)
            {
                await _currentSession.TryTogglePlayPauseAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while trying to play or pause media.");
        }
    }

    /// <inheritdoc />
    public async void Next()
    {
        try
        {
            if (_currentSession is not null)
            {
                await _currentSession.TrySkipNextAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while trying to play next media.");
        }
    }

    /// <inheritdoc />
    public async void Previous()
    {
        try
        {
            if (_currentSession is not null)
            {
                await _currentSession.TrySkipPreviousAsync();
            }
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
            _sessionManager.CurrentSessionChanged -= OnCurrentSessionChanged;
        }

        if (_currentSession is not null)
        {
            _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        }
    }

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        ThreadHelper.RunOnUIThreadAsync(() =>
        {
            UpdateSession(_sessionManager?.GetCurrentSession());
        }).ForgetSafely();
    }

    private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        RefreshMediaInfoAsync().ForgetSafely();
    }

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        RefreshMediaInfoAsync().ForgetSafely();
    }

    private void UpdateSession(GlobalSystemMediaTransportControlsSession? session)
    {
        if (session != _currentSession)
        {
            if (_currentSession is not null)
            {
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            }

            _currentSession = session;

            if (_currentSession is not null)
            {
                _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
            }

            _forceThumbnailRefresh = true;
        }

        RefreshMediaInfoAsync().ForgetSafely();
    }

    private async Task RefreshMediaInfoAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            try
            {
                if (_currentSession is null)
                {
                    RaiseNoMedia();
                    return;
                }

                GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProperties = null;
                try
                {
                    mediaProperties = await _currentSession.TryGetMediaPropertiesAsync();
                }
                catch
                {
                    // Session may have been disposed.
                }

                if (mediaProperties is null)
                {
                    RaiseNoMedia();
                    return;
                }

                string? title = string.IsNullOrEmpty(mediaProperties.Title) ? null : mediaProperties.Title;
                string? artist = string.IsNullOrEmpty(mediaProperties.Artist) ? null : mediaProperties.Artist;

                bool thumbnailChanged
                    = _forceThumbnailRefresh
                    || !string.Equals(title, _lastTitle, StringComparison.Ordinal)
                    || !string.Equals(artist, _lastArtist, StringComparison.Ordinal);
                _forceThumbnailRefresh = false;
                _lastTitle = title;
                _lastArtist = artist;

                Stream? thumbnailStream = null;
                if (thumbnailChanged)
                {
                    try
                    {
                        IRandomAccessStreamReference? thumbnailRef = mediaProperties.Thumbnail;
                        if (thumbnailRef is not null)
                        {
                            IRandomAccessStreamWithContentType stream = await thumbnailRef.OpenReadAsync();
                            thumbnailStream = stream.AsStreamForRead();
                        }
                    }
                    catch
                    {
                        // Thumbnail retrieval can fail if the media source doesn't provide one.
                    }
                }

                GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackInfo = _currentSession.GetPlaybackInfo();
                GlobalSystemMediaTransportControlsSessionPlaybackControls controls = playbackInfo.Controls;

                MediaInfoChanged?.Invoke(this, new MediaInfoChangedEventArgs
                {
                    SongTitle = title,
                    ArtistName = artist,
                    IsPlaying = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                    IsNextEnabled = controls.IsNextEnabled,
                    IsPreviousEnabled = controls.IsPreviousEnabled,
                    IsPlayPauseToggleEnabled = controls.IsPlayPauseToggleEnabled,
                    ThumbnailStream = thumbnailStream,
                    ThumbnailChanged = thumbnailChanged,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while trying to update media information.");
            }
        });
    }

    private void RaiseNoMedia()
    {
        _lastTitle = null;
        _lastArtist = null;
        _forceThumbnailRefresh = false;
        MediaInfoChanged?.Invoke(this, new MediaInfoChangedEventArgs
        {
            SongTitle = null,
            ArtistName = null,
            IsPlaying = false,
            IsNextEnabled = false,
            IsPreviousEnabled = false,
            IsPlayPauseToggleEnabled = false,
            ThumbnailStream = null,
            ThumbnailChanged = true,
        });
    }
}
