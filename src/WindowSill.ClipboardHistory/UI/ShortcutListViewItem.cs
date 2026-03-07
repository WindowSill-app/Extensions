using CommunityToolkit.Diagnostics;
using WindowSill.API;
using WindowSill.ClipboardHistory.ViewModels;

namespace WindowSill.ClipboardHistory.UI;

[TemplatePart(Name = PART_ShortcutBadge, Type = typeof(ShortcutListViewItem))]
public sealed class ShortcutListViewItem : ListViewItem, IShortcutControl
{
    private const string PART_ShortcutBadge = "PART_ShortcutBadge";

    private readonly ShortcutListView _shortcutListView;
    private ShortcutBadge? _shortcutBadge;

    public ShortcutListViewItem(ShortcutListView shortcutListView)
    {
        DefaultStyleKey = typeof(ShortcutListViewItem);
        _shortcutListView = shortcutListView;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _shortcutBadge = (ShortcutBadge)GetTemplateChild(PART_ShortcutBadge);
    }

    public void AssignShortcutNumber(int number)
    {
        Guard.IsNotNull(_shortcutBadge);
        _shortcutBadge.Content = number.ToString();
    }

    public void InvokeShortcutAction()
    {
        _shortcutListView.OnItemInvoked((ClipboardHistoryItemViewModelBase)DataContext);
    }
}
