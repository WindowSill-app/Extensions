using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Providers.CalDav;

namespace WindowSill.Date.Core.Providers.ICloud;

/// <summary>
/// Per-account client for Apple iCloud Calendar, extending CalDAV with Apple-specific
/// principal discovery flow. iCloud requires: principal URL → calendar home → calendars.
/// </summary>
internal sealed class ICloudCalendarAccountClient : CalDavCalendarAccountClient
{
    private static readonly XNamespace DavNs = "DAV:";
    private static readonly XNamespace CalDavNs = "urn:ietf:params:xml:ns:caldav";

    private const string ICloudCalDavServer = "https://caldav.icloud.com";

    private readonly IReadOnlyDictionary<string, string> _authData;
    private string? _calendarHomeUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="ICloudCalendarAccountClient"/> class.
    /// </summary>
    /// <param name="account">The account this client is scoped to.</param>
    /// <param name="authData">The persisted auth data (username, password).</param>
    internal ICloudCalendarAccountClient(CalendarAccount account, IReadOnlyDictionary<string, string> authData)
        : base(account, authData)
    {
        _authData = authData;
        _calendarHomeUrl = authData.GetValueOrDefault("calendar_home");
    }

    /// <inheritdoc />
    protected override string CalDavBaseUrl => ICloudCalDavServer;

