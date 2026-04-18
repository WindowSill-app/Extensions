using WindowSill.API;
using WindowSill.Date.Core;
using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

internal sealed partial class SettingsView : UserControl
{
    public SettingsView(ISettingsProvider settingsProvider, CalendarAccountManager calendarAccountManager)
    {
        ViewModel = new SettingsViewModel(settingsProvider, calendarAccountManager);
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for the settings view.
    /// </summary>
    internal SettingsViewModel ViewModel { get; }
}
