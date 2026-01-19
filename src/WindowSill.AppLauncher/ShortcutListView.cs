using WindowSill.AppLauncher.Core.AppInfo;

namespace WindowSill.AppLauncher;

public sealed class ShortcutListView : ListView
{
    public ShortcutListView()
    {
        DefaultStyleKey = typeof(ShortcutListView);
    }

    internal event EventHandler<AppInfo>? ItemInvoked;

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new ShortcutListViewItem(this);
    }

    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        return item is ShortcutListViewItem;
    }

    internal void OnItemInvoked(AppInfo item)
    {
        ItemInvoked?.Invoke(this, item);
    }
}
