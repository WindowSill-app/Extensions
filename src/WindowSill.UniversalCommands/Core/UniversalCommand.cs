using Windows.System;

namespace WindowSill.UniversalCommands.Core;

internal sealed record UniversalCommand
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the display name shown in the sill bar.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of action to perform.
    /// </summary>
    public UniversalCommandType Type { get; set; }

    /// <summary>
    /// Gets or sets the keyboard chord sequence for a <see cref="UniversalCommandType.KeyboardShortcut"/> action.
    /// Each inner list is one key combination (e.g., Ctrl+K). Multiple inner lists form a chord
    /// (e.g., [Ctrl+K, Ctrl+D]). Stored as integer virtual key codes for serialization.
    /// </summary>
    public List<List<int>> KeyboardChord { get; set; } = [];

    /// <summary>
    /// Gets the keyboard chord as <see cref="VirtualKey"/> values.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<VirtualKey>> KeyboardChordKeys
        => KeyboardChord.Select(combo => (IReadOnlyList<VirtualKey>)combo.Select(k => (VirtualKey)k).ToList()).ToList();

    /// <summary>
    /// Gets or sets the PowerShell command for a <see cref="ActionType.PowerShellCommand"/> action.
    /// </summary>
    public string? PowerShellCommand { get; set; }

    /// <summary>
    /// Gets or sets the process name of the target application.
    /// When <see langword="null"/>, the action is global (shown for all apps).
    /// </summary>
    public string? TargetAppProcessName { get; set; }

    /// <summary>
    /// Gets or sets the icon glyph character displayed in the sill bar button.
    /// Used when <see cref="IconImagePath"/> is <see langword="null"/>.
    /// </summary>
    public char IconGlyph { get; set; } = '\uE768'; // Play icon default

    /// <summary>
    /// Gets or sets the path to a custom image file (PNG, SVG, JPG, WEBP) used as the icon.
    /// When set, takes priority over <see cref="IconGlyph"/>.
    /// </summary>
    public string? IconImagePath { get; set; }
}
