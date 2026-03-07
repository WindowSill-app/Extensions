using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Content view for user activity clipboard items, displayed in the list row.
/// </summary>
internal sealed partial class UserActivityItemContentView : UserControl
{
    internal UserActivityItemContentView(UserActivityItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal UserActivityItemViewModel ViewModel { get; }
}
