using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.UI;

public sealed class ShortcutListView : ListView
{
    public ShortcutListView()
    {
        DefaultStyleKey = typeof(ShortcutListView);
    }

    public event EventHandler<ClipboardHistoryItemViewModelBase>? ItemInvoked;

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new ShortcutListViewItem(this);
    }

    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        return item is ShortcutListViewItem;
    }

    internal void OnItemInvoked(ClipboardHistoryItemViewModelBase item)
    {
        ItemInvoked?.Invoke(this, item);
    }
}
