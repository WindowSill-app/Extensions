namespace WindowSill.Date.Core.Models;

/// <summary>
/// Represents the status of a calendar event.
/// </summary>
public enum CalendarEventStatus
{
    /// <summary>The event is confirmed.</summary>
    Confirmed,

    /// <summary>The event is tentatively scheduled.</summary>
    Tentative,

    /// <summary>The event has been cancelled.</summary>
    Cancelled,
}
