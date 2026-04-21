using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Ical.Net;
using WindowSill.Date.Core.Models;
using CalendarEvent = WindowSill.Date.Core.Models.CalendarEvent;
using ICalCalendarEvent = Ical.Net.CalendarComponents.CalendarEvent;

namespace WindowSill.Date.Core.Providers.CalDav;

/// <summary>
/// Per-account client for CalDAV calendar operations (RFC 4791).
/// Uses PROPFIND and REPORT methods to discover calendars and fetch events.
/// </summary>
internal class CalDavCalendarAccountClient : ICalendarAccountClient
{
    private static readonly XNamespace DavNs = "DAV:";
    private static readonly XNamespace CalDavNs = "urn:ietf:params:xml:ns:caldav";
    private static readonly XNamespace AppleNs = "http://apple.com/ns/ical/";

    private readonly IReadOnlyDictionary<string, string> _authData;
    private HttpClient? _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalDavCalendarAccountClient"/> class.
    /// </summary>
    /// <param name="account">The account this client is scoped to.</param>
    /// <param name="authData">The persisted auth data (server_url, username, password).</param>
    internal CalDavCalendarAccountClient(CalendarAccount account, IReadOnlyDictionary<string, string> authData)
    {
        Account = account;
        _authData = authData;
    }

    /// <inheritdoc />
    public CalendarAccount Account { get; }

