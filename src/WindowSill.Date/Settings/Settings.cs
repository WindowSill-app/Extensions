using WindowSill.API;

namespace WindowSill.Date.Settings;

/// <summary>
/// Setting definitions for the Date extension display options.
/// </summary>
internal static class Settings
{
    /// <summary>
    /// The sill bar display mode (Icon or DateTime).
    /// </summary>
    internal static readonly SettingDefinition<SillDisplayMode> DisplayMode
        = new(SillDisplayMode.Icon, typeof(Settings).Assembly);

    /// <summary>
    /// The date format to display in the sill bar.
    /// </summary>
    internal static readonly SettingDefinition<DateFormat> DateFormat
        = new(global::WindowSill.Date.Settings.DateFormat.AbbreviatedDayMonth, typeof(Settings).Assembly);

    /// <summary>
    /// The time format to display in the sill bar.
    /// </summary>
    internal static readonly SettingDefinition<TimeFormat> TimeFormat
        = new(global::WindowSill.Date.Settings.TimeFormat.TwelveHour, typeof(Settings).Assembly);

    /// <summary>
    /// Whether to show seconds in the time display.
    /// </summary>
    internal static readonly SettingDefinition<bool> ShowSeconds
        = new(false, typeof(Settings).Assembly);
}
