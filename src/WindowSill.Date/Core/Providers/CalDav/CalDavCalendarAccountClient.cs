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
/// Supports the full discovery flow: principal URL → calendar home → calendars.
/// Uses PROPFIND and REPORT methods for calendar and event operations.
/// </summary>
internal class CalDavCalendarAccountClient : ICalendarAccountClient
{
    private static readonly XNamespace DavNs = "DAV:";
    private static readonly XNamespace CalDavNs = "urn:ietf:params:xml:ns:caldav";
    private static readonly XNamespace AppleNs = "http://apple.com/ns/ical/";

    private readonly IReadOnlyDictionary<string, string> _authData;
    private HttpClient? _httpClient;
    private string? _calendarHomeUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalDavCalendarAccountClient"/> class.
    /// </summary>
    /// <param name="account">The account this client is scoped to.</param>
    /// <param name="authData">The persisted auth data (server_url, username, password).</param>
    internal CalDavCalendarAccountClient(CalendarAccount account, IReadOnlyDictionary<string, string> authData)
    {
        Account = account;
        _authData = authData;
        _calendarHomeUrl = authData.GetValueOrDefault("calendar_home");
    }

    /// <inheritdoc />
    public CalendarAccount Account { get; }

    /// <summary>
    /// Gets the base server URL for CalDAV operations.
    /// Subclasses (e.g., iCloud) can override this.
    /// </summary>
    protected virtual string CalDavBaseUrl => "https://caldav.example.com";

