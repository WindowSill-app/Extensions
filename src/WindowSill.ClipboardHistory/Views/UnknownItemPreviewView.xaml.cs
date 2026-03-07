using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Preview flyout view for unknown clipboard items.
/// </summary>
internal sealed partial class UnknownItemPreviewView : UserControl
{
    internal UnknownItemPreviewView(UnknownItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal UnknownItemViewModel ViewModel { get; }
}
