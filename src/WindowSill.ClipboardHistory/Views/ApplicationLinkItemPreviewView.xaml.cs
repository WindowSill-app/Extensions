using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Preview view for application link clipboard items, displayed in the detail pane.
/// </summary>
internal sealed partial class ApplicationLinkItemPreviewView : UserControl
{
    internal ApplicationLinkItemPreviewView(ApplicationLinkItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal ApplicationLinkItemViewModel ViewModel { get; }
}
