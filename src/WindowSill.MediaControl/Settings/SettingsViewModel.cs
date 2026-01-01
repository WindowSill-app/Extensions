using CommunityToolkit.Mvvm.ComponentModel;

using WindowSill.API;

namespace WindowSill.MediaControl.Settings;

internal sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsProvider _settingsProvider;

    public SettingsViewModel(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
    }

    public bool ShowTitleArtistAndThumbnail
    {
        get => _settingsProvider.GetSetting(Settings.ShowTitleArtistAndThumbnail);
        set => _settingsProvider.SetSetting(Settings.ShowTitleArtistAndThumbnail, value);
    }
}
