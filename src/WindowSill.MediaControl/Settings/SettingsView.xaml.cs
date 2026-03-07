using WindowSill.API;

namespace WindowSill.MediaControl.Settings;

/// <summary>
/// Settings view for the Media Control extension.
/// </summary>
internal sealed partial class SettingsView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsView"/> class.
    /// </summary>
    /// <param name="settingsProvider">The settings provider for reading and writing settings.</param>
    public SettingsView(ISettingsProvider settingsProvider)
    {
        ViewModel = new SettingsViewModel(settingsProvider);
        InitializeComponent();
    }
    /// <summary>
    /// Gets the view model.
    /// </summary>
    public SettingsViewModel ViewModel { get; }
}
