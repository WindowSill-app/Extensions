using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

/// <summary>
/// Hover preview flyout for a meeting sill item.
/// Shows full title, countdown, and exact date/time.
/// </summary>
internal sealed partial class MeetingPreviewFlyout : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MeetingPreviewFlyout"/> class.
    /// </summary>
    /// <param name="viewModel">The meeting view model.</param>
    internal MeetingPreviewFlyout(MeetingSillItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for x:Bind.
    /// </summary>
    internal MeetingSillItemViewModel ViewModel { get; }
}