    /// <summary>
    /// Gets the base server URL for CalDAV operations.
    /// Subclasses (e.g., iCloud) can override this.
    /// </summary>
    protected virtual string CalDavBaseUrl => $"https://caldav.example.com"; // Overridden per account.

    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarInfo>> GetCalendarsAsync(CancellationToken cancellationToken)
    {
        HttpClient client = await GetOrCreateHttpClientAsync(cancellationToken);

        string serverUrl = _authData.GetValueOrDefault("server_url")
            ?? CalDavBaseUrl;

        // PROPFIND to discover calendars.
        string propfindBody = """
            <?xml version="1.0" encoding="utf-8"?>
            <d:propfind xmlns:d="DAV:" xmlns:cs="http://calendarserver.org/ns/" xmlns:c="urn:ietf:params:xml:ns:caldav" xmlns:ic="http://apple.com/ns/ical/">
              <d:prop>
                <d:displayname/>
                <d:resourcetype/>
                <ic:calendar-color/>
              </d:prop>
            </d:propfind>
            """;

        using var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), serverUrl)
        {
            Content = new StringContent(propfindBody, Encoding.UTF8, "application/xml"),
        };
        request.Headers.Add("Depth", "1");

        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        string xml = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseCalendarsFromPropfind(xml);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        HttpClient client = await GetOrCreateHttpClientAsync(cancellationToken);

        IReadOnlyList<CalendarInfo> calendars = await GetCalendarsAsync(cancellationToken);
        var allEvents = new List<CalendarEvent>();

        foreach (CalendarInfo calendar in calendars)
        {
            string reportBody = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <c:calendar-query xmlns:d="DAV:" xmlns:c="urn:ietf:params:xml:ns:caldav">
                  <d:prop>
                    <d:getetag/>
                    <c:calendar-data/>
                  </d:prop>
                  <c:filter>
                    <c:comp-filter name="VCALENDAR">
                      <c:comp-filter name="VEVENT">
                        <c:time-range start="{from.UtcDateTime:yyyyMMddTHHmmssZ}" end="{to.UtcDateTime:yyyyMMddTHHmmssZ}"/>
                      </c:comp-filter>
                    </c:comp-filter>
                  </c:filter>
                </c:calendar-query>
                """;

            string serverUrl = _authData.GetValueOrDefault("server_url")
                ?? CalDavBaseUrl;
            string calendarUrl = $"{serverUrl.TrimEnd('/')}/{calendar.Id}/";

            using var request = new HttpRequestMessage(new HttpMethod("REPORT"), calendarUrl)
            {
                Content = new StringContent(reportBody, Encoding.UTF8, "application/xml"),
            };
            request.Headers.Add("Depth", "1");

            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                continue;
            }

            string xml = await response.Content.ReadAsStringAsync(cancellationToken);
            allEvents.AddRange(ParseEventsFromReport(xml, calendar));
        }

        return allEvents;
    }

    /// <inheritdoc />
    public Task<bool> RefreshAuthAsync(CancellationToken cancellationToken)
    {
        // CalDAV uses basic auth or app-specific passwords — no token refresh needed.
        _httpClient?.Dispose();
        _httpClient = null;
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        _httpClient?.Dispose();
        _httpClient = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();
        _httpClient = null;
        return ValueTask.CompletedTask;
    }

    private Task<HttpClient> GetOrCreateHttpClientAsync(CancellationToken cancellationToken)
    {
        if (_httpClient is not null)
        {
            return Task.FromResult(_httpClient);
        }

        string? username = _authData.GetValueOrDefault("username");
        string? password = _authData.GetValueOrDefault("password");

        _httpClient = new HttpClient();

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);
        }

        return Task.FromResult(_httpClient);
    }

    private List<CalendarInfo> ParseCalendarsFromPropfind(string xml)
    {
        var calendars = new List<CalendarInfo>();

        try
        {
            var doc = XDocument.Parse(xml);

            foreach (XElement responseEl in doc.Descendants(DavNs + "response"))
            {
                XElement? resourceType = responseEl.Descendants(DavNs + "resourcetype").FirstOrDefault();
                if (resourceType?.Descendants(CalDavNs + "calendar").Any() != true)
                {
                    continue;
                }

                string? href = responseEl.Element(DavNs + "href")?.Value;
                string? displayName = responseEl.Descendants(DavNs + "displayname").FirstOrDefault()?.Value;
                string? color = responseEl.Descendants(AppleNs + "calendar-color").FirstOrDefault()?.Value;

                calendars.Add(new CalendarInfo
                {
                    Id = href?.Trim('/').Split('/').LastOrDefault() ?? Guid.NewGuid().ToString(),
                    AccountId = Account.Id,
                    Name = displayName ?? "Calendar",
                    Color = color,
                    IsDefault = false,
                    IsReadOnly = false,
                });
            }
        }
        catch (Exception)
        {
            // Malformed XML — return empty list.
        }

        return calendars;
    }

    private List<CalendarEvent> ParseEventsFromReport(string xml, CalendarInfo calendar)
    {
        var events = new List<CalendarEvent>();

        try
        {
            var doc = XDocument.Parse(xml);

            foreach (XElement responseEl in doc.Descendants(DavNs + "response"))
            {
                string? icalData = responseEl.Descendants(CalDavNs + "calendar-data").FirstOrDefault()?.Value;
                if (string.IsNullOrEmpty(icalData))
                {
                    continue;
                }

                var ical = Calendar.Load(icalData);
                foreach (ICalCalendarEvent vEvent in ical.Events)
                {
                    events.Add(MapVEventToCalendarEvent(vEvent, calendar));
                }
            }
        }
        catch (Exception)
        {
            // Malformed response — return whatever we've parsed so far.
        }

        return events;
    }

    private CalendarEvent MapVEventToCalendarEvent(ICalCalendarEvent vEvent, CalendarInfo calendar)
    {
        bool isAllDay = !vEvent.Start.HasTime;

        DateTimeOffset startTime = isAllDay
            ? new DateTimeOffset(vEvent.Start.Date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            : new DateTimeOffset(vEvent.Start.Value, TimeSpan.Zero);

        DateTimeOffset endTime = vEvent.End is not null
            ? (isAllDay
                ? new DateTimeOffset(vEvent.End.Date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
                : new DateTimeOffset(vEvent.End.Value, TimeSpan.Zero))
            : startTime;

        string? description = vEvent.Description;
        string? location = vEvent.Location;

        VideoCallInfo? videoCall = VideoCallDetector.Detect(description, location);

        return new CalendarEvent
        {
            Id = vEvent.Uid ?? Guid.NewGuid().ToString(),
            CalendarId = calendar.Id,
            AccountId = Account.Id,
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
            ResponseStatus = AttendeeResponseStatus.NotResponded, // CalDAV doesn't always provide this directly.
            VideoCall = videoCall,
            Organizer = vEvent.Organizer is not null
                ? new CalendarEventAttendee(
                    vEvent.Organizer.CommonName,
                    vEvent.Organizer.Value?.Authority ?? string.Empty,
                    AttendeeResponseStatus.Accepted,
                    IsOrganizer: true)
                : null,
            Attendees = vEvent.Attendees?
                .Select(a => new CalendarEventAttendee(
                    a.CommonName,
                    a.Value?.Authority ?? string.Empty,
                    AttendeeResponseStatus.NotResponded))
                .ToList() ?? [],
            RecurrenceRule = vEvent.RecurrenceRules?.FirstOrDefault()?.ToString(),
            Color = calendar.Color,
            IsPrivate = string.Equals(vEvent.Class, "PRIVATE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(vEvent.Class, "CONFIDENTIAL", StringComparison.OrdinalIgnoreCase),
            ProviderType = CalendarProviderType.CalDav,
        };
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
