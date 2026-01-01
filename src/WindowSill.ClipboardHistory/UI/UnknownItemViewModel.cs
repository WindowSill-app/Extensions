using Windows.ApplicationModel.DataTransfer;
using WindowSill.API;

namespace WindowSill.ClipboardHistory.UI;

internal sealed class UnknownItemViewModel : ClipboardHistoryItemViewModelBase
{
    private readonly SillListViewButtonItem _view;

    private UnknownItemViewModel(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
        : base(processInteractionService, item)
    {
        _view = new SillListViewButtonItem(base.PasteCommand);
        _view.DataContext = this;

        _view.Content = "/WindowSill.ClipboardHistory/Misc/UnsupportedFormat".GetLocalizedString();
        _view.PreviewFlyoutContent = string.Join(", ", item.Content.AvailableFormats);
    }

    internal static (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateView(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
    {
        var viewModel = new UnknownItemViewModel(processInteractionService, item);
        return (viewModel, viewModel._view);
    }
}
