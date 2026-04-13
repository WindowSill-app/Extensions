using WindowSill.InlineTerminal.ViewModels;

namespace WindowSill.InlineTerminal.Core.UI;

/// <summary>
/// Custom ListView for command shortcuts with keyboard support.
/// </summary>
public sealed class ShortcutListView : ListView
{
    public ShortcutListView()
    {
        DefaultStyleKey = typeof(ShortcutListView);
    }

    /// <summary>
    /// Raised when an item is invoked via shortcut.
    /// </summary>
    public event EventHandler<ActiveRunItem>? ItemInvoked;

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride()
    {
        return new ShortcutListViewItem(this);
    }

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        return item is ShortcutListViewItem;
    }

    internal void OnItemInvoked(ActiveRunItem item)
    {
        ItemInvoked?.Invoke(this, item);
    }
}
