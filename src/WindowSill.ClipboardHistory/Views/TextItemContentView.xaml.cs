using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Content view for text clipboard items, displayed in the list row.
/// </summary>
internal sealed partial class TextItemContentView : UserControl
{
    internal TextItemContentView(TextItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal TextItemViewModel ViewModel { get; }
}
