using System.ComponentModel.Composition;

using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;

using WindowSill.CalendarPlus.Core.Abstractions;
using WindowSill.CalendarPlus.Core.Auth;

using CalendarEventModel = WindowSill.CalendarPlus.Core.Models.CalendarEvent;
using CalendarInfoModel = WindowSill.CalendarPlus.Core.Models.CalendarInfo;

namespace WindowSill.CalendarPlus.Core.Providers;

/// <summary>
/// Calendar provider for Microsoft Outlook/365 using Microsoft Graph API.
/// </summary>
[Export(typeof(OutlookCalendarProvider))]
internal sealed class OutlookCalendarProvider : ICalendarProvider
{
    private readonly MicrosoftAuthHelper _authHelper;
    private GraphServiceClient? _graphClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookCalendarProvider"/> class.
    /// </summary>
    /// <param name="authHelper">The Microsoft authentication helper.</param>
    [ImportingConstructor]
    public OutlookCalendarProvider(MicrosoftAuthHelper authHelper)
    {
        _authHelper = authHelper;
    }

    /// <inheritdoc/>
    public string ProviderId => "microsoft";

    /// <inheritdoc/>
    public string DisplayName => "Microsoft Outlook";

    /// <inheritdoc/>
    public bool IsAuthenticated => _authHelper.IsAuthenticated;

    /// <inheritdoc/>
    public async Task<bool> AuthenticateAsync(CancellationToken cancellationToken)
    {
        bool result = await _authHelper.AuthenticateAsync(cancellationToken);
        if (result)
        {
            _graphClient = CreateGraphClient();
        }

        return result;
    }

    /// <inheritdoc/>
    public Task SignOutAsync()
    {
        _graphClient = null;
        return _authHelper.SignOutAsync();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CalendarInfoModel>> GetCalendarsAsync(CancellationToken cancellationToken)
    {
        if (_graphClient is null)
        {
            return [];
        }

        try
        {
            var calendarsResponse = await _graphClient.Me.Calendars.GetAsync(cancellationToken: cancellationToken);
            if (calendarsResponse?.Value is null)
            {
                return [];
            }

            return calendarsResponse.Value.Select(c => new CalendarInfoModel
            {
                Id = c.Id ?? string.Empty,
                AccountId = _authHelper.AccountEmail ?? "microsoft",
                Name = c.Name ?? "Calendar",
                HexColor = c.HexColor,
                IsReadOnly = c.CanEdit == false,
            }).ToList();
        }
        catch (Exception)
        {
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CalendarEventModel>> GetEventsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
    {
        if (_graphClient is null)
        {
            return [];
        }

        try
        {
            string startUtc = start.UtcDateTime.ToString("o");
            string endUtc = end.UtcDateTime.ToString("o");

            var eventsResponse = await _graphClient.Me.CalendarView.GetAsync(config =>
            {
                config.QueryParameters.StartDateTime = startUtc;
                config.QueryParameters.EndDateTime = endUtc;
                config.QueryParameters.Select = ["id", "subject", "start", "end", "location", "body", "organizer", "isAllDay", "isCancelled", "onlineMeeting", "onlineMeetingUrl", "showAs", "isOrganizer", "recurrence"];
                config.QueryParameters.Orderby = ["start/dateTime"];
                config.QueryParameters.Top = 100;
            }, cancellationToken);

            if (eventsResponse?.Value is null)
            {
                return [];
            }

            string accountId = _authHelper.AccountEmail ?? "microsoft";

            return eventsResponse.Value.Select(e => new CalendarEventModel
            {
                Id = e.Id ?? Guid.NewGuid().ToString(),
                CalendarId = "default",
                AccountId = accountId,
                Subject = e.Subject ?? "(No subject)",
                Start = ParseGraphDateTime(e.Start),
                End = ParseGraphDateTime(e.End),
                Location = e.Location?.DisplayName,
                Body = e.Body?.Content,
                OrganizerName = e.Organizer?.EmailAddress?.Name,
                IsAllDay = e.IsAllDay ?? false,
                IsCancelled = e.IsCancelled ?? false,
                OnlineMeetingUrl = e.OnlineMeetingUrl ?? e.OnlineMeeting?.JoinUrl,
                OnlineMeetingProvider = e.OnlineMeeting?.JoinUrl is not null ? "Teams" : null,
                ShowAs = e.ShowAs?.ToString(),
                IsRecurring = e.Recurrence is not null,
            }).ToList();
        }
        catch (Exception)
        {
            return [];
        }
    }

    /// <summary>
    /// Parses a Microsoft Graph <see cref="DateTimeTimeZone"/> into a <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="dt">The Graph datetime with timezone.</param>
    /// <returns>The parsed <see cref="DateTimeOffset"/>, or <see cref="DateTimeOffset.MinValue"/> if parsing fails.</returns>
    internal static DateTimeOffset ParseGraphDateTime(DateTimeTimeZone? dt)
    {
        if (dt is null)
        {
            return DateTimeOffset.MinValue;
        }

        if (DateTime.TryParse(dt.DateTime, out DateTime parsed))
        {
            try
            {
                TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(dt.TimeZone ?? "UTC");
                return new DateTimeOffset(parsed, tz.GetUtcOffset(parsed));
            }
            catch
            {
                return new DateTimeOffset(parsed, TimeSpan.Zero);
            }
        }

        return DateTimeOffset.MinValue;
    }

    private GraphServiceClient CreateGraphClient()
    {
        var authProvider = new BaseBearerTokenAuthenticationProvider(
            new TokenProvider(_authHelper));
        return new GraphServiceClient(new HttpClient(), authProvider);
    }

    /// <summary>
    /// Token provider adapter for Microsoft Graph authentication.
    /// </summary>
    private sealed class TokenProvider : IAccessTokenProvider
    {
        private readonly MicrosoftAuthHelper _authHelper;

        public TokenProvider(MicrosoftAuthHelper authHelper) => _authHelper = authHelper;

        /// <inheritdoc/>
        public AllowedHostsValidator AllowedHostsValidator { get; } = new();

        /// <inheritdoc/>
        public async Task<string> GetAuthorizationTokenAsync(
            Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            return await _authHelper.GetAccessTokenAsync(cancellationToken) ?? string.Empty;
        }
    }
}
