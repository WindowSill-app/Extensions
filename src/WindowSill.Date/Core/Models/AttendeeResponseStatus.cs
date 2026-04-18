namespace WindowSill.Date.Core.Models;

/// <summary>
/// Represents an attendee's response to a calendar event invitation.
/// </summary>
public enum AttendeeResponseStatus
{
    /// <summary>The attendee has not responded.</summary>
    NotResponded,

    /// <summary>The attendee accepted the invitation.</summary>
    Accepted,

    /// <summary>The attendee tentatively accepted.</summary>
    Tentative,

    /// <summary>The attendee declined the invitation.</summary>
    Declined,
}
