using WindowSill.API;

namespace WindowSill.InlineTerminal.Settings;

internal static class Settings
{
    /// <summary>
    /// Whether to wrap the output text in the command result view.
    /// </summary>
    internal static readonly SettingDefinition<bool> WordWrapOutput
        = new(true, typeof(Settings).Assembly);

    /// <summary>
    /// Whether to disable the ClickFix security warning when running commands from a web browser.
    /// </summary>
    internal static readonly SettingDefinition<bool> DisableClickFixWarning
        = new(false, typeof(Settings).Assembly);

    /// <summary>
    /// Number of minutes after completion before a run is automatically dismissed.
    /// 0 means auto-dismiss is disabled. Valid values: 0, 5, 10, 15, 30.
    /// </summary>
    internal static readonly SettingDefinition<int> AutoDismissMinutes
        = new(5, typeof(Settings).Assembly);
}
