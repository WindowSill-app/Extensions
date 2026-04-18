using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;
using CalendarEvent = WindowSill.Date.Core.Models.CalendarEvent;

namespace WindowSill.Date.Providers.Google;

/// <summary>
/// Per-account client for Google Calendar operations.
/// </summary>
internal sealed class GoogleCalendarAccountClient : ICalendarAccountClient
{
    private readonly IReadOnlyDictionary<string, string> _authData;
    private readonly Func<IReadOnlyDictionary<string, string>, CancellationToken, Task> _onAuthDataChanged;
    private CalendarService? _calendarService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCalendarAccountClient"/> class.
    /// </summary>
    /// <param name="account">The account this client is scoped to.</param>
    /// <param name="authData">The persisted auth data (access_token, refresh_token).</param>
    /// <param name="onAuthDataChanged">Callback to persist updated auth data.</param>
    internal GoogleCalendarAccountClient(
        CalendarAccount account,
        IReadOnlyDictionary<string, string> authData,
        Func<IReadOnlyDictionary<string, string>, CancellationToken, Task> onAuthDataChanged)
    {
        Account = account;
        _authData = authData;
        _onAuthDataChanged = onAuthDataChanged;
    }

    /// <inheritdoc />
    public CalendarAccount Account { get; }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarInfo>> GetCalendarsAsync(CancellationToken cancellationToken)
    {
        CalendarService service = await GetOrCreateServiceAsync(cancellationToken);

        CalendarList? calendarList = await service.CalendarList.List().ExecuteAsync(cancellationToken);

        if (calendarList?.Items is null)
        {
            return [];
        }

        return calendarList.Items
            .Select(c => new CalendarInfo
            {
                Id = c.Id ?? string.Empty,
                AccountId = Account.Id,
                Name = c.Summary ?? "Unnamed",
                Color = c.BackgroundColor,
                IsDefault = c.Primary ?? false,
                IsReadOnly = c.AccessRole is "reader" or "freeBusyReader",
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        CalendarService service = await GetOrCreateServiceAsync(cancellationToken);

        // Fetch calendars first, then events from each.
        IReadOnlyList<CalendarInfo> calendars = await GetCalendarsAsync(cancellationToken);

        var allEvents = new List<CalendarEvent>();

        foreach (CalendarInfo calendar in calendars)
        {
            EventsResource.ListRequest request = service.Events.List(calendar.Id);
            request.TimeMinDateTimeOffset = from;
            request.TimeMaxDateTimeOffset = to;
            request.SingleEvents = true; // Expand recurring events.
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
            request.MaxResults = 250;

            Events? events = await request.ExecuteAsync(cancellationToken);

            if (events?.Items is not null)
            {
                allEvents.AddRange(events.Items.Select(e => MapToCalendarEvent(e, calendar)));
            }
        }

        return allEvents;
    }

    /// <inheritdoc />
    public async Task<bool> RefreshAuthAsync(CancellationToken cancellationToken)
    {
        string? refreshToken = _authData.GetValueOrDefault("refresh_token");
        if (string.IsNullOrEmpty(refreshToken))
        {
            return false;
        }

        // TODO: Use refresh token to get new access token from Google token endpoint.
        // Call _onAuthDataChanged with updated tokens.
        _calendarService = null;
        return true;
    }

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _calendarService = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _calendarService?.Dispose();
        _calendarService = null;
        return ValueTask.CompletedTask;
    }

    private async Task<CalendarService> GetOrCreateServiceAsync(CancellationToken cancellationToken)
    {
        if (_calendarService is not null)
        {
            return _calendarService;
        }

        string? accessToken = _authData.GetValueOrDefault("access_token");
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new InvalidOperationException("No access token available. Call RefreshAuthAsync first.");
        }

        // TODO: Use proper credential with auto-refresh.
        _calendarService = new CalendarService(new BaseClientService.Initializer
        {
            ApplicationName = "WindowSill.Date",
        });

        return _calendarService;
    }

