namespace WindowSill.Date.Core.Models;

/// <summary>
/// A stable identity key for a meeting sill item.
/// Uses the provider's event ID + account ID + start time for strong uniqueness,
/// even across recurring events and cross-calendar duplicates.
/// </summary>
internal sealed record MeetingKey(string EventId, string AccountId, DateTimeOffset StartTime)
{
    /// <summary>
    /// Creates a <see cref="MeetingKey"/> from a <see cref="CalendarEvent"/>.
    /// </summary>
    /// <param name="calendarEvent">The calendar event.</param>
    /// <returns>A meeting key for identity comparison.</returns>
    public static MeetingKey FromEvent(CalendarEvent calendarEvent)
        => new(calendarEvent.Id, calendarEvent.AccountId, calendarEvent.StartTime);
}
