using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;

using NPSMLib;

using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using WindowSill.API;

namespace WindowSill.MediaControl;

internal sealed partial class MediaControlViewModel : ObservableObject
{
    private const uint HorizontalThumbnailSize = 40;
    private const uint VerticalThumbnailSize = 64;

    private readonly Lock _lock = new();
    private readonly ILogger _logger;
    private readonly ISettingsProvider _settingsProvider;

    private NowPlayingSessionManager? _sessionManager;
    private NowPlayingSession? _currentSession;
    private MediaPlaybackDataSource? _mediaPlaybackDataSource;

    public MediaControlViewModel(ISettingsProvider settingsProvider)
    {
        _logger = this.Log();
        _settingsProvider = settingsProvider;
        _settingsProvider.SettingChanged += SettingsProvider_SettingChanged;
        ShowTitleArtistAndThumbnail = _settingsProvider.GetSetting(Settings.Settings.ShowTitleArtistAndThumbnail);

        InitializeAsync().Forget();
    }

    [ObservableProperty]
    public partial string? SongName { get; set; }

    [ObservableProperty]
    public partial string? ArtistName { get; set; }

    [ObservableProperty]
    public partial string? SongAndArtistName { get; set; }

    [ObservableProperty]
    public partial ImageSource? Thumbnail { get; set; }

    [ObservableProperty]
    public partial ImageSource? ThumbnailLarge { get; set; }

    [ObservableProperty]
    public partial bool IsNextAvailable { get; set; }

    [ObservableProperty]
    public partial bool IsPreviousAvailable { get; set; }

