using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;

using WindowSill.API;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// ViewModel for a single meeting sill item. Manages countdown text,
/// phase transitions, and meeting metadata for display.
/// Side effects (flashing, notifications) fire only on phase transition, not every tick.
/// </summary>
internal sealed partial class MeetingSillItemViewModel : ObservableObject, IDisposable
{
    /// <summary>
    /// Duration the "Live" phase lasts before transitioning to "Elapsed".
    /// </summary>
    private static readonly TimeSpan livePhaseDuration = TimeSpan.FromMinutes(1);

    private bool _disposed;
    private bool _flashFired;
    private bool _notificationFired;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeetingSillItemViewModel"/> class.
    /// </summary>
    /// <param name="calendarEvent">The calendar event this item represents.</param>
    public MeetingSillItemViewModel(CalendarEvent calendarEvent)
    {
        Event = calendarEvent;
        Key = MeetingKey.FromEvent(calendarEvent);
    }

    /// <summary>
    /// Gets the underlying calendar event.
    /// </summary>
    public CalendarEvent Event { get; }

    /// <summary>
    /// Gets the stable identity key for this meeting.
    /// </summary>
    public MeetingKey Key { get; }

    /// <summary>
    /// Gets the current phase of the meeting countdown.
    /// </summary>
    [ObservableProperty]
    public partial MeetingPhase Phase { get; private set; }

    /// <summary>
    /// Gets the meeting title text for the bar.
    /// </summary>
    public string Title => Event.Title;

