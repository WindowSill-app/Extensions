namespace WindowSill.Date.Settings;

/// <summary>
/// Controls where upcoming meeting sill items appear relative to the main date sill
/// and pinned world clocks.
/// </summary>
internal enum MeetingPlacement
{
    /// <summary>
    /// Meeting sills appear before all other sills (leftmost / topmost).
    /// </summary>
    BeforeAll,

    /// <summary>
    /// Meeting sills appear after all other sills (rightmost / bottommost).
    /// </summary>
    AfterAll,
}
