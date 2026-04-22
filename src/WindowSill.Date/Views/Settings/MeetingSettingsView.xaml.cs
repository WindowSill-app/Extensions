using WindowSill.API;
using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

/// <summary>
/// Settings view for meeting sill options.
/// </summary>
internal sealed partial class MeetingSettingsView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MeetingSettingsView"/> class.
    /// </summary>
    /// <param name="settingsProvider">The settings provider.</param>
    /// <param name="meetingService">The meeting countdown service for on-demand sync.</param>
    public MeetingSettingsView(ISettingsProvider settingsProvider)
    {
        ViewModel = new MeetingSettingsViewModel(settingsProvider);
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model.
    /// </summary>
    internal MeetingSettingsViewModel ViewModel { get; }
}
