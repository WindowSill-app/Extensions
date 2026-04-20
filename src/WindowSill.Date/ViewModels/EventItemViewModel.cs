using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;

using WindowSill.Date.Core.Models;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// ViewModel representing a single calendar event in the popup event list.
/// </summary>
internal sealed partial class EventItemViewModel : ObservableObject
{
    private readonly CalendarEvent _event;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventItemViewModel"/> class.
    /// </summary>
    /// <param name="calendarEvent">The calendar event to display.</param>
    public EventItemViewModel(CalendarEvent calendarEvent)
    {
        _event = calendarEvent;
    }

    /// <summary>
    /// Gets the event title.
    /// </summary>
    public string Title => _event.Title;

    /// <summary>
    /// Gets the formatted time range text (e.g., "9:00 AM – 10:00 AM").
    /// Returns "All day" for all-day events.
    /// </summary>
    public string TimeRangeText
    {
        get
        {
            if (_event.IsAllDay)
            {
                return "All day";
            }

            string start = _event.StartTime.LocalDateTime.ToString("h:mm tt", CultureInfo.CurrentCulture);
            string end = _event.EndTime.LocalDateTime.ToString("h:mm tt", CultureInfo.CurrentCulture);
            return $"{start} – {end}";
        }
    }

    /// <summary>
    /// Gets the calendar color as a hex string (e.g., "#FF5733").
    /// </summary>
    public string? Color => _event.Color;

    /// <summary>
    /// Gets a value indicating whether this event has a video call link.
    /// </summary>
    public bool HasVideoCall => _event.VideoCall is not null;

    /// <summary>
    /// Gets the video call join URL, if available.
    /// </summary>
    public Uri? VideoCallUrl => _event.VideoCall?.JoinUrl;

    /// <summary>
    /// Gets the URL to open this event in the provider's web interface.
    /// </summary>
    public Uri? WebLink => _event.WebLink;

    /// <summary>
    /// Gets the event location text.
    /// </summary>
    public string? Location => _event.Location;

    /// <summary>
    /// Gets a value indicating whether this event is currently in progress.
    /// </summary>
    public bool IsNow
    {
        get
        {
            if (_event.IsAllDay)
            {
                return false;
            }

            DateTimeOffset now = DateTimeOffset.Now;
            return now >= _event.StartTime && now < _event.EndTime;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this event is in the past.
    /// </summary>
    public bool IsPast => _event.EndTime < DateTimeOffset.Now;

    /// <summary>
    /// Gets a value indicating whether this event is an all-day event.
    /// </summary>
    public bool IsAllDay => _event.IsAllDay;

    /// <summary>
    /// Gets a value indicating whether this event has been cancelled.
    /// </summary>
    public bool IsCancelled => _event.Status == CalendarEventStatus.Cancelled;

    /// <summary>
    /// Gets a value indicating whether this event was declined by the user.
    /// </summary>
    public bool IsDeclined => _event.ResponseStatus == AttendeeResponseStatus.Declined;

    /// <summary>
    /// Gets a value indicating whether this event has a web link for opening in calendar.
    /// </summary>
    public bool HasWebLink => _event.WebLink is not null;

    /// <summary>
    /// Gets the event start time for sorting purposes.
    /// </summary>
    public DateTimeOffset StartTime => _event.StartTime;
}