    /// <summary>
    /// Discovers the iCloud calendar home URL via principal discovery,
    /// then lists calendars under it.
    /// </summary>
    public override async Task<IReadOnlyList<CalendarInfo>> GetCalendarsAsync(CancellationToken cancellationToken)
    {
        string calendarHome = await DiscoverCalendarHomeAsync(cancellationToken);

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

        HttpClient client = await GetClientAsync(cancellationToken);
        using var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), calendarHome)
        {
            Content = new StringContent(propfindBody, Encoding.UTF8, "application/xml"),
        };
        request.Headers.Add("Depth", "1");

        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        string xml = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseCalendars(xml, calendarHome);
    }

    /// <summary>
    /// Fetches events for each calendar using the full calendar href.
    /// </summary>
    public override async Task<IReadOnlyList<Models.CalendarEvent>> GetEventsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CalendarInfo> calendars = await GetCalendarsAsync(cancellationToken);
        HttpClient client = await GetClientAsync(cancellationToken);
        var allEvents = new List<Models.CalendarEvent>();

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

            // Calendar.Id stores the full href for iCloud calendars.
            string calendarUrl = calendar.Id.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? calendar.Id
                : $"{ICloudCalDavServer}{calendar.Id}";

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
            allEvents.AddRange(ParseEventsFromICalData(xml, calendar));
        }

        return allEvents;
    }

    /// <summary>
    /// Validates the credentials by attempting principal discovery.
    /// Returns the calendar home URL if successful.
    /// </summary>
    internal static async Task<string> ValidateAndDiscoverAsync(string appleId, string appPassword, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{appleId}:{appPassword}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);

        // Step 1: Discover current-user-principal.
        string principalUrl = await DiscoverPrincipalUrlAsync(client, ICloudCalDavServer, cancellationToken);

        // Step 2: Discover calendar-home-set.
        string calendarHome = await DiscoverCalendarHomeFromPrincipalAsync(client, principalUrl, cancellationToken);

        return calendarHome;
    }

    /// <summary>
    /// Discovers the calendar home URL, caching the result.
    /// </summary>
    private async Task<string> DiscoverCalendarHomeAsync(CancellationToken cancellationToken)
    {
        if (_calendarHomeUrl is not null)
        {
            return _calendarHomeUrl;
        }

        HttpClient client = await GetClientAsync(cancellationToken);

        string principalUrl = await DiscoverPrincipalUrlAsync(client, ICloudCalDavServer, cancellationToken);
        _calendarHomeUrl = await DiscoverCalendarHomeFromPrincipalAsync(client, principalUrl, cancellationToken);

        return _calendarHomeUrl;
    }

    private static async Task<string> DiscoverPrincipalUrlAsync(HttpClient client, string serverUrl, CancellationToken cancellationToken)
    {
        string body = """
            <?xml version="1.0" encoding="utf-8"?>
            <d:propfind xmlns:d="DAV:">
              <d:prop>
                <d:current-user-principal/>
              </d:prop>
            </d:propfind>
            """;

        using var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), serverUrl)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/xml"),
        };
        request.Headers.Add("Depth", "0");

        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        string xml = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = XDocument.Parse(xml);

        string? principalHref = doc.Descendants(DavNs + "current-user-principal")
            .Descendants(DavNs + "href")
            .FirstOrDefault()?.Value;

        if (string.IsNullOrEmpty(principalHref))
        {
            throw new InvalidOperationException("iCloud did not return a current-user-principal.");
        }

        return principalHref.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? principalHref
            : $"{serverUrl.TrimEnd('/')}{principalHref}";
    }

    private static async Task<string> DiscoverCalendarHomeFromPrincipalAsync(HttpClient client, string principalUrl, CancellationToken cancellationToken)
    {
        string body = """
            <?xml version="1.0" encoding="utf-8"?>
            <d:propfind xmlns:d="DAV:" xmlns:c="urn:ietf:params:xml:ns:caldav">
              <d:prop>
                <c:calendar-home-set/>
              </d:prop>
            </d:propfind>
            """;

        using var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), principalUrl)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/xml"),
        };
        request.Headers.Add("Depth", "0");

        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        string xml = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = XDocument.Parse(xml);

        string? homeHref = doc.Descendants(CalDavNs + "calendar-home-set")
            .Descendants(DavNs + "href")
            .FirstOrDefault()?.Value;

        if (string.IsNullOrEmpty(homeHref))
        {
            throw new InvalidOperationException("iCloud did not return a calendar-home-set.");
        }

        return homeHref.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? homeHref
            : $"{ICloudCalDavServer}{homeHref}";
    }

    private async Task<HttpClient> GetClientAsync(CancellationToken cancellationToken)
    {
        // Use reflection-free approach: create a simple authenticated client.
        // The base class manages its own client, but we need direct access for discovery.
        string? username = _authData.GetValueOrDefault("username");
        string? password = _authData.GetValueOrDefault("password");

        var client = new HttpClient();
        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);
        }

        return client;
    }

    private List<CalendarInfo> ParseCalendars(string xml, string calendarHomeUrl)
    {
        var calendars = new List<CalendarInfo>();
        var appleNs = XNamespace.Get("http://apple.com/ns/ical/");

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
                string? color = responseEl.Descendants(appleNs + "calendar-color").FirstOrDefault()?.Value;

                if (href is null)
                {
                    continue;
                }

                // Store the full URL as the calendar ID for event fetching.
                string fullUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? href
                    : $"{ICloudCalDavServer}{href}";

                calendars.Add(new CalendarInfo
                {
                    Id = fullUrl,
                    AccountId = Account.Id,
                    Name = displayName ?? "Calendar",
                    Color = NormalizeColor(color),
                    IsDefault = false,
                    IsReadOnly = false,
                });
            }
        }
        catch
        {
            // Malformed XML — return empty list.
        }

        return calendars;
    }

    private List<Models.CalendarEvent> ParseEventsFromICalData(string xml, CalendarInfo calendar)
    {
        var events = new List<Models.CalendarEvent>();

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

                var ical = Ical.Net.Calendar.Load(icalData);
                foreach (Ical.Net.CalendarComponents.CalendarEvent vEvent in ical.Events)
                {
                    events.Add(CalDavEventMapper.MapVEvent(
                        vEvent, calendar, CalendarProviderType.ICloud, Account.Email));
                }
            }
        }
        catch
        {
            // Malformed iCal data — skip.
        }

        return events;
    }

    /// <summary>
    /// Normalizes Apple's hex color format (e.g., "#1BADF8FF") to standard 6-char hex.
    /// </summary>
    internal static string? NormalizeColor(string? color)
    {
        if (color is null)
        {
            return null;
        }

        color = color.TrimStart('#');

        // Apple often returns 8-char RGBA; take the first 6 (RGB).
        if (color.Length >= 6)
        {
            return $"#{color[..6]}";
        }

        return null;
    }
}
