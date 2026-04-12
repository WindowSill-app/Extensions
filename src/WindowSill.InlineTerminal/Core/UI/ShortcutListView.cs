using WindowSill.InlineTerminal.Core.Commands;

namespace WindowSill.InlineTerminal.Core.UI;

public sealed class ShortcutListView : ListView
{
    public ShortcutListView()
    {
        DefaultStyleKey = typeof(ShortcutListView);
    }

    public event EventHandler<CommandRunnerHandle>? ItemInvoked;

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new ShortcutListViewItem(this);
    }

    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        return item is ShortcutListViewItem;
    }

    internal void OnItemInvoked(CommandRunnerHandle item)
    {
        ItemInvoked?.Invoke(this, item);
    }
}
