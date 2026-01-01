using CommunityToolkit.Mvvm.ComponentModel;

using WindowSill.API;

namespace WindowSill.ShortTermReminder.Settings;

internal sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsProvider _settingsProvider;

    public SettingsViewModel(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
    }

    public bool UseFullScreenNotification
    {
        get => _settingsProvider.GetSetting(Settings.UseFullScreenNotification);
        set => _settingsProvider.SetSetting(Settings.UseFullScreenNotification, value);
    }
}
