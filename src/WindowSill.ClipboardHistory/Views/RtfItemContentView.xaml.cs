using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Content view for RTF clipboard items, displayed in the list row.
/// </summary>
internal sealed partial class RtfItemContentView : UserControl
{
    internal RtfItemContentView(RtfItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal RtfItemViewModel ViewModel { get; }
}
