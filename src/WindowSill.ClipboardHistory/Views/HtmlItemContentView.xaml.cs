using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Content view for HTML clipboard items, displayed in the list row.
/// </summary>
internal sealed partial class HtmlItemContentView : UserControl
{
    internal HtmlItemContentView(HtmlItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal HtmlItemViewModel ViewModel { get; }
}
