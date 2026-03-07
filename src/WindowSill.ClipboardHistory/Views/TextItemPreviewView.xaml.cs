using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Preview flyout view for text clipboard items.
/// </summary>
internal sealed partial class TextItemPreviewView : UserControl
{
    internal TextItemPreviewView(TextItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal TextItemViewModel ViewModel { get; }
}
