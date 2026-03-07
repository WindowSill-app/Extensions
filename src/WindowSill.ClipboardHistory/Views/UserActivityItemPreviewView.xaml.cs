using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Preview view for user activity clipboard items, displayed in the detail pane.
/// </summary>
internal sealed partial class UserActivityItemPreviewView : UserControl
{
    internal UserActivityItemPreviewView(UserActivityItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal UserActivityItemViewModel ViewModel { get; }
}
