using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Content view for unknown clipboard items, displayed in the list row.
/// </summary>
internal sealed partial class UnknownItemContentView : UserControl
{
    internal UnknownItemContentView(UnknownItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal UnknownItemViewModel ViewModel { get; }
}
