using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Preview flyout view for HTML clipboard items.
/// </summary>
internal sealed partial class HtmlItemPreviewView : UserControl
{
    internal HtmlItemPreviewView(HtmlItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal HtmlItemViewModel ViewModel { get; }
}
