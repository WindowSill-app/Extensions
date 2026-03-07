using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Preview view for color clipboard items, displayed in the detail pane.
/// </summary>
internal sealed partial class ColorItemPreviewView : UserControl
{
    internal ColorItemPreviewView(ColorItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal ColorItemViewModel ViewModel { get; }
}
