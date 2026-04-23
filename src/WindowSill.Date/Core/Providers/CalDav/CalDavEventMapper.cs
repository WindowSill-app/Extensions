using WindowSill.Date.Core.Models;
using ICalCalendarEvent = Ical.Net.CalendarComponents.CalendarEvent;

namespace WindowSill.Date.Core.Providers.CalDav;

/// <summary>
/// Maps iCalendar VEVENT objects to <see cref="CalendarEvent"/> instances.
/// Shared between CalDAV and iCloud providers.
/// </summary>
internal static class CalDavEventMapper
{
    /// <summary>
    /// Maps an iCalendar VEVENT to a <see cref="CalendarEvent"/>.
    /// </summary>
    /// <param name="vEvent">The parsed iCal event.</param>
    /// <param name="calendar">The calendar this event belongs to.</param>
    /// <param name="providerType">The provider type. Defaults to <see cref="CalendarProviderType.CalDav"/>.</param>
    /// <param name="accountEmail">The current user's email for resolving response status.</param>
    /// <returns>The mapped calendar event.</returns>
    public static CalendarEvent MapVEvent(
        ICalCalendarEvent vEvent,
        CalendarInfo calendar,
        CalendarProviderType providerType = CalendarProviderType.CalDav,
        string? accountEmail = null)
    {
        bool isAllDay = !vEvent.Start.HasTime;

        DateTimeOffset startTime = isAllDay
            ? new DateTimeOffset(vEvent.Start.Date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            : ToDateTimeOffset(vEvent.Start);

        DateTimeOffset endTime = vEvent.End is not null
            ? (isAllDay
                ? new DateTimeOffset(vEvent.End.Date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
                : ToDateTimeOffset(vEvent.End))
            : startTime;

        string? description = vEvent.Description;
        string? location = vEvent.Location;

        VideoCallInfo? videoCall = VideoCallDetector.Detect(description, location);

        // Map each attendee with their individual response status.
        var attendees = vEvent.Attendees?
            .Select(a => new CalendarEventAttendee(
                a.CommonName,
                ExtractEmail(a.Value),
                MapPartStat(a.ParticipationStatus)))
            .ToList() ?? [];

        return new CalendarEvent
        {
            Id = vEvent.Uid ?? Guid.NewGuid().ToString(),
            CalendarId = calendar.Id,
            AccountId = calendar.AccountId,
            Title = vEvent.Summary ?? "No Title",
            Description = description,
            Location = location,
            StartTime = startTime,
            EndTime = endTime,
            IsAllDay = isAllDay,
            Status = MapICalStatus(vEvent.Status),
            BusyStatus = string.Equals(vEvent.Transparency, "TRANSPARENT", StringComparison.OrdinalIgnoreCase)
                ? BusyStatus.Free
                : BusyStatus.Busy,
            ResponseStatus = ResolveUserResponseStatus(vEvent, attendees, accountEmail),
            VideoCall = videoCall,
            Organizer = vEvent.Organizer is not null
                ? new CalendarEventAttendee(
                    vEvent.Organizer.CommonName,
                    ExtractEmail(vEvent.Organizer.Value),
                    AttendeeResponseStatus.Accepted,
                    IsOrganizer: true)
                : null,
            Attendees = attendees,
            RecurrenceRule = vEvent.RecurrenceRules?.FirstOrDefault()?.ToString(),
            Color = calendar.Color,
            IsPrivate = string.Equals(vEvent.Class, "PRIVATE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(vEvent.Class, "CONFIDENTIAL", StringComparison.OrdinalIgnoreCase),
            ProviderType = providerType,
        };
    }

    /// <summary>
    /// Resolves the current user's response status from the event's attendee list.
    /// </summary>
    private static AttendeeResponseStatus ResolveUserResponseStatus(
        ICalCalendarEvent vEvent,
        List<CalendarEventAttendee> attendees,
        string? accountEmail)
    {
        if (string.IsNullOrEmpty(accountEmail))
        {
            return AttendeeResponseStatus.NotResponded;
        }

        // Check if the user is the organizer.
        string? organizerEmail = ExtractEmail(vEvent.Organizer?.Value);
        if (organizerEmail is not null
            && organizerEmail.Equals(accountEmail, StringComparison.OrdinalIgnoreCase))
        {
            return AttendeeResponseStatus.Accepted;
        }

        // Find the user in the attendee list.
        CalendarEventAttendee? self = attendees
            .FirstOrDefault(a => a.Email.Equals(accountEmail, StringComparison.OrdinalIgnoreCase));

        if (self is not null)
        {
            return self.ResponseStatus;
        }

        // User not in attendee list and not organizer — likely a personal/own event.
        return AttendeeResponseStatus.Accepted;
    }

    /// <summary>
    /// Extracts an email address from a <c>mailto:</c> URI (or returns the Authority for other schemes).
    /// </summary>
    internal static string ExtractEmail(Uri? uri)
    {
        if (uri is null)
        {
            return string.Empty;
        }

        string raw = uri.OriginalString;

        // mailto:bob@example.com → bob@example.com
        if (raw.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return raw[7..];
        }

        // Fallback for other schemes.
        return uri.Authority ?? uri.AbsolutePath ?? string.Empty;
    }

    /// <summary>
    /// Maps an iCalendar PARTSTAT value to <see cref="AttendeeResponseStatus"/>.
    /// </summary>
    internal static AttendeeResponseStatus MapPartStat(string? partStat)
    {
        return partStat?.ToUpperInvariant() switch
        {
            "ACCEPTED" => AttendeeResponseStatus.Accepted,
            "TENTATIVE" => AttendeeResponseStatus.Tentative,
            "DECLINED" => AttendeeResponseStatus.Declined,
            "NEEDS-ACTION" => AttendeeResponseStatus.NotResponded,
            _ => AttendeeResponseStatus.NotResponded,
        };
    }

    /// <summary>
    /// Converts an iCal <see cref="Ical.Net.DataTypes.CalDateTime"/> to a <see cref="DateTimeOffset"/>,
    /// properly handling timezone-aware and floating times.
    /// </summary>
    private static DateTimeOffset ToDateTimeOffset(Ical.Net.DataTypes.CalDateTime calDateTime)
    {
        if (calDateTime.IsFloating)
        {
            // Floating = no timezone specified; treat as local time.
            return new DateTimeOffset(calDateTime.Value, TimeZoneInfo.Local.GetUtcOffset(calDateTime.Value));
        }

        // Has an explicit timezone (or UTC) — use AsUtc which resolves via NodaTime.
        return new DateTimeOffset(calDateTime.AsUtc, TimeSpan.Zero);
    }

    private static CalendarEventStatus MapICalStatus(string? status)
    {
        return status?.ToUpperInvariant() switch
        {
            "CONFIRMED" => CalendarEventStatus.Confirmed,
            "TENTATIVE" => CalendarEventStatus.Tentative,
            "CANCELLED" => CalendarEventStatus.Cancelled,
            _ => CalendarEventStatus.Confirmed,
        };
    }
}
