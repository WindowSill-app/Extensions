using CommunityToolkit.Diagnostics;
using WindowSill.API;
using WindowSill.InlineTerminal.ViewModels;

namespace WindowSill.InlineTerminal.Core.UI;

/// <summary>
/// Custom ListViewItem with shortcut badge support.
/// </summary>
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

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _shortcutBadge = (ShortcutBadge)GetTemplateChild(PART_ShortcutBadge);
    }

    /// <inheritdoc />
    public void AssignShortcutNumber(int number)
    {
        Guard.IsNotNull(_shortcutBadge);
        _shortcutBadge.Content = number.ToString();
    }

    /// <inheritdoc />
    public void InvokeShortcutAction()
    {
        _shortcutListView.OnItemInvoked((ActiveRunItem)DataContext);
    }
}
