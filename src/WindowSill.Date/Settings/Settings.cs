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

    /// <summary>
    /// JSON-serialized list of world clock entries.
    /// </summary>
    internal static readonly SettingDefinition<string> WorldClockEntries
        = new("[]", typeof(Settings).Assembly);

    /// <summary>
    /// Whether the calendar section is expanded by default in the popup.
    /// </summary>
    internal static readonly SettingDefinition<bool> ShowCalendarInPopup
        = new(true, typeof(Settings).Assembly);

    /// <summary>
    /// Whether the world clocks section is expanded by default in the popup.
    /// </summary>
    internal static readonly SettingDefinition<bool> ShowWorldClocksInPopup
        = new(true, typeof(Settings).Assembly);

    // ──────────────────────────────────────────────
    //  Travel time settings
    // ──────────────────────────────────────────────

    /// <summary>
    /// The user's OpenRouteService API key for routing. Empty = feature disabled.
    /// </summary>
    internal static readonly SettingDefinition<string> OpenRouteServiceApiKey
        = new(string.Empty, typeof(Settings).Assembly);

    /// <summary>
    /// Fallback commute time in minutes when routing fails or is unavailable.
    /// </summary>
    internal static readonly SettingDefinition<int> FallbackCommuteMinutes
        = new(30, typeof(Settings).Assembly);

    /// <summary>
    /// Extra buffer in minutes added to the travel time to give the user time to get ready.
    /// departure time = meeting start − travel time − buffer.
    /// </summary>
    internal static readonly SettingDefinition<int> DepartureBufferMinutes
        = new(10, typeof(Settings).Assembly);

    // ──────────────────────────────────────────────
    //  Meeting sill settings
    // ──────────────────────────────────────────────

    /// <summary>
    /// How many minutes before a meeting the sill item appears.
    /// </summary>
    internal static readonly SettingDefinition<int> ReminderWindowMinutes
        = new(30, typeof(Settings).Assembly);

    /// <summary>
    /// Maximum number of meeting sill items shown simultaneously.
    /// </summary>
    internal static readonly SettingDefinition<int> MaxMeetingSills
        = new(5, typeof(Settings).Assembly);

    /// <summary>
    /// Whether to show a "Join" button in the sill when a video call link is detected.
    /// </summary>
    internal static readonly SettingDefinition<bool> ShowJoinButton
        = new(true, typeof(Settings).Assembly);

    /// <summary>
    /// Whether sill items flash when a meeting is about to start.
    /// </summary>
    internal static readonly SettingDefinition<bool> EnableSillFlashing
        = new(true, typeof(Settings).Assembly);

    /// <summary>
    /// Whether to show a full-screen notification when a meeting starts.
    /// </summary>
    internal static readonly SettingDefinition<bool> EnableFullScreenNotification
        = new(true, typeof(Settings).Assembly);

    /// <summary>
    /// Whether to show a toast notification when a meeting starts.
    /// </summary>
    internal static readonly SettingDefinition<bool> EnableToastNotification
        = new(false, typeof(Settings).Assembly);

    /// <summary>
    /// Whether to show all-day events as meeting sill items.
    /// </summary>
    internal static readonly SettingDefinition<bool> ShowAllDayMeetings
        = new(false, typeof(Settings).Assembly);

    /// <summary>
    /// How frequently (in seconds) to poll for upcoming meetings.
    /// </summary>
    internal static readonly SettingDefinition<int> MeetingPollIntervalSeconds
        = new(60, typeof(Settings).Assembly);
}