    /// <summary>
    /// Gets the countdown or elapsed time text (e.g., "in 22 min", "4:32", "is live!", "• 12 min").
    /// </summary>
    [ObservableProperty]
    public partial string CountdownText { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the meeting is in an urgent phase (accent background).
    /// </summary>
    [ObservableProperty]
    public partial bool IsUrgent { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the Join button should be visible.
    /// </summary>
    [ObservableProperty]
    public partial bool IsJoinVisible { get; private set; }

    /// <summary>
    /// Gets the video call join URL, if available.
    /// </summary>
    public Uri? VideoCallUrl => Event.VideoCall?.JoinUrl;

    /// <summary>
    /// Gets a value indicating whether this event has a video call link.
    /// </summary>
    public bool HasVideoCall => Event.VideoCall is not null;

    /// <summary>
    /// Gets the video call provider name, if available.
    /// </summary>
    public string? VideoCallProviderName => Event.VideoCall?.Provider.ToString();

    /// <summary>
    /// Gets the calendar color hex string.
    /// </summary>
    public string? CalendarColor => Event.Color;

    /// <summary>
    /// Gets the event web link for "Show in Calendar".
    /// </summary>
    public Uri? WebLink => Event.WebLink;

    /// <summary>
    /// Gets the event location, if any.
    /// </summary>
    public string? Location => Event.Location;

    /// <summary>
    /// Gets a value indicating whether this event has a physical location.
    /// </summary>
    public bool HasLocation => !string.IsNullOrWhiteSpace(Event.Location);

    /// <summary>
    /// Gets or sets the cached travel time estimate for this meeting.
    /// Set asynchronously after creation.
    /// </summary>
    [ObservableProperty]
    public partial TravelTimeEstimateResult? TravelTimeEstimate { get; set; }

    /// <summary>
    /// Gets a value indicating whether a travel time estimate is available.
    /// </summary>
    public bool HasTravelTime => TravelTimeEstimate is not null && TravelTimeEstimate.IsSuccess;

    /// <summary>
    /// Gets the formatted travel time text for display in the flyout (e.g., "~25 min travel").
    /// </summary>
    public string? TravelTimeText
    {
        get
        {
            if (TravelTimeEstimate is not { IsSuccess: true, Duration: { } duration })
            {
                return null;
            }

            int minutes = (int)Math.Ceiling(duration.TotalMinutes);
            return string.Format("/WindowSill.Date/Meetings/TravelTimeMinutes".GetLocalizedString(), minutes);
        }
    }

    /// <summary>
    /// Gets the organizer display text.
    /// </summary>
    public string? OrganizerText => Event.Organizer is not null
        ? Event.Organizer.Name ?? Event.Organizer.Email
        : null;

    /// <summary>
    /// Gets the calendar ID for display in the flyout.
    /// </summary>
    public string CalendarId => Event.CalendarId;

    /// <summary>
    /// Gets a value indicating whether this event is an all-day event.
    /// </summary>
    public bool IsAllDay => Event.IsAllDay;

    /// <summary>
    /// Gets the formatted time range text for the flyout (e.g., "17:00 to 18:00").
    /// </summary>
    public string TimeRangeText
    {
        get
        {
            if (Event.IsAllDay)
            {
                return "/WindowSill.Date/Meetings/AllDay".GetLocalizedString();
            }

            string start = Event.StartTime.LocalDateTime.ToString("H:mm", CultureInfo.CurrentCulture);
            string end = Event.EndTime.LocalDateTime.ToString("H:mm", CultureInfo.CurrentCulture);
            return $"{start} – {end}";
        }
    }

    /// <summary>
    /// Gets the formatted date text for the flyout (e.g., "28 Feb 2023").
    /// </summary>
    public string DateText
        => Event.StartTime.LocalDateTime.ToString("d MMM yyyy", CultureInfo.CurrentCulture);

    /// <summary>
    /// Gets the full date + time text for the preview flyout.
    /// </summary>
    public string FullDateTimeText
    {
        get
        {
            string date = Event.StartTime.LocalDateTime.ToString("dddd, MMMM d, yyyy", CultureInfo.CurrentCulture);
            string time = Event.StartTime.LocalDateTime.ToString("h:mm tt", CultureInfo.CurrentCulture);
            return $"{date} at {time}";
        }
    }

    /// <summary>
    /// Fired once when <see cref="Phase"/> transitions to <see cref="MeetingPhase.Flashing"/> or <see cref="MeetingPhase.Live"/>.
    /// The subscriber should call <c>StartFlashing()</c> on the sill item.
    /// </summary>
    public event Action? FlashRequested;

    /// <summary>
    /// Fired once when <see cref="Phase"/> transitions to <see cref="MeetingPhase.Live"/>.
    /// The subscriber should show the full-screen notification.
    /// </summary>
    public event Action? NotificationRequested;

    /// <summary>
    /// Updates the countdown text and phase based on the current time.
    /// For physical meetings with a travel estimate, phases are relative to departure time.
    /// Side effects (flashing, notifications) fire only once per transition.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <param name="showJoinButton">Whether the join button setting is enabled.</param>
    /// <param name="departureBufferMinutes">Extra buffer minutes added before travel time for the user to get ready.</param>
    public void UpdateCountdown(DateTimeOffset now, bool showJoinButton, int departureBufferMinutes = 0)
    {
        TimeSpan timeUntilStart = Event.StartTime - now;
        TimeSpan timeUntilEnd = Event.EndTime - now;

        // Compute departure time for physical meetings with a travel estimate.
        // departure = meetingStart − travelTime − buffer
        TimeSpan? travelDuration = TravelTimeEstimate is { IsSuccess: true, Duration: { } d } ? d : null;
        DateTimeOffset? departureTime = travelDuration.HasValue
            ? Event.StartTime - travelDuration.Value - TimeSpan.FromMinutes(departureBufferMinutes)
            : null;
        TimeSpan? timeUntilDeparture = departureTime.HasValue
            ? departureTime.Value - now
            : null;

        MeetingPhase newPhase = ComputePhase(timeUntilStart, timeUntilEnd, timeUntilDeparture);
        MeetingPhase previousPhase = Phase;
        Phase = newPhase;

        // Update countdown text based on phase.
        CountdownText = newPhase switch
        {
            MeetingPhase.Normal when timeUntilDeparture.HasValue
                => FormatDepartureCountdownMinutes(timeUntilDeparture.Value),
            MeetingPhase.Normal
                => FormatCountdownMinutes(timeUntilStart),
            MeetingPhase.Urgent when timeUntilDeparture is { } ttd && ttd > TimeSpan.Zero
                => FormatDepartureCountdownSeconds(ttd),
            MeetingPhase.Urgent
                => FormatCountdownSeconds(timeUntilStart),
            MeetingPhase.Flashing when timeUntilDeparture is { } ttd && ttd > TimeSpan.Zero
                => FormatDepartureCountdownSeconds(ttd),
            MeetingPhase.Flashing
                => FormatCountdownSeconds(timeUntilStart),
            MeetingPhase.Departure
                => "/WindowSill.Date/Meetings/LeaveNow".GetLocalizedString(),
            MeetingPhase.Traveling
                => FormatCountdownMinutes(timeUntilStart),
            MeetingPhase.Live
                => "/WindowSill.Date/Meetings/IsLive".GetLocalizedString(),
            MeetingPhase.Elapsed
                => FormatElapsed(now - Event.StartTime),
            MeetingPhase.Ended
                => string.Empty,
            _ => string.Empty,
        };

        // Update visual state.
        IsUrgent = newPhase is MeetingPhase.Urgent or MeetingPhase.Flashing
            or MeetingPhase.Departure or MeetingPhase.Live;

        // Join button visible from Urgent phase onwards if video call exists and setting enabled.
        IsJoinVisible = showJoinButton
            && HasVideoCall
            && newPhase >= MeetingPhase.Urgent
            && newPhase < MeetingPhase.Ended;

        // Edge-triggered side effects: fire only on transition.
        if (newPhase != previousPhase)
        {
            if (newPhase is MeetingPhase.Flashing or MeetingPhase.Departure or MeetingPhase.Live
                && !_flashFired)
            {
                _flashFired = true;
                FlashRequested?.Invoke();
            }

            if (newPhase is MeetingPhase.Departure or MeetingPhase.Live
                && !_notificationFired)
            {
                _notificationFired = true;
                NotificationRequested?.Invoke();
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        FlashRequested = null;
        NotificationRequested = null;
    }

    /// <summary>
    /// Computes the meeting phase from timing data.
    /// For physical meetings, urgency phases are relative to departure time.
    /// Pure function — no side effects.
    /// </summary>
    /// <param name="timeUntilStart">Time until the meeting starts.</param>
    /// <param name="timeUntilEnd">Time until the meeting ends.</param>
    /// <param name="timeUntilDeparture">Time until the user should leave (null for virtual meetings).</param>
    internal static MeetingPhase ComputePhase(
        TimeSpan timeUntilStart,
        TimeSpan timeUntilEnd,
        TimeSpan? timeUntilDeparture = null)
    {
        if (timeUntilEnd <= TimeSpan.Zero)
        {
            return MeetingPhase.Ended;
        }

        if (timeUntilStart <= TimeSpan.Zero)
        {
            // Meeting has started.
            TimeSpan elapsed = -timeUntilStart;
            if (elapsed < livePhaseDuration)
            {
                return MeetingPhase.Live;
            }

            return MeetingPhase.Elapsed;
        }

        // Physical meeting with departure time.
        if (timeUntilDeparture.HasValue)
        {
            TimeSpan ttd = timeUntilDeparture.Value;

            if (ttd <= TimeSpan.Zero)
            {
                // Departure time reached — user should be traveling.
                return MeetingPhase.Departure;
            }

            // Urgency phases relative to departure, not meeting start.
            if (ttd <= TimeSpan.FromSeconds(30))
            {
                return MeetingPhase.Flashing;
            }

            if (ttd <= TimeSpan.FromMinutes(5))
            {
                return MeetingPhase.Urgent;
            }

            return MeetingPhase.Normal;
        }

        // Virtual meeting — urgency relative to meeting start.
        if (timeUntilStart <= TimeSpan.FromSeconds(30))
        {
            return MeetingPhase.Flashing;
        }

        if (timeUntilStart <= TimeSpan.FromMinutes(5))
        {
            return MeetingPhase.Urgent;
        }

        return MeetingPhase.Normal;
    }

    /// <summary>
    /// Formats countdown as "in N min" for ≥5 minutes.
    /// </summary>
    private static string FormatCountdownMinutes(TimeSpan remaining)
    {
        int totalMinutes = (int)Math.Ceiling(remaining.TotalMinutes);
        return string.Format(
            "/WindowSill.Date/Meetings/InNMinutes".GetLocalizedString(),
            totalMinutes);
    }

    /// <summary>
    /// Formats departure countdown as "leave in N min" for ≥5 minutes.
    /// </summary>
    private static string FormatDepartureCountdownMinutes(TimeSpan remaining)
    {
        int totalMinutes = (int)Math.Ceiling(remaining.TotalMinutes);
        return string.Format(
            "/WindowSill.Date/Meetings/LeaveInNMinutes".GetLocalizedString(),
            totalMinutes);
    }

    /// <summary>
    /// Formats countdown as "N:SS" for &lt;5 minutes.
    /// </summary>
    private static string FormatCountdownSeconds(TimeSpan remaining)
    {
        int minutes = (int)remaining.TotalMinutes;
        int seconds = remaining.Seconds;
        return $"{minutes}:{seconds:D2}";
    }

    /// <summary>
    /// Formats departure countdown as "leave in N:SS" for &lt;5 minutes.
    /// </summary>
    private static string FormatDepartureCountdownSeconds(TimeSpan remaining)
    {
        int minutes = (int)remaining.TotalMinutes;
        int seconds = remaining.Seconds;
        return string.Format(
            "/WindowSill.Date/Meetings/LeaveInCountdown".GetLocalizedString(),
            $"{minutes}:{seconds:D2}");
    }

    /// <summary>
    /// Formats elapsed time as "• N min" for in-progress meetings.
    /// </summary>
    private static string FormatElapsed(TimeSpan elapsed)
    {
        int totalMinutes = Math.Max(1, (int)elapsed.TotalMinutes);
        return string.Format(
            "/WindowSill.Date/Meetings/ElapsedMinutes".GetLocalizedString(),
            totalMinutes);
    }
}
