using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Content view for color clipboard items, displayed in the list row.
/// </summary>
internal sealed partial class ColorItemContentView : UserControl
{
    internal ColorItemContentView(ColorItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal ColorItemViewModel ViewModel { get; }
}
