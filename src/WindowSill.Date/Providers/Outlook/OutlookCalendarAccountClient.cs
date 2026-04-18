using Microsoft.Graph;
using Microsoft.Graph.Models;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;
using CalendarEvent = WindowSill.Date.Core.Models.CalendarEvent;

namespace WindowSill.Date.Providers.Outlook;

/// <summary>
/// Per-account client for Microsoft Outlook calendar operations using Microsoft Graph.
/// </summary>
internal sealed class OutlookCalendarAccountClient : ICalendarAccountClient
{
    private readonly ICalendarCredentialStore _credentialStore;
    private GraphServiceClient? _graphClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookCalendarAccountClient"/> class.
    /// </summary>
    /// <param name="account">The account this client is scoped to.</param>
    /// <param name="credentialStore">The credential store for token access.</param>
    internal OutlookCalendarAccountClient(CalendarAccount account, ICalendarCredentialStore credentialStore)
    {
        Account = account;
        _credentialStore = credentialStore;
    }

    /// <inheritdoc />
    public CalendarAccount Account { get; }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarInfo>> GetCalendarsAsync(CancellationToken cancellationToken)
    {
        GraphServiceClient client = await GetOrCreateGraphClientAsync(cancellationToken);

        CalendarCollectionResponse? calendars = await client.Me.Calendars.GetAsync(
            cancellationToken: cancellationToken);

        if (calendars?.Value is null)
        {
            return [];
        }

        return calendars.Value
            .Select(c => new CalendarInfo
            {
                Id = c.Id ?? string.Empty,
                AccountId = Account.Id,
                Name = c.Name ?? "Unnamed",
                Color = MapCalendarColor(c.Color),
                IsDefault = c.IsDefaultCalendar ?? false,
                IsReadOnly = !(c.CanEdit ?? true),
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        GraphServiceClient client = await GetOrCreateGraphClientAsync(cancellationToken);

        // Use calendarView to get expanded recurring events.
        EventCollectionResponse? events = await client.Me.CalendarView.GetAsync(
            config =>
            {
                config.QueryParameters.StartDateTime = from.UtcDateTime.ToString("o");
                config.QueryParameters.EndDateTime = to.UtcDateTime.ToString("o");
                config.QueryParameters.Top = 250;
                config.QueryParameters.Select = [
                    "id", "subject", "body", "bodyPreview", "location", "start", "end",
                    "isAllDay", "isCancelled", "showAs", "responseStatus", "onlineMeeting",
                    "webLink", "organizer", "attendees", "recurrence", "sensitivity",
                ];
            },
            cancellationToken: cancellationToken);

        if (events?.Value is null)
        {
            return [];
        }

        return events.Value.Select(MapToCalendarEvent).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> RefreshAuthAsync(CancellationToken cancellationToken)
    {
        // TODO: Use stored refresh token to acquire new access token via MSAL.
        string? refreshToken = await _credentialStore.RetrieveAsync(Account.Id, "refresh_token", cancellationToken);
        if (string.IsNullOrEmpty(refreshToken))
        {
            return false;
        }

        // Force re-creation of the Graph client on next use.
        _graphClient = null;
        return true;
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        await _credentialStore.RemoveAsync(Account.Id, cancellationToken);
        _graphClient = null;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _graphClient = null;
        return ValueTask.CompletedTask;
    }

    private async Task<GraphServiceClient> GetOrCreateGraphClientAsync(CancellationToken cancellationToken)
    {
        if (_graphClient is not null)
        {
            return _graphClient;
        }

        string? accessToken = await _credentialStore.RetrieveAsync(Account.Id, "access_token", cancellationToken);
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new InvalidOperationException("No access token available. Call RefreshAuthAsync first.");
        }

        // TODO: Replace with proper token credential provider that handles refresh.
        _graphClient = new GraphServiceClient(
            new Microsoft.Kiota.Abstractions.Authentication.BaseBearerTokenAuthenticationProvider(
                new StaticAccessTokenProvider(accessToken)));

        return _graphClient;
    }

    private CalendarEvent MapToCalendarEvent(Event graphEvent)
    {
        Uri? videoCallUrl = null;
        VideoCallInfo? videoCall = null;

        if (graphEvent.OnlineMeeting?.JoinUrl is string joinUrl && Uri.TryCreate(joinUrl, UriKind.Absolute, out Uri? parsedJoinUrl))
        {
            videoCallUrl = parsedJoinUrl;
        }

        // Try to detect video call from body/location as well.
        videoCall = VideoCallDetector.Detect(
            graphEvent.BodyPreview,
            graphEvent.Location?.DisplayName);

        if (videoCall is null && videoCallUrl is not null)
        {
            videoCall = new VideoCallInfo(videoCallUrl, VideoCallProvider.MicrosoftTeams);
        }

        return new CalendarEvent
        {
            Id = graphEvent.Id ?? string.Empty,
            CalendarId = string.Empty, // Graph calendarView doesn't include calendar ID by default.
            AccountId = Account.Id,
            Title = graphEvent.Subject ?? "No Title",
            Description = graphEvent.BodyPreview,
            HtmlDescription = graphEvent.Body?.ContentType == BodyType.Html ? graphEvent.Body.Content : null,
            Location = graphEvent.Location?.DisplayName,
            StartTime = ParseGraphDateTime(graphEvent.Start),
            EndTime = ParseGraphDateTime(graphEvent.End),
            IsAllDay = graphEvent.IsAllDay ?? false,
            Status = graphEvent.IsCancelled == true ? CalendarEventStatus.Cancelled : CalendarEventStatus.Confirmed,
            BusyStatus = MapShowAs(graphEvent.ShowAs),
            ResponseStatus = MapResponseStatus(graphEvent.ResponseStatus?.Response),
            VideoCall = videoCall,
            WebLink = Uri.TryCreate(graphEvent.WebLink, UriKind.Absolute, out Uri? webUri) ? webUri : null,
            Organizer = graphEvent.Organizer?.EmailAddress is not null
                ? new CalendarEventAttendee(
                    graphEvent.Organizer.EmailAddress.Name,
                    graphEvent.Organizer.EmailAddress.Address ?? string.Empty,
                    AttendeeResponseStatus.Accepted,
                    IsOrganizer: true)
                : null,
            Attendees = graphEvent.Attendees?
                .Select(a => new CalendarEventAttendee(
                    a.EmailAddress?.Name,
                    a.EmailAddress?.Address ?? string.Empty,
                    MapResponseStatus(a.Status?.Response)))
                .ToList() ?? [],
            RecurrenceRule = null, // Graph returns recurrence as a structured object; RRULE conversion is complex.
            IsPrivate = graphEvent.Sensitivity == Sensitivity.Private || graphEvent.Sensitivity == Sensitivity.Confidential,
            ProviderType = CalendarProviderType.Outlook,
        };
    }

    private static DateTimeOffset ParseGraphDateTime(DateTimeTimeZone? dateTime)
    {
        if (dateTime is null || string.IsNullOrEmpty(dateTime.DateTime))
        {
            return DateTimeOffset.MinValue;
        }

        if (TimeZoneInfo.TryFindSystemTimeZoneById(dateTime.TimeZone ?? "UTC", out TimeZoneInfo? tz))
        {
            DateTime dt = DateTime.Parse(dateTime.DateTime);
            return new DateTimeOffset(dt, tz.GetUtcOffset(dt));
        }

        return DateTimeOffset.Parse(dateTime.DateTime);
    }

    private static BusyStatus MapShowAs(FreeBusyStatus? showAs)
    {
        return showAs switch
        {
            FreeBusyStatus.Free => BusyStatus.Free,
            FreeBusyStatus.Busy => BusyStatus.Busy,
            FreeBusyStatus.Oof => BusyStatus.OutOfOffice,
            FreeBusyStatus.WorkingElsewhere => BusyStatus.WorkingElsewhere,
            FreeBusyStatus.Tentative => BusyStatus.Busy,
            _ => BusyStatus.Unknown,
        };
    }

    private static AttendeeResponseStatus MapResponseStatus(ResponseType? response)
    {
        return response switch
        {
            ResponseType.Accepted => AttendeeResponseStatus.Accepted,
            ResponseType.TentativelyAccepted => AttendeeResponseStatus.Tentative,
            ResponseType.Declined => AttendeeResponseStatus.Declined,
            _ => AttendeeResponseStatus.NotResponded,
        };
    }

    private static string? MapCalendarColor(CalendarColor? color)
    {
        return color switch
        {
            CalendarColor.Auto => null,
            CalendarColor.LightBlue => "#4A90D9",
            CalendarColor.LightGreen => "#4CAF50",
            CalendarColor.LightOrange => "#FF9800",
            CalendarColor.LightGray => "#9E9E9E",
            CalendarColor.LightYellow => "#FFEB3B",
            CalendarColor.LightTeal => "#009688",
            CalendarColor.LightPink => "#E91E63",
            CalendarColor.LightBrown => "#795548",
            CalendarColor.LightRed => "#F44336",
            CalendarColor.MaxColor => null,
            _ => null,
        };
    }

    /// <summary>
    /// Simple static token provider for Graph SDK when we already have an access token.
    /// </summary>
    private sealed class StaticAccessTokenProvider(string accessToken)
        : Microsoft.Kiota.Abstractions.Authentication.IAccessTokenProvider
    {
        public Microsoft.Kiota.Abstractions.Authentication.AllowedHostsValidator AllowedHostsValidator { get; } = new();

        public Task<string> GetAuthorizationTokenAsync(
            Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(accessToken);
        }
    }
}
