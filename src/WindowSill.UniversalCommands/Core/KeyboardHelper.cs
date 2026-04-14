using Windows.System;
using WindowSill.API;

namespace WindowSill.UniversalCommands.Core;

internal static class KeyboardHelper
{
    /// <summary>
    /// Gets a user-friendly display name for a <see cref="VirtualKey"/>.
    /// </summary>
    /// <param name="key">The virtual key.</param>
    /// <returns>A human-readable key name.</returns>
    internal static string GetDisplayName(VirtualKey key) => key switch
    {
        VirtualKey.Control or VirtualKey.LeftControl or VirtualKey.RightControl => Localize("KeyCtrl"),
        VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu => Localize("KeyAlt"),
        VirtualKey.Shift or VirtualKey.LeftShift or VirtualKey.RightShift => Localize("KeyShift"),
        VirtualKey.LeftWindows or VirtualKey.RightWindows => Localize("KeyWin"),
        VirtualKey.Back => Localize("KeyBackspace"),
        VirtualKey.Delete => Localize("KeyDel"),
        VirtualKey.Escape => Localize("KeyEsc"),
        VirtualKey.Insert => Localize("KeyIns"),
        VirtualKey.Space => Localize("KeySpace"),
        VirtualKey.Tab => Localize("KeyTab"),
        VirtualKey.Enter => Localize("KeyEnter"),
        VirtualKey.Left => Localize("KeyLeft"),
        VirtualKey.Right => Localize("KeyRight"),
        VirtualKey.Up => Localize("KeyUp"),
        VirtualKey.Down => Localize("KeyDown"),
        VirtualKey.PageUp => Localize("KeyPgUp"),
        VirtualKey.PageDown => Localize("KeyPgDn"),
        VirtualKey.Home => Localize("KeyHome"),
        VirtualKey.End => Localize("KeyEnd"),
        VirtualKey.Snapshot => Localize("KeyPrtSc"),
        VirtualKey.Scroll => Localize("KeyScrLk"),
        VirtualKey.Pause => Localize("KeyPause"),
        VirtualKey.CapitalLock => Localize("KeyCapsLock"),
        VirtualKey.NumberKeyLock => Localize("KeyNumLock"),
        VirtualKey.Number0 => "0",
        VirtualKey.Number1 => "1",
        VirtualKey.Number2 => "2",
        VirtualKey.Number3 => "3",
        VirtualKey.Number4 => "4",
        VirtualKey.Number5 => "5",
        VirtualKey.Number6 => "6",
        VirtualKey.Number7 => "7",
        VirtualKey.Number8 => "8",
        VirtualKey.Number9 => "9",
        VirtualKey.NumberPad0 => Localize("KeyNum0"),
        VirtualKey.NumberPad1 => Localize("KeyNum1"),
        VirtualKey.NumberPad2 => Localize("KeyNum2"),
        VirtualKey.NumberPad3 => Localize("KeyNum3"),
        VirtualKey.NumberPad4 => Localize("KeyNum4"),
        VirtualKey.NumberPad5 => Localize("KeyNum5"),
        VirtualKey.NumberPad6 => Localize("KeyNum6"),
        VirtualKey.NumberPad7 => Localize("KeyNum7"),
        VirtualKey.NumberPad8 => Localize("KeyNum8"),
        VirtualKey.NumberPad9 => Localize("KeyNum9"),
        VirtualKey.Multiply => Localize("KeyNumMultiply"),
        VirtualKey.Add => Localize("KeyNumAdd"),
        VirtualKey.Subtract => Localize("KeyNumSubtract"),
        VirtualKey.Decimal => Localize("KeyNumDecimal"),
        VirtualKey.Divide => Localize("KeyNumDivide"),
        _ => key.ToString()
    };

    private static string Localize(string resourceName)
        => $"/WindowSill.UniversalCommands/ShortcutRecorderControl/{resourceName}".GetLocalizedString();

    /// <summary>
    /// Formats a list of virtual keys as a shortcut string (e.g., "Ctrl + Shift + P").
    /// </summary>
    /// <param name="keys">The list of virtual keys.</param>
    /// <returns>A formatted shortcut string.</returns>
    internal static string FormatShortcut(IReadOnlyList<VirtualKey> keys)
    {
        return string.Join(" + ", keys.Select(GetDisplayName));
    }

    /// <summary>
    /// Formats a chord sequence as a display string (e.g., "Ctrl + K, Ctrl + D").
    /// </summary>
    /// <param name="chord">The chord — a list of key combinations.</param>
    /// <returns>A formatted chord string with combos separated by commas.</returns>
    internal static string FormatChord(IReadOnlyList<IReadOnlyList<VirtualKey>> chord)
    {
        return string.Join(",  ", chord.Select(FormatShortcut));
    }

    /// <summary>
    /// Determines whether a key is a modifier key.
    /// </summary>
    /// <param name="key">The virtual key to check.</param>
    /// <returns><see langword="true"/> if the key is a modifier; otherwise, <see langword="false"/>.</returns>
    internal static bool IsModifierKey(VirtualKey key) => key is
        VirtualKey.Control or VirtualKey.LeftControl or VirtualKey.RightControl or
        VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu or
        VirtualKey.Shift or VirtualKey.LeftShift or VirtualKey.RightShift or
        VirtualKey.LeftWindows or VirtualKey.RightWindows;

    /// <summary>
    /// Normalizes left/right modifier variants to generic modifier keys for consistent storage.
    /// </summary>
    /// <param name="key">The virtual key to normalize.</param>
    /// <returns>The normalized virtual key.</returns>
    internal static VirtualKey NormalizeModifier(VirtualKey key) => key switch
    {
        VirtualKey.LeftControl or VirtualKey.RightControl => VirtualKey.Control,
        VirtualKey.LeftMenu or VirtualKey.RightMenu => VirtualKey.Menu,
        VirtualKey.LeftShift or VirtualKey.RightShift => VirtualKey.Shift,
        _ => key
    };

    internal static async Task ExecuteChordAsync(
        IReadOnlyList<IReadOnlyList<VirtualKey>> chord,
        IProcessInteractionService processInteractionService,
        WindowInfo? windowInfo)
    {
        for (int i = 0; i < chord.Count; i++)
        {
            VirtualKey[] keys = chord[i].ToArray();

            if (windowInfo is not null)
            {
                await processInteractionService.SimulateKeysOnWindow(windowInfo, keys);
            }
            else
            {
                await processInteractionService.SimulateKeysOnLastActiveWindow(keys);
            }

            // Small delay between chord steps.
            if (i < chord.Count - 1)
            {
                await Task.Delay(100);
            }
        }
    }
}
