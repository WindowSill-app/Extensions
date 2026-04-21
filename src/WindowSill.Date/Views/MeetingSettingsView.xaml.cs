using WindowSill.API;
using WindowSill.Date.Core.Services;
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
    public MeetingSettingsView(ISettingsProvider settingsProvider, MeetingStateService? meetingStateService = null)
    {
        ViewModel = new MeetingSettingsViewModel(settingsProvider, meetingStateService);
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model.
    /// </summary>
    internal MeetingSettingsViewModel ViewModel { get; }

    private void SyncNowCard_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SyncNowCommand.Execute(null);
    }
}
