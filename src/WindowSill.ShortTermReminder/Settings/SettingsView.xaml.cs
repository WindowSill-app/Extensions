using WindowSill.API;

namespace WindowSill.ShortTermReminder.Settings;

/// <summary>
/// Settings view for configuring the Short Term Reminder extension.
/// </summary>
internal sealed partial class SettingsView : UserControl
{
    public SettingsView(ISettingsProvider settingsProvider)
    {
        ViewModel = new SettingsViewModel(settingsProvider);
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for the settings view.
    /// </summary>
    internal SettingsViewModel ViewModel { get; }
}
