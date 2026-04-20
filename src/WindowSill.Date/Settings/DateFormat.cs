namespace WindowSill.Date.Settings;

/// <summary>
/// Available date display formats for the sill bar.
/// </summary>
public enum DateFormat
{
    /// <summary>
    /// Do not display a date.
    /// </summary>
    None,

    /// <summary>
    /// Abbreviated day of week + short month + day. Example: "Sat, Apr 19".
    /// </summary>
    AbbreviatedDayMonth,

    /// <summary>
    /// Short month + day. Example: "Apr 19".
    /// </summary>
    ShortMonthDay,

    /// <summary>
    /// Day + short month (international). Example: "19 Apr".
    /// </summary>
    DayShortMonth,

    /// <summary>
    /// Full day of week + full month + day. Example: "Saturday, April 19".
    /// </summary>
    FullDayMonth,

    /// <summary>
    /// Compact US style without year. Example: "4/19".
    /// </summary>
    MonthSlashDayCompact,

    /// <summary>
    /// Padded US style without year. Example: "04/19".
    /// </summary>
    MonthSlashDay,

    /// <summary>
    /// Compact European style without year. Example: "19/4".
    /// </summary>
    DaySlashMonthCompact,

    /// <summary>
    /// Padded European style without year. Example: "19/04".
    /// </summary>
    DaySlashMonth,

    /// <summary>
    /// US style with year. Example: "04/19/2026".
    /// </summary>
    MonthDayYear,

    /// <summary>
    /// European style with year. Example: "19/04/2026".
    /// </summary>
    DayMonthYear,

    /// <summary>
    /// ISO 8601 format. Example: "2026-04-19".
    /// </summary>
    Iso8601,
}
