namespace WindowSill.Date.Core.Models;

/// <summary>
/// Indicates how the user's time is shown during a calendar event.
/// </summary>
public enum BusyStatus
{
    /// <summary>The user is available.</summary>
    Free,

    /// <summary>The user is busy.</summary>
    Busy,

    /// <summary>The user is out of office.</summary>
    OutOfOffice,

    /// <summary>The user is working from another location.</summary>
    WorkingElsewhere,

    /// <summary>The busy status is unknown.</summary>
    Unknown,
}
