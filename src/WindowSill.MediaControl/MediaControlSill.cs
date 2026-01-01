using System.ComponentModel.Composition;

using WindowSill.API;
using WindowSill.MediaControl.Settings;

namespace WindowSill.MediaControl;

[Export(typeof(ISill))]
[Name("Media Control")]
[Priority(Priority.Highest)]
public sealed class MediaControlSill : ISill, ISillSingleView
{
    private readonly ISettingsProvider _settingsProvider;
    private MediaControlView? _mediaControlView;

    [ImportingConstructor]
    internal MediaControlSill(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
    }

    public string DisplayName => "/WindowSill.MediaControl/Misc/DisplayName".GetLocalizedString();

    public IconElement CreateIcon() => new SymbolIcon(Symbol.Play);

    public SillSettingsView[]? SettingsViews =>
        [
        new SillSettingsView(
            DisplayName,
            new(() => new SettingsView(_settingsProvider)))
        ];

    public SillView View
    {
        get
        {
            _mediaControlView ??= new MediaControlView(_settingsProvider);
            return _mediaControlView.View;
        }
    }

    public ValueTask OnDeactivatedAsync()
    {
        throw new NotImplementedException();
    }
}