    private CalendarEvent MapToCalendarEvent(Event googleEvent, CalendarInfo calendar)
    {
        bool isAllDay = googleEvent.Start?.DateTimeDateTimeOffset is null && !string.IsNullOrEmpty(googleEvent.Start?.Date);

        DateTimeOffset startTime = isAllDay
            ? DateTimeOffset.Parse(googleEvent.Start!.Date!)
            : googleEvent.Start?.DateTimeDateTimeOffset ?? DateTimeOffset.MinValue;

        DateTimeOffset endTime = isAllDay
            ? DateTimeOffset.Parse(googleEvent.End!.Date!)
            : googleEvent.End?.DateTimeDateTimeOffset ?? DateTimeOffset.MinValue;

        VideoCallInfo? videoCall = null;
        if (googleEvent.ConferenceData?.EntryPoints is not null)
        {
            EntryPoint? videoEntry = googleEvent.ConferenceData.EntryPoints
                .FirstOrDefault(ep => ep.EntryPointType == "video");
            if (videoEntry?.Uri is not null)
            {
                videoCall = VideoCallDetector.Detect(videoEntry.Uri, null)
                    ?? new VideoCallInfo(new Uri(videoEntry.Uri), VideoCallProvider.Other);
            }
        }

        videoCall ??= VideoCallDetector.Detect(googleEvent.Description, googleEvent.Location);

        return new CalendarEvent
        {
            Id = googleEvent.Id ?? string.Empty,
            CalendarId = calendar.Id,
            AccountId = Account.Id,
            Title = googleEvent.Summary ?? "No Title",
            Description = googleEvent.Description,
            Location = googleEvent.Location,
            StartTime = startTime,
            EndTime = endTime,
            IsAllDay = isAllDay,
            Status = MapEventStatus(googleEvent.Status),
            BusyStatus = googleEvent.Transparency == "transparent" ? BusyStatus.Free : BusyStatus.Busy,
            ResponseStatus = MapAttendeeResponse(googleEvent.Attendees
                ?.FirstOrDefault(a => a.Self == true)?.ResponseStatus),
            VideoCall = videoCall,
            WebLink = Uri.TryCreate(googleEvent.HtmlLink, UriKind.Absolute, out Uri? link) ? link : null,
            Organizer = googleEvent.Organizer is not null
                ? new CalendarEventAttendee(
                    googleEvent.Organizer.DisplayName,
                    googleEvent.Organizer.Email ?? string.Empty,
                    AttendeeResponseStatus.Accepted,
                    IsOrganizer: true)
                : null,
            Attendees = googleEvent.Attendees?
                .Select(a => new CalendarEventAttendee(
                    a.DisplayName,
                    a.Email ?? string.Empty,
                    MapAttendeeResponse(a.ResponseStatus)))
                .ToList() ?? [],
            RecurrenceRule = googleEvent.Recurrence?.FirstOrDefault(r => r.StartsWith("RRULE:", StringComparison.OrdinalIgnoreCase)),
            Color = calendar.Color,
            IsPrivate = googleEvent.Visibility == "private" || googleEvent.Visibility == "confidential",
            ProviderType = CalendarProviderType.Google,
        };
    }

    private static CalendarEventStatus MapEventStatus(string? status)
    {
        return status switch
        {
            "confirmed" => CalendarEventStatus.Confirmed,
            "tentative" => CalendarEventStatus.Tentative,
            "cancelled" => CalendarEventStatus.Cancelled,
            _ => CalendarEventStatus.Confirmed,
        };
    }

    private static AttendeeResponseStatus MapAttendeeResponse(string? response)
    {
        return response switch
        {
            "accepted" => AttendeeResponseStatus.Accepted,
            "declined" => AttendeeResponseStatus.Declined,
            "tentative" => AttendeeResponseStatus.Tentative,
            "needsAction" => AttendeeResponseStatus.NotResponded,
            _ => AttendeeResponseStatus.NotResponded,
        };
    }
}
