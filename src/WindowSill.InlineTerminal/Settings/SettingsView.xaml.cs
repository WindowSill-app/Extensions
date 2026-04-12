using WindowSill.API;

namespace WindowSill.InlineTerminal.Settings;

/// <summary>
/// Settings view for the Inline Terminal extension.
/// </summary>
internal sealed partial class SettingsView : UserControl
{
    internal SettingsView(ISettingsProvider settingsProvider)
    {
        ViewModel = new SettingsViewModel(settingsProvider);
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for the settings view.
    /// </summary>
    internal SettingsViewModel ViewModel { get; }
}
