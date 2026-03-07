using WindowSill.ShortTermReminder.ViewModels;

namespace WindowSill.ShortTermReminder.Views;

/// <summary>
/// UserControl providing the preview flyout content for a reminder list item,
/// displaying the reminder title and remaining time.
/// </summary>
internal sealed partial class ReminderPreviewFlyoutContent : UserControl
{
    internal ReminderPreviewFlyoutContent(ReminderListItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this flyout content.
    /// </summary>
    internal ReminderListItemViewModel ViewModel { get; }
}
