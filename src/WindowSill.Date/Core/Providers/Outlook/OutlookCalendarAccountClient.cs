using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using WindowSill.Date.Core.Models;
using CalendarEvent = WindowSill.Date.Core.Models.CalendarEvent;

namespace WindowSill.Date.Core.Providers.Outlook;

/// <summary>
/// Per-account client for Microsoft Outlook calendar operations using Microsoft Graph.
/// Uses MSAL for token management with automatic silent refresh.
/// </summary>
internal sealed class OutlookCalendarAccountClient : ICalendarAccountClient
{
    private readonly IPublicClientApplication _msalClient;
    private readonly SemaphoreSlim _requestGate = new(1, 1);
    private GraphServiceClient? _graphClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookCalendarAccountClient"/> class.
    /// </summary>
    /// <param name="account">The account this client is scoped to.</param>
    /// <param name="msalClient">The MSAL client for token management.</param>
    internal OutlookCalendarAccountClient(CalendarAccount account, IPublicClientApplication msalClient)
    {
        Account = account;
        _msalClient = msalClient;
    }

    /// <inheritdoc />
    public CalendarAccount Account { get; }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarInfo>> GetCalendarsAsync(CancellationToken cancellationToken)
    {
        GraphServiceClient client = GetOrCreateGraphClient();

        await _requestGate.WaitAsync(cancellationToken);
        try
        {
            return await WithRetryAsync(
                ct => FetchCalendarsAsync(client, ct),
                cancellationToken);
        }
        catch (Exception)
        {
            return [];
        }
        finally
        {
            _requestGate.Release();
        }
    }