    /// <summary>
    /// Gets the server URL from auth data or the base URL.
    /// </summary>
    protected string ServerUrl => _authData.GetValueOrDefault("server_url") ?? CalDavBaseUrl;

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<CalendarInfo>> GetCalendarsAsync(CancellationToken cancellationToken)
    {
        HttpClient client = await GetOrCreateHttpClientAsync(cancellationToken);
        string calendarHome = await DiscoverCalendarHomeAsync(client, cancellationToken);

        // PROPFIND the calendar home to list calendars.
        string propfindBody = """
            <?xml version="1.0" encoding="utf-8"?>
            <d:propfind xmlns:d="DAV:" xmlns:c="urn:ietf:params:xml:ns:caldav" xmlns:ic="http://apple.com/ns/ical/">
              <d:prop>
                <d:displayname/>
                <d:resourcetype/>
                <ic:calendar-color/>
              </d:prop>
            </d:propfind>
            """;

        using var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), calendarHome)
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
    public virtual async Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(
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

            // Calendar.Id stores the full href from PROPFIND.
            string calendarUrl = ResolveCalendarUrl(calendar.Id);

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

    /// <summary>
    /// Validates credentials and discovers the calendar home URL.
    /// Used during the connect flow.
    /// </summary>
    /// <returns>The calendar home URL if discovery succeeds.</returns>
    internal static async Task<string> ValidateAndDiscoverAsync(
        string serverUrl, string username, string password, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);

        return await DiscoverCalendarHomeStaticAsync(client, serverUrl, cancellationToken);
    }

    /// <summary>
    /// Discovers the calendar home URL via principal discovery, caching the result.
    /// Falls back to the server URL if principal discovery fails (some servers
    /// serve calendars directly at the base URL).
    /// </summary>
    private async Task<string> DiscoverCalendarHomeAsync(HttpClient client, CancellationToken cancellationToken)
    {
        if (_calendarHomeUrl is not null)
        {
            return _calendarHomeUrl;
        }

        try
        {
            _calendarHomeUrl = await DiscoverCalendarHomeStaticAsync(client, ServerUrl, cancellationToken);
        }
        catch
        {
            // Principal discovery not supported — fall back to server URL.
            _calendarHomeUrl = ServerUrl;
        }

        return _calendarHomeUrl;
    }

    /// <summary>
    /// Static discovery: principal URL → calendar-home-set.
    /// </summary>
    private static async Task<string> DiscoverCalendarHomeStaticAsync(
        HttpClient client, string serverUrl, CancellationToken cancellationToken)
    {
        // Step 1: Discover current-user-principal.
        string principalBody = """
            <?xml version="1.0" encoding="utf-8"?>
            <d:propfind xmlns:d="DAV:">
              <d:prop>
                <d:current-user-principal/>
              </d:prop>
            </d:propfind>
            """;

        using var principalRequest = new HttpRequestMessage(new HttpMethod("PROPFIND"), serverUrl)
        {
            Content = new StringContent(principalBody, Encoding.UTF8, "application/xml"),
        };
        principalRequest.Headers.Add("Depth", "0");

        using HttpResponseMessage principalResponse = await client.SendAsync(principalRequest, cancellationToken);
        principalResponse.EnsureSuccessStatusCode();

        string principalXml = await principalResponse.Content.ReadAsStringAsync(cancellationToken);
        var principalDoc = XDocument.Parse(principalXml);

        string? principalHref = principalDoc.Descendants(DavNs + "current-user-principal")
            .Descendants(DavNs + "href")
            .FirstOrDefault()?.Value;

        if (string.IsNullOrEmpty(principalHref))
        {
            throw new InvalidOperationException("Server did not return a current-user-principal.");
        }

        string principalUrl = ResolveHref(serverUrl, principalHref);

        // Step 2: Discover calendar-home-set.
        string homeBody = """
            <?xml version="1.0" encoding="utf-8"?>
            <d:propfind xmlns:d="DAV:" xmlns:c="urn:ietf:params:xml:ns:caldav">
              <d:prop>
                <c:calendar-home-set/>
              </d:prop>
            </d:propfind>
            """;

        using var homeRequest = new HttpRequestMessage(new HttpMethod("PROPFIND"), principalUrl)
        {
            Content = new StringContent(homeBody, Encoding.UTF8, "application/xml"),
        };
        homeRequest.Headers.Add("Depth", "0");

        using HttpResponseMessage homeResponse = await client.SendAsync(homeRequest, cancellationToken);
        homeResponse.EnsureSuccessStatusCode();

        string homeXml = await homeResponse.Content.ReadAsStringAsync(cancellationToken);
        var homeDoc = XDocument.Parse(homeXml);

        string? homeHref = homeDoc.Descendants(CalDavNs + "calendar-home-set")
            .Descendants(DavNs + "href")
            .FirstOrDefault()?.Value;

        if (string.IsNullOrEmpty(homeHref))
        {
            throw new InvalidOperationException("Server did not return a calendar-home-set.");
        }

        return ResolveHref(serverUrl, homeHref);
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

                if (href is null)
                {
                    continue;
                }

                calendars.Add(new CalendarInfo
                {
                    // Store the full href for use in REPORT requests.
                    Id = href,
                    AccountId = Account.Id,
                    Name = displayName ?? "Calendar",
                    Color = NormalizeColor(color),
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
                    events.Add(CalDavEventMapper.MapVEvent(vEvent, calendar, accountEmail: Account.Email));
                }
            }
        }
        catch (Exception)
        {
            // Malformed response — return whatever we've parsed so far.
        }

        return events;
    }

    /// <summary>
    /// Resolves a calendar ID (href) to a full URL for REPORT requests.
    /// </summary>
    private string ResolveCalendarUrl(string calendarId)
    {
        if (calendarId.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return calendarId;
        }

        // Relative href — resolve against server URL.
        return ResolveHref(ServerUrl, calendarId);
    }

    /// <summary>
    /// Resolves a potentially-relative href against a base server URL.
    /// </summary>
    private static string ResolveHref(string serverUrl, string href)
    {
        if (href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return href;
        }

        var baseUri = new Uri(serverUrl);
        return new Uri(baseUri, href).AbsoluteUri;
    }

    /// <summary>
    /// Normalizes color formats (e.g., Apple's "#1BADF8FF" RGBA → "#1BADF8" RGB).
    /// </summary>
    internal static string? NormalizeColor(string? color)
    {
        if (color is null)
        {
            return null;
        }

        color = color.TrimStart('#');
        if (color.Length >= 6)
        {
            return $"#{color[..6]}";
        }

        return null;
    }
}
