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

    /// <summary>
    /// Gets or sets the auto-dismiss interval in minutes. 0 means disabled.
    /// </summary>
    public int AutoDismissMinutes
    {
        get => _settingsProvider.GetSetting(Settings.AutoDismissMinutes);
        set => _settingsProvider.SetSetting(Settings.AutoDismissMinutes, value);
    }

    /// <summary>
    /// Gets the available auto-dismiss interval options.
    /// </summary>
    public IReadOnlyList<AutoDismissOption> AutoDismissOptions { get; } =
    [
        new(0, "/WindowSill.InlineTerminal/SettingsUserControl/AutoDismissOff".GetLocalizedString()),
        new(1, string.Format("/WindowSill.InlineTerminal/SettingsUserControl/AutoDismissMinutesFormat".GetLocalizedString(), 1)),
        new(2, string.Format("/WindowSill.InlineTerminal/SettingsUserControl/AutoDismissMinutesFormat".GetLocalizedString(), 2)),
        new(5, string.Format("/WindowSill.InlineTerminal/SettingsUserControl/AutoDismissMinutesFormat".GetLocalizedString(), 5)),
        new(10, string.Format("/WindowSill.InlineTerminal/SettingsUserControl/AutoDismissMinutesFormat".GetLocalizedString(), 10)),
        new(15, string.Format("/WindowSill.InlineTerminal/SettingsUserControl/AutoDismissMinutesFormat".GetLocalizedString(), 15)),
        new(30, string.Format("/WindowSill.InlineTerminal/SettingsUserControl/AutoDismissMinutesFormat".GetLocalizedString(), 30)),
    ];

    /// <summary>
    /// Gets or sets the selected auto-dismiss option.
    /// </summary>
    public AutoDismissOption SelectedAutoDismissOption
    {
        get => AutoDismissOptions.FirstOrDefault(o => o.Minutes == AutoDismissMinutes) ?? AutoDismissOptions[0];
        set
        {
            if (value is not null && value.Minutes != AutoDismissMinutes)
            {
                AutoDismissMinutes = value.Minutes;
                OnPropertyChanged();
            }
        }
    }
}

/// <summary>
/// Represents an auto-dismiss interval option for display in a ComboBox.
/// </summary>
/// <param name="Minutes">The interval in minutes (0 = disabled).</param>
/// <param name="DisplayText">The localized display text.</param>
internal sealed record AutoDismissOption(int Minutes, string DisplayText)
{
    /// <inheritdoc />
    public override string ToString() => DisplayText;
}
