using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Content view for image clipboard items, displayed in the list row.
/// </summary>
internal sealed partial class ImageItemContentView : UserControl
{
    internal ImageItemContentView(ImageItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal ImageItemViewModel ViewModel { get; }
}
