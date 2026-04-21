namespace WindowSill.Date.Core.Models;

/// <summary>
/// Represents a calendar event with rich metadata suitable for a Dato-like bar display.
/// </summary>
public sealed class CalendarEvent
{
    /// <summary>
    /// Gets the provider-specific unique identifier for this event.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the identifier of the calendar this event belongs to.
    /// </summary>
    public required string CalendarId { get; init; }

    /// <summary>
    /// Gets the identifier of the account this event belongs to.
    /// </summary>
    public required string AccountId { get; init; }

    /// <summary>
    /// Gets the event title or subject.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the event description or body text.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the HTML-formatted notes for this event, if available.
    /// </summary>
    public string? HtmlDescription { get; init; }

    /// <summary>
    /// Gets the event location (physical address or room name).
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// Gets the start time of the event.
    /// For all-day events, this represents the start date with time set to midnight in the event's time zone.
    /// </summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// Gets the end time of the event.
    /// For all-day events, this represents the end date (exclusive) with time set to midnight.
    /// </summary>
    public required DateTimeOffset EndTime { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is an all-day event.
    /// </summary>
    public bool IsAllDay { get; init; }

    /// <summary>
    /// Gets the confirmation status of this event.
    /// </summary>
    public CalendarEventStatus Status { get; init; } = CalendarEventStatus.Confirmed;

    /// <summary>
    /// Gets how the user's time is displayed during this event.
    /// </summary>
    public BusyStatus BusyStatus { get; init; } = BusyStatus.Busy;

    /// <summary>
    /// Gets the current user's response status for this event.
    /// </summary>
    public AttendeeResponseStatus ResponseStatus { get; init; } = AttendeeResponseStatus.NotResponded;

    /// <summary>
    /// Gets video call information if a conference link was detected.
    /// </summary>
    public VideoCallInfo? VideoCall { get; init; }

    /// <summary>
    /// Gets a URL to open this event in the provider's web interface.
    /// </summary>
    public Uri? WebLink { get; init; }

    /// <summary>
    /// Gets the event organizer information.
    /// </summary>
    public CalendarEventAttendee? Organizer { get; init; }

    /// <summary>
    /// Gets the list of event attendees.
    /// </summary>
    public IReadOnlyList<CalendarEventAttendee> Attendees { get; init; } = [];

    /// <summary>
    /// Gets the recurrence rule in RFC 5545 RRULE format, if this is a recurring event.
    /// </summary>
    public string? RecurrenceRule { get; init; }

    /// <summary>
    /// Gets the hex color string associated with this event's calendar (e.g., "#FF5733").
    /// </summary>
    public string? Color { get; init; }

    /// <summary>
    /// Gets a value indicating whether this event is marked as private or confidential.
    /// </summary>
    public bool IsPrivate { get; init; }

    /// <summary>
    /// Gets the provider type that sourced this event.
    /// </summary>
    public required CalendarProviderType ProviderType { get; init; }

    /// <summary>
    /// Creates a copy of this event with a different color.
    /// </summary>
    /// <param name="color">The new hex color string, or <see langword="null"/> to clear.</param>
    /// <returns>A new event with the specified color.</returns>
    public CalendarEvent WithColor(string? color) => new()
    {
        Id = Id,
        CalendarId = CalendarId,
        AccountId = AccountId,
        Title = Title,
        Description = Description,
        HtmlDescription = HtmlDescription,
        Location = Location,
        StartTime = StartTime,
        EndTime = EndTime,
        IsAllDay = IsAllDay,
        Status = Status,
        BusyStatus = BusyStatus,
        ResponseStatus = ResponseStatus,
        VideoCall = VideoCall,
        WebLink = WebLink,
        Organizer = Organizer,
        Attendees = Attendees,
        RecurrenceRule = RecurrenceRule,
        Color = color,
        IsPrivate = IsPrivate,
        ProviderType = ProviderType,
    };
}
