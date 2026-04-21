namespace WindowSill.Date.Core.Models;

/// <summary>
/// Phases of a meeting sill item's countdown lifecycle.
/// For virtual meetings: Normal → Urgent → Flashing → Live → Elapsed → Ended.
/// For physical meetings: Normal → Urgent → Departure → Traveling → Live → Elapsed → Ended.
/// </summary>
internal enum MeetingPhase
{
    /// <summary>
    /// Meeting (or departure) is ≥5 minutes away. Normal appearance.
    /// </summary>
    Normal,

    /// <summary>
    /// Meeting (or departure) is &lt;5 minutes away. Accent background, mm:ss countdown.
    /// </summary>
    Urgent,

    /// <summary>
    /// Meeting is ≤30 seconds away (virtual) or departure is imminent. Flashing sill item.
    /// </summary>
    Flashing,

    /// <summary>
    /// Departure time reached for a physical location meeting.
    /// "Leave now!" with flashing + notification. Stays until meeting starts.
    /// </summary>
    Departure,

    /// <summary>
    /// Between departure and meeting start. Calm countdown to meeting start.
    /// </summary>
    Traveling,

    /// <summary>
    /// Meeting start time reached. "Is live!" text. Stays for 1 minute.
    /// </summary>
    Live,

    /// <summary>
    /// Meeting is in progress (1+ min after start). Shows elapsed time.
    /// </summary>
    Elapsed,

    /// <summary>
    /// Meeting has ended. Item should be removed.
    /// </summary>
    Ended,
}
