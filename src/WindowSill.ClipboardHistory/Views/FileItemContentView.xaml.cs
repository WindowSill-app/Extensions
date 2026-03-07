using WindowSill.ClipboardHistory.ViewModels;
using WindowSill.API;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Content view for file clipboard items, displayed in the list row.
/// </summary>
internal sealed partial class FileItemContentView : UserControl
{
    internal FileItemContentView(FileItemViewModel viewModel)
    {
        ViewModel = viewModel;
        OpenInFileExplorerTooltip = "/WindowSill.ClipboardHistory/Misc/OpenInFileExplorer".GetLocalizedString();
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal FileItemViewModel ViewModel { get; }

    /// <summary>
    /// Gets the localized tooltip for the open in file explorer button.
    /// </summary>
    internal string OpenInFileExplorerTooltip { get; }
}
