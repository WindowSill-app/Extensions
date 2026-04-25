namespace WindowSill.Date.Settings;

/// <summary>
/// Available time display formats for the sill bar.
/// </summary>
public enum TimeFormat
{
    /// <summary>
    /// Do not display the time.
    /// </summary>
    None,

    /// <summary>
    /// 12-hour format with AM/PM. Example: "11:28 AM".
    /// </summary>
    TwelveHour,

    /// <summary>
    /// 24-hour format. Example: "23:28".
    /// </summary>
    TwentyFourHour,
}
