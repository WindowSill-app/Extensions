using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Preview flyout view for RTF clipboard items.
/// </summary>
internal sealed partial class RtfItemPreviewView : UserControl
{
    internal RtfItemPreviewView(RtfItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal RtfItemViewModel ViewModel { get; }
}
