using WindowSill.API;

namespace WindowSill.InlineTerminal.Core.UI;

/// <summary>
/// Custom SplitButton with shortcut badge support.
/// </summary>
internal sealed class ShortcutSplitButton : SplitButton, IShortcutControl
{
    internal static readonly DependencyProperty ShortcutBadgeProperty
        = DependencyProperty.Register(
            nameof(ShortcutBadge),
            typeof(ShortcutBadge),
            typeof(ShortcutSplitButton),
            new PropertyMetadata(null));

    internal ShortcutBadge? ShortcutBadge
    {
        get => GetValue(ShortcutBadgeProperty) as ShortcutBadge;
        set => SetValue(ShortcutBadgeProperty, value);
    }

    /// <inheritdoc />
    public void AssignShortcutNumber(int number)
    {
        ShortcutBadge?.Content = number.ToString();
    }

    /// <inheritdoc />
    public void InvokeShortcutAction()
    {
        if (Command is not null && Command.CanExecute(CommandParameter))
        {
            Command.Execute(CommandParameter);
        }
    }
}
