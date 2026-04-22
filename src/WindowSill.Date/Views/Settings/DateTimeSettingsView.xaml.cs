using WindowSill.API;
using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

/// <summary>
/// Settings view for date and time display preferences.
/// </summary>
internal sealed partial class DateTimeSettingsView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeSettingsView"/> class.
    /// </summary>
    /// <param name="settingsProvider">The settings provider.</param>
    public DateTimeSettingsView(ISettingsProvider settingsProvider)
    {
        ViewModel = new DateTimeSettingsViewModel(settingsProvider);
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model.
    /// </summary>
    internal DateTimeSettingsViewModel ViewModel { get; }
}
