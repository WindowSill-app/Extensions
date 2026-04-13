using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;

namespace WindowSill.InlineTerminal.Settings;

internal sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsProvider _settingsProvider;

    public SettingsViewModel(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
    }

    /// <summary>
    /// Gets or sets whether the output text wraps in the command result view.
    /// </summary>
    public bool WordWrapOutput
    {
        get => _settingsProvider.GetSetting(Settings.WordWrapOutput);
        set => _settingsProvider.SetSetting(Settings.WordWrapOutput, value);
    }

    /// <summary>
    /// Gets or sets whether the ClickFix security warning is disabled.
    /// </summary>
    public bool DisableClickFixWarning
    {
        get => _settingsProvider.GetSetting(Settings.DisableClickFixWarning);
        set => _settingsProvider.SetSetting(Settings.DisableClickFixWarning, value);
    }
}
