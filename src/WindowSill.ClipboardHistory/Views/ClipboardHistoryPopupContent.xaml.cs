using Windows.ApplicationModel;
using WindowSill.API;
using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Popup content view for the compact mode clipboard history.
/// Displays all clipboard history items in a scrollable list within a single popup.
/// </summary>
internal sealed partial class ClipboardHistoryPopupContent : SillPopupContent
{
    internal ClipboardHistoryPopupContent(ClipboardHistoryPopupViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the ViewModel for the popup content.
    /// </summary>
    internal ClipboardHistoryPopupViewModel ViewModel { get; }

    internal string PopupTitle { get; } = "/WindowSill.ClipboardHistory/Misc/DisplayName".GetLocalizedString();

    internal string ClearHistoryTooltip { get; } = "/WindowSill.ClipboardHistory/Misc/ClearHistory".GetLocalizedString();

    private void ClipboardItemsShortcutListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ClipboardHistoryItemViewModelBase viewModel)
        {
            Close();
            viewModel.PasteCommand.Execute(null);
        }
    }

    private void ClipboardItemsShortcutListView_ItemInvoked(object sender, ClipboardHistoryItemViewModelBase e)
    {
        Close();
        e.PasteCommand.Execute(null);
    }
}
