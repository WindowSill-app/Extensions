using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using NPSMLib;

using WindowSill.API;
using WindowSill.MediaControl.Core;

namespace WindowSill.MediaControl.ViewModels;

/// <summary>
/// View model for the media control view, exposing playback state and commands.
/// </summary>
internal sealed partial class MediaControlViewModel : ObservableObject
{
    private readonly IMediaSessionService _mediaSessionService;
    private readonly IThumbnailService _thumbnailService;
    private readonly ISettingsProvider _settingsProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaControlViewModel"/> class.
    /// </summary>
    /// <param name="mediaSessionService">The media session management service.</param>
    /// <param name="thumbnailService">The thumbnail processing service.</param>
    /// <param name="settingsProvider">The settings provider for user preferences.</param>
    public MediaControlViewModel(
        IMediaSessionService mediaSessionService,
        IThumbnailService thumbnailService,
        ISettingsProvider settingsProvider)
    {
        _mediaSessionService = mediaSessionService;
        _thumbnailService = thumbnailService;
        _settingsProvider = settingsProvider;

        ShowTitleArtistAndThumbnail = settingsProvider.GetSetting(Settings.Settings.ShowTitleArtistAndThumbnail);
        settingsProvider.SettingChanged += OnSettingChanged;
        mediaSessionService.MediaInfoChanged += OnMediaInfoChanged;

        mediaSessionService.InitializeAsync().Forget();
    }

    /// <summary>
    /// Gets or sets the song name.
    /// </summary>
    [ObservableProperty]
    public partial string? SongName { get; set; }

    /// <summary>
    /// Gets or sets the artist name.
    /// </summary>
    [ObservableProperty]
    public partial string? ArtistName { get; set; }

    /// <summary>
    /// Gets or sets the combined song and artist name for compact display.
    /// </summary>
    [ObservableProperty]
    public partial string? SongAndArtistName { get; set; }

    /// <summary>
    /// Gets or sets the small thumbnail image source.
    /// </summary>
    [ObservableProperty]
    public partial ImageSource? Thumbnail { get; set; }

    /// <summary>
    /// Gets or sets the large thumbnail image source used in the preview flyout.
    /// </summary>
    [ObservableProperty]
    public partial ImageSource? ThumbnailLarge { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the next track command is available.
    /// </summary>
    [ObservableProperty]
    public partial bool IsNextAvailable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the previous track command is available.
    /// </summary>
    [ObservableProperty]
    public partial bool IsPreviousAvailable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the play/pause command is available.
    /// </summary>
    [ObservableProperty]
    public partial bool IsPlayPauseAvailable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether media is currently playing.
    /// </summary>
    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the title, artist, and thumbnail should be shown.
    /// </summary>
    [ObservableProperty]
    public partial bool ShowTitleArtistAndThumbnail { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the extension should appear in the sill.
    /// </summary>
    [ObservableProperty]
    public partial bool ShouldAppearInSill { get; set; }

    /// <summary>
    /// Gets a value indicating whether the artist name is present.
    /// </summary>
    public bool HasArtistName => !string.IsNullOrEmpty(ArtistName);

    /// <summary>
    /// Activates the window of the application that is currently playing media.
    /// </summary>
    [RelayCommand]
    internal void SwitchToPlayingSourceWindow()
    {
        if (_mediaSessionService.CurrentSessionWindowHandle is nint handle)
        {
            WindowHelper.ActivateWindow(handle);
        }
    }

    /// <summary>
    /// Sends a play/pause toggle command.
    /// </summary>
    [RelayCommand]
    private void PlayPause() => _mediaSessionService.PlayPause();

    /// <summary>
    /// Sends a next track command.
    /// </summary>
    [RelayCommand]
    private void Next() => _mediaSessionService.Next();

    /// <summary>
    /// Sends a previous track command.
    /// </summary>
    [RelayCommand]
    private void Previous() => _mediaSessionService.Previous();

    private async void OnMediaInfoChanged(object? sender, MediaInfoChangedEventArgs e)
    {
        SongName = e.SongTitle ?? string.Empty;
        ArtistName = e.ArtistName ?? string.Empty;
        OnPropertyChanged(nameof(HasArtistName));

        if (string.IsNullOrEmpty(e.ArtistName))
        {
            SongAndArtistName = e.SongTitle ?? string.Empty;
        }
        else if (string.IsNullOrEmpty(e.SongTitle))
        {
            SongAndArtistName = e.ArtistName ?? string.Empty;
        }
        else
        {
            SongAndArtistName = $"{e.ArtistName} - {e.SongTitle}";
        }

        SillLocation sillLocation = _settingsProvider.GetSetting(PredefinedSettings.SillLocation);
        (Thumbnail, ThumbnailLarge) = await _thumbnailService.CreateThumbnailsAsync(
            e.ThumbnailStream,
            sillLocation);

        UpdatePlaybackInfo(e.PlaybackInfo);
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

    private void OnSettingChanged(ISettingsProvider sender, SettingChangedEventArgs args)
    {
        if (args.SettingName == Settings.Settings.ShowTitleArtistAndThumbnail.Name)
        {
            ShowTitleArtistAndThumbnail = _settingsProvider.GetSetting(Settings.Settings.ShowTitleArtistAndThumbnail);
        }
        else if (args.SettingName == PredefinedSettings.SillLocation.Name)
        {
            _mediaSessionService.RequestUpdate();
        }
    }
}
