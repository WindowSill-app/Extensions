using WindowSill.API;

namespace WindowSill.ClipboardHistory.Settings;

internal static class Settings
{
    /// <summary>
    /// The maximum amount of items
    /// </summary>
    internal static readonly SettingDefinition<int> MaximumHistoryCount
        = new(25, typeof(Settings).Assembly);

    /// <summary>
    /// Whether passwords should be hidden
    /// </summary>
    internal static readonly SettingDefinition<bool> HidePasswords
        = new(true, typeof(Settings).Assembly);

    /// <summary>
    /// Whether to use compact mode, displaying all clipboard items in a single popup
    /// </summary>
    internal static readonly SettingDefinition<bool> CompactMode
        = new(false, typeof(Settings).Assembly);
}
