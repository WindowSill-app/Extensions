using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using WindowSill.Date.Core.Models;
using CalendarEvent = WindowSill.Date.Core.Models.CalendarEvent;

namespace WindowSill.Date.Core.Providers.Google;

/// <summary>
/// Per-account client for Google Calendar operations.
/// Uses <see cref="UserCredential"/> for automatic token refresh.
/// </summary>
internal sealed class GoogleCalendarAccountClient : ICalendarAccountClient
{
    private readonly IReadOnlyDictionary<string, string> _authData;
    private readonly Func<IReadOnlyDictionary<string, string>, CancellationToken, Task> _onAuthDataChanged;
    private CalendarService? _calendarService;
    private UserCredential? _credential;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCalendarAccountClient"/> class.
    /// </summary>
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
        try
        {
            UserCredential credential = CreateCredential();
            bool refreshed = await credential.RefreshTokenAsync(cancellationToken);

            if (refreshed)
            {
                await PersistTokensAsync(credential.Token, cancellationToken);
                _credential = credential;
                _calendarService = null;
            }

            return refreshed;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _calendarService?.Dispose();
        _calendarService = null;
        _credential = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _calendarService?.Dispose();
        _calendarService = null;
        _credential = null;
        return ValueTask.CompletedTask;
    }

    private Task<CalendarService> GetOrCreateServiceAsync(CancellationToken cancellationToken)
    {
        if (_calendarService is not null)
        {
            return Task.FromResult(_calendarService);
        }

        _credential ??= CreateCredential();

        _calendarService = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = _credential,
            ApplicationName = "WindowSill.Date",
        });

        return Task.FromResult(_calendarService);
    }

    private UserCredential CreateCredential()
    {
        GoogleAuthorizationCodeFlow flow = GoogleCalendarProvider.CreateFlow();

        var token = new TokenResponse
        {
            AccessToken = _authData.GetValueOrDefault("access_token"),
            RefreshToken = _authData.GetValueOrDefault("refresh_token"),
        };

        if (_authData.TryGetValue("issued_utc", out string? issuedStr)
            && DateTime.TryParse(issuedStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime issued))
        {
            token.IssuedUtc = issued;
        }

        if (_authData.TryGetValue("token_expiry", out string? expiryStr)
            && long.TryParse(expiryStr, out long expirySeconds))
        {
            token.ExpiresInSeconds = expirySeconds;
        }

        return new UserCredential(flow, Account.Email, token);
    }

    private async Task PersistTokensAsync(TokenResponse token, CancellationToken cancellationToken)
    {
        var updatedAuthData = new Dictionary<string, string>
        {
            ["access_token"] = token.AccessToken ?? string.Empty,
            ["refresh_token"] = token.RefreshToken ?? _authData.GetValueOrDefault("refresh_token") ?? string.Empty,
            ["token_expiry"] = token.ExpiresInSeconds?.ToString() ?? "3600",
            ["issued_utc"] = token.IssuedUtc.ToString("O"),
        };

        await _onAuthDataChanged(updatedAuthData, cancellationToken);
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
            ResponseStatus = ResolveResponseStatus(googleEvent),
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

    private static AttendeeResponseStatus ResolveResponseStatus(Event googleEvent)
    {
        // Check if we're in the attendee list.
        EventAttendee? self = googleEvent.Attendees?.FirstOrDefault(a => a.Self == true);
        if (self is not null)
        {
            return MapAttendeeResponse(self.ResponseStatus);
        }

        // Organizer not in attendees, or it's a personal event with no attendees.
        if (googleEvent.Organizer?.Self == true || googleEvent.Attendees is null or { Count: 0 })
        {
            return AttendeeResponseStatus.Accepted;
        }

        return AttendeeResponseStatus.NotResponded;
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
