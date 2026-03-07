using System.ComponentModel.Composition;

using WindowSill.API;
using WindowSill.MediaControl.Core;
using WindowSill.MediaControl.Settings;
using WindowSill.MediaControl.ViewModels;

namespace WindowSill.MediaControl;

/// <summary>
/// Entry point for the Media Control extension, providing playback controls in the sill.
/// </summary>
[Export(typeof(ISill))]
[Name("Media Control")]
[Priority(Priority.Highest)]
[SupportMultipleMonitors()]
public sealed class MediaControlSill : ISill, ISillSingleView
{
    private readonly ISettingsProvider _settingsProvider;
    private Views.MediaControlView? _mediaControlView;

    [ImportingConstructor]
    internal MediaControlSill(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
    }

    /// <inheritdoc />
    public string DisplayName => "/WindowSill.MediaControl/Misc/DisplayName".GetLocalizedString();

    /// <inheritdoc />
    public IconElement CreateIcon() => new SymbolIcon(Symbol.Play);

    /// <inheritdoc />
    public SillSettingsView[]? SettingsViews =>
        [
        new SillSettingsView(
            DisplayName,
            new(() => new SettingsView(_settingsProvider)))
        ];

    /// <inheritdoc />
    public SillView View
    {
        get
        {
            if (_mediaControlView is null)
            {
                IMediaSessionService mediaSessionService = new MediaSessionService();
                IThumbnailService thumbnailService = new ThumbnailService();
                var viewModel = new MediaControlViewModel(mediaSessionService, thumbnailService, _settingsProvider);
                _mediaControlView = new Views.MediaControlView(viewModel);
            }

            return _mediaControlView.SillView;
        }
    }

    /// <inheritdoc />
    public ValueTask OnDeactivatedAsync()
    {
        throw new NotImplementedException();
    }
}
