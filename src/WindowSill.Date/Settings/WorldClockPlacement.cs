namespace WindowSill.Date.Settings;

/// <summary>
/// Controls where pinned world clocks appear relative to the main date sill.
/// </summary>
internal enum WorldClockPlacement
{
    /// <summary>
    /// All pinned clocks appear before the main date sill.
    /// </summary>
    BeforeDateSill,

    /// <summary>
    /// All pinned clocks appear after the main date sill.
    /// </summary>
    AfterDateSill,

    /// <summary>
    /// Clocks in earlier timezones appear before the date sill;
    /// clocks in later timezones appear after.
    /// </summary>
    ByTimezone,
}
