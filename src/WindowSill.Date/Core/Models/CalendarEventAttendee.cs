namespace WindowSill.Date.Core.Models;

/// <summary>
/// Represents an attendee of a calendar event.
/// </summary>
/// <param name="Name">The display name of the attendee, if available.</param>
/// <param name="Email">The email address of the attendee.</param>
/// <param name="ResponseStatus">The attendee's response to the event invitation.</param>
/// <param name="IsOrganizer">Whether this attendee is the event organizer.</param>
public sealed record CalendarEventAttendee(
    string? Name,
    string Email,
    AttendeeResponseStatus ResponseStatus,
    bool IsOrganizer = false);
