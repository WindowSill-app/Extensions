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
}
