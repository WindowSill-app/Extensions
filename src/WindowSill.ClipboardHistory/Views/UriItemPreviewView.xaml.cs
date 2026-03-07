using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Preview view for URI clipboard items, displayed in the detail pane.
/// </summary>
internal sealed partial class UriItemPreviewView : UserControl
{
    internal UriItemPreviewView(UriItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal UriItemViewModel ViewModel { get; }
}