    private async Task<IReadOnlyList<CalendarInfo>> FetchCalendarsAsync(
        GraphServiceClient client, CancellationToken cancellationToken)
    {
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
                Color = c.HexColor ?? MapCalendarColor(c.Color),
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
        GraphServiceClient client = GetOrCreateGraphClient();

        await _requestGate.WaitAsync(cancellationToken);
        try
        {
            return await WithRetryAsync(async ct =>
            {
                // Fetch calendars first, then query each one individually
                // so every event has a valid CalendarId.
                IReadOnlyList<CalendarInfo> calendars = await FetchCalendarsAsync(client, ct);
                var allEvents = new List<CalendarEvent>();

                foreach (CalendarInfo calendar in calendars)
                {
                    EventCollectionResponse? events = await client.Me.Calendars[calendar.Id].CalendarView.GetAsync(
                        config =>
                        {
                            config.QueryParameters.StartDateTime = from.UtcDateTime.ToString("o");
                            config.QueryParameters.EndDateTime = to.UtcDateTime.ToString("o");
                            config.QueryParameters.Top = 250;
                            config.QueryParameters.Orderby = ["start/dateTime"];
                            config.QueryParameters.Select = [
                                "id", "subject", "body", "bodyPreview", "location", "start", "end",
                                "isAllDay", "isCancelled", "showAs", "responseStatus", "onlineMeeting",
                                "onlineMeetingUrl", "webLink", "organizer", "attendees", "recurrence", "sensitivity",
                            ];
                        },
                        cancellationToken: ct);

                    if (events?.Value is not null)
                    {
                        allEvents.AddRange(events.Value.Select(e => MapToCalendarEvent(e, calendar)));
                    }
                }

                return (IReadOnlyList<CalendarEvent>)allEvents;
            }, cancellationToken);
        }
        catch (Exception)
        {
            return [];
        }
        finally
        {
            _requestGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> RefreshAuthAsync(CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<IAccount> accounts = await _msalClient.GetAccountsAsync();
            IAccount? account = accounts.FirstOrDefault(a =>
                string.Equals(a.Username, Account.Email, StringComparison.OrdinalIgnoreCase));

            if (account is null)
            {
                return false;
            }

            await _msalClient.AcquireTokenSilent(OutlookCalendarProvider.Scopes, account)
                .ExecuteAsync(cancellationToken);

            // Force re-creation of the Graph client to pick up the new token.
            _graphClient = null;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        IEnumerable<IAccount> accounts = await _msalClient.GetAccountsAsync();
        IAccount? account = accounts.FirstOrDefault(a =>
            string.Equals(a.Username, Account.Email, StringComparison.OrdinalIgnoreCase));

        if (account is not null)
        {
            await _msalClient.RemoveAsync(account);
        }

        _graphClient = null;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _graphClient = null;
        _requestGate.Dispose();
        return ValueTask.CompletedTask;
    }

    private GraphServiceClient GetOrCreateGraphClient()
    {
        if (_graphClient is not null)
        {
            return _graphClient;
        }

        var authProvider = new BaseBearerTokenAuthenticationProvider(
            new MsalTokenProvider(_msalClient, Account.Email));
        _graphClient = new GraphServiceClient(new HttpClient(), authProvider);

        return _graphClient;
    }

    /// <summary>
    /// Retries an operation up to 3 times with exponential backoff when throttled.
    /// </summary>
    private static async Task<T> WithRetryAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        const int MaxRetries = 3;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (Microsoft.Graph.Models.ODataErrors.ODataError ex) when (
                attempt < MaxRetries
                && ex.ResponseStatusCode == 429)
            {
                int delaySeconds = (int)Math.Pow(2, attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
            catch (HttpRequestException ex) when (
                attempt < MaxRetries
                && ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                int delaySeconds = (int)Math.Pow(2, attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
        }

        return await operation(cancellationToken);
    }

    private CalendarEvent MapToCalendarEvent(Event graphEvent, CalendarInfo calendar)
    {
        VideoCallInfo? videoCall = null;

        // Check structured online meeting data first.
        Uri? onlineMeetingUrl = null;
        if (graphEvent.OnlineMeeting?.JoinUrl is string joinUrl
            && Uri.TryCreate(joinUrl, UriKind.Absolute, out Uri? parsedJoinUrl))
        {
            onlineMeetingUrl = parsedJoinUrl;
        }
        else if (!string.IsNullOrEmpty(graphEvent.OnlineMeetingUrl)
            && Uri.TryCreate(graphEvent.OnlineMeetingUrl, UriKind.Absolute, out Uri? parsedMeetingUrl))
        {
            onlineMeetingUrl = parsedMeetingUrl;
        }

        // Try to detect video call provider from body/location.
        videoCall = VideoCallDetector.Detect(
            graphEvent.BodyPreview,
            graphEvent.Location?.DisplayName);

        // Fall back to the structured meeting URL if no provider was detected from text.
        if (videoCall is null && onlineMeetingUrl is not null)
        {
            videoCall = VideoCallDetector.Detect(onlineMeetingUrl.ToString(), null)
                ?? new VideoCallInfo(onlineMeetingUrl, VideoCallProvider.MicrosoftTeams);
        }

        return new CalendarEvent
        {
            Id = graphEvent.Id ?? string.Empty,
            CalendarId = calendar.Id,
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
            RecurrenceRule = null,
            Color = calendar.Color,
            IsPrivate = graphEvent.Sensitivity == Sensitivity.Private || graphEvent.Sensitivity == Sensitivity.Confidential,
            ProviderType = CalendarProviderType.Outlook,
        };
    }

    /// <summary>
    /// Parses a Microsoft Graph <see cref="DateTimeTimeZone"/> into a <see cref="DateTimeOffset"/>.
    /// </summary>
    internal static DateTimeOffset ParseGraphDateTime(DateTimeTimeZone? dateTime)
    {
        if (dateTime is null || string.IsNullOrEmpty(dateTime.DateTime))
        {
            return DateTimeOffset.MinValue;
        }

        if (DateTime.TryParse(dateTime.DateTime, out DateTime dt))
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(dateTime.TimeZone ?? "UTC");
                return new DateTimeOffset(dt, tz.GetUtcOffset(dt));
            }
            catch
            {
                return new DateTimeOffset(dt, TimeSpan.Zero);
            }
        }

        return DateTimeOffset.MinValue;
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
            ResponseType.Accepted or ResponseType.Organizer => AttendeeResponseStatus.Accepted,
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
    /// MSAL-backed token provider that silently refreshes tokens for a specific account.
    /// </summary>
    private sealed class MsalTokenProvider : IAccessTokenProvider
    {
        private readonly IPublicClientApplication _msalClient;
        private readonly string _accountEmail;

        public MsalTokenProvider(IPublicClientApplication msalClient, string accountEmail)
        {
            _msalClient = msalClient;
            _accountEmail = accountEmail;
        }

        public AllowedHostsValidator AllowedHostsValidator { get; } = new();

        public async Task<string> GetAuthorizationTokenAsync(
            Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<IAccount> accounts = await _msalClient.GetAccountsAsync();
            IAccount? account = accounts.FirstOrDefault(a =>
                string.Equals(a.Username, _accountEmail, StringComparison.OrdinalIgnoreCase));

            if (account is null)
            {
                return string.Empty;
            }

            try
            {
                AuthenticationResult result = await _msalClient
                    .AcquireTokenSilent(OutlookCalendarProvider.Scopes, account)
                    .ExecuteAsync(cancellationToken);
                return result.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                return string.Empty;
            }
        }
    }
}
