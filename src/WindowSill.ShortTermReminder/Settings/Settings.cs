using WindowSill.API;

namespace WindowSill.ShortTermReminder.Settings;

internal static class Settings
{
    /// <summary>
    /// The list of reminders to be saved so we reload them when the app restarts.
    /// </summary>
    internal static readonly SettingDefinition<Reminder[]> Reminders
        = new(Array.Empty<Reminder>(), typeof(Settings).Assembly);

    /// <summary>
    /// Whether to use full-screen notifications or toast-notifications.
    /// </summary>
    internal static readonly SettingDefinition<bool> UseFullScreenNotification
        = new(true, typeof(Settings).Assembly);
}