    [ObservableProperty]
    public partial bool IsPlayPauseAvailable { get; set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    public partial bool ShowTitleArtistAndThumbnail { get; set; }

    [ObservableProperty]
    public partial bool ShouldAppearInSill { get; set; }

    internal void SwitchToPlayingSourceWindow()
    {
        lock (_lock)
        {
            if (_currentSession is not null)
            {
                WindowHelper.ActivateWindow(_currentSession.Hwnd);
            }
        }
    }

    [RelayCommand]
    private void PlayPause()
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

    [RelayCommand]
    private void Next()
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

    [RelayCommand]
    private void Previous()
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

    private void SettingsProvider_SettingChanged(ISettingsProvider sender, SettingChangedEventArgs args)
    {
        if (args.SettingName == Settings.Settings.ShowTitleArtistAndThumbnail.Name)
        {
            ShowTitleArtistAndThumbnail = _settingsProvider.GetSetting(Settings.Settings.ShowTitleArtistAndThumbnail);
        }
        else if (args.SettingName == PredefinedSettings.SillLocation.Name)
        {
            UpdateInfoAsync(_sessionManager?.CurrentSession).ForgetSafely();
        }
    }

    private void SessionManager_SessionListChanged(object? sender, NowPlayingSessionManagerEventArgs e)
    {
        UpdateInfoAsync(_sessionManager?.CurrentSession).ForgetSafely();
    }

    private void MediaPlaybackDataSource_MediaPlaybackDataChanged(object? sender, MediaPlaybackDataChangedArgs e)
    {
        UpdateInfoAsync(_sessionManager?.CurrentSession).ForgetSafely();
    }

    private async Task InitializeAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            _sessionManager = new NowPlayingSessionManager();

            if (_sessionManager is not null)
            {
                _sessionManager.SessionListChanged += SessionManager_SessionListChanged;

                await UpdateInfoAsync(_sessionManager.CurrentSession);
            }
        });
    }

    private async Task UpdateInfoAsync(NowPlayingSession? session)
    {
        await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            lock (_lock)
            {
                if (session != _currentSession)
                {
                    if (_currentSession is not null && _mediaPlaybackDataSource is not null)
                    {
                        try
                        {
                            _mediaPlaybackDataSource.MediaPlaybackDataChanged -= MediaPlaybackDataSource_MediaPlaybackDataChanged;
                        }
                        catch { }
                    }

                    _currentSession = session;

                    if (_currentSession is not null)
                    {
                        try
                        {
                            _mediaPlaybackDataSource = _currentSession.ActivateMediaPlaybackDataSource();
                            _mediaPlaybackDataSource.MediaPlaybackDataChanged += MediaPlaybackDataSource_MediaPlaybackDataChanged;
                        }
                        catch { }
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

                        SongName = mediaInfo.Title ?? string.Empty;
                        ArtistName = mediaInfo.Artist ?? string.Empty;
                        if (string.IsNullOrEmpty(mediaInfo.Artist))
                        {
                            SongAndArtistName = mediaInfo.Title ?? string.Empty;
                        }
                        else if (string.IsNullOrEmpty(mediaInfo.Title))
                        {
                            SongAndArtistName = mediaInfo.Artist ?? string.Empty;
                        }
                        else
                        {
                            SongAndArtistName = $"{mediaInfo.Artist} - {mediaInfo.Title}";
                        }

                        (ImageSource? thumbnail, ImageSource? thumbnailLarge) = await GetThumbnailImageSourceAsync(mediaPlaybackDataSource.GetThumbnailStream());
                        Thumbnail = thumbnail;
                        ThumbnailLarge = thumbnailLarge;

                        UpdatePlaybackInfo(mediaPlaybackDataSource.GetMediaPlaybackInfo());
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while trying to retrieve media information.");
                    }
                }

                // Ensure proper state reset when session is null
                UpdatePlaybackInfo(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while trying to update media information.");
            }
        });
    }

    private void UpdatePlaybackInfo(MediaPlaybackInfo? playback)
    {
        if (playback is not null)
        {
            IsNextAvailable = playback.Value.PlaybackCaps.HasFlag(MediaPlaybackCapabilities.Next);
            IsPreviousAvailable = playback.Value.PlaybackCaps.HasFlag(MediaPlaybackCapabilities.Previous);
            IsPlayPauseAvailable = playback.Value.PlaybackCaps.HasFlag(MediaPlaybackCapabilities.PlayPauseToggle);
            IsPlaying = playback.Value.PlaybackState == MediaPlaybackState.Playing;
            ShouldAppearInSill = !string.IsNullOrEmpty(SongName);
        }
        else
        {
            ShouldAppearInSill = false;
            IsNextAvailable = false;
            IsPreviousAvailable = false;
            IsPlayPauseAvailable = false;
            IsPlaying = false;
        }
    }

    private async Task<(ImageSource? thumbnail, ImageSource? thumbnailLarge)> GetThumbnailImageSourceAsync(Stream thumbnail)
    {
        try
        {
            if (thumbnail is not null)
            {
                using IRandomAccessStream thumbnailStream = thumbnail.AsRandomAccessStream();

                // Create the decoder from the stream
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(thumbnailStream);

                // Get the SoftwareBitmap representation of the file
                SoftwareBitmap thumbnailLarge = await decoder.GetSoftwareBitmapAsync();
                if (thumbnailLarge.BitmapPixelFormat != BitmapPixelFormat.Bgra8 || thumbnailLarge.BitmapAlphaMode == BitmapAlphaMode.Straight)
                {
                    thumbnailLarge = SoftwareBitmap.Convert(thumbnailLarge, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                // Scale down for the small thumbnail
                uint size
                    = _settingsProvider.GetSetting(PredefinedSettings.SillLocation)
                    is SillLocation.Left or SillLocation.Right
                    ? VerticalThumbnailSize
                    : HorizontalThumbnailSize;

                var transform = new BitmapTransform()
                {
                    ScaledWidth = size,
                    ScaledHeight = size,
                    InterpolationMode = BitmapInterpolationMode.Fant
                };
                SoftwareBitmap thumbnailSmall
                    = await decoder.GetSoftwareBitmapAsync(
                        thumbnailLarge.BitmapPixelFormat,
                        thumbnailLarge.BitmapAlphaMode,
                        transform,
                        ExifOrientationMode.IgnoreExifOrientation,
                        ColorManagementMode.ColorManageToSRgb);

                var sourceLarge = new SoftwareBitmapSource();
                await sourceLarge.SetBitmapAsync(thumbnailLarge);

                var sourceSmall = new SoftwareBitmapSource();
                await sourceSmall.SetBitmapAsync(thumbnailSmall);
                return (sourceSmall, sourceLarge);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while trying to retrieve media thumbnail.");
        }
        finally
        {
            thumbnail?.Dispose();
        }

        return (null, null);
    }
}
