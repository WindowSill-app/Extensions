using System.Net;
using Microsoft.Extensions.Logging;
using WindowSill.API;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Core;

/// <summary>
/// Decorates an <see cref="ICalendarAccountClient"/> with automatic auth-refresh
/// and retry. Callers that need raw client access can use <see cref="Client"/>.
/// </summary>
internal sealed class CalendarAccountClientDecorator : IAsyncDisposable
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarAccountClientDecorator"/> class.
    /// </summary>
    /// <param name="client">The underlying calendar account client.</param>
    public CalendarAccountClientDecorator(ICalendarAccountClient client)
    {
        Client = client;
        _logger = this.Log();
    }

    /// <summary>
    /// Gets the underlying client for direct access.
    /// </summary>
    public ICalendarAccountClient Client { get; }

    /// <summary>
    /// Gets the account this decorator is scoped to.
    /// </summary>
    public CalendarAccount Account => Client.Account;

    /// <summary>
    /// Retrieves calendars.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The calendars available in this account.</returns>
    public Task<IReadOnlyList<CalendarInfo>> GetCalendarsAsync(CancellationToken cancellationToken = default)
    {
        return WithAuthRetryAsync(ct => Client.GetCalendarsAsync(ct), cancellationToken);
    }

    /// <summary>
    /// Retrieves events within the specified range.
    /// </summary>
    /// <param name="from">The start of the time range (inclusive).</param>
    /// <param name="to">The end of the time range (exclusive).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The events occurring within the specified time range.</returns>
    public Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        return WithAuthRetryAsync(ct => Client.GetEventsAsync(from, to, ct), cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return Client.DisposeAsync();
    }

    /// <summary>
    /// Executes an operation and retries once after refreshing auth if the
    /// failure looks like an authentication/authorization error.
    /// </summary>
    private async Task<T> WithAuthRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        try
        {
            return await operation(cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested && IsAuthError(ex))
        {
            _logger.LogWarning(
                ex,
                "Auth failure for account {AccountId}, attempting token refresh.",
                Account.Id);

            bool refreshed = await Client.RefreshAuthAsync(cancellationToken);
            if (!refreshed)
            {
                _logger.LogWarning("Token refresh failed for account {AccountId}. Re-throwing.", Account.Id);
                throw;
            }

            return await operation(cancellationToken);
        }
    }

    /// <summary>
    /// Determines whether an exception indicates an authentication or authorization failure.
    /// </summary>
    private static bool IsAuthError(Exception ex)
    {
        return ex switch
        {
            HttpRequestException httpEx => httpEx.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            InvalidOperationException ioEx => ioEx.Message.Contains("token", StringComparison.OrdinalIgnoreCase)
                                           || ioEx.Message.Contains("auth", StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }
}
