using Windows.System;
using WindowSill.API;

namespace WindowSill.ClipboardHistory.Settings;

/// <summary>
/// Settings view for the Clipboard History extension.
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

    private async void OpenWindowsClipboardHistorySettingsCard_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("ms-settings:clipboard"));
    }
}
