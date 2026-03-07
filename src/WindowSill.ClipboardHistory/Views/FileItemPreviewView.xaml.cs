using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Preview view for file clipboard items, displayed in the detail pane.
/// </summary>
internal sealed partial class FileItemPreviewView : UserControl
{
    internal FileItemPreviewView(FileItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal FileItemViewModel ViewModel { get; }
}
