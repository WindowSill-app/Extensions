using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;

namespace UnitTests.Date.Core.Fakes;

/// <summary>
/// A fake <see cref="ICalendarAccountClient"/> for unit testing.
/// </summary>
internal sealed class FakeCalendarAccountClient : ICalendarAccountClient
{
    private readonly Func<DateTimeOffset, DateTimeOffset, CancellationToken, Task<IReadOnlyList<CalendarEvent>>>? _getEvents;

    public FakeCalendarAccountClient(
        CalendarAccount account,
        Func<DateTimeOffset, DateTimeOffset, CancellationToken, Task<IReadOnlyList<CalendarEvent>>>? getEvents = null)
    {
        Account = account;
        _getEvents = getEvents;
    }

    /// <inheritdoc />
    public CalendarAccount Account { get; }

    /// <summary>
    /// Gets or sets the result that <see cref="RefreshAuthAsync"/> returns.
    /// </summary>
    public bool RefreshResult { get; set; } = true;

    /// <summary>
    /// Gets or sets a custom implementation for <see cref="GetCalendarsAsync"/>.
    /// </summary>
    public Func<CancellationToken, Task<IReadOnlyList<CalendarInfo>>>? GetCalendarsFunc { get; set; }

    /// <summary>
    /// Gets a value indicating whether <see cref="DisconnectAsync"/> was called.
    /// </summary>
    public bool WasDisconnected { get; private set; }

    /// <summary>
    /// Gets a value indicating whether <see cref="DisposeAsync"/> was called.
    /// </summary>
    public bool WasDisposed { get; private set; }

    /// <inheritdoc />
    public Task<IReadOnlyList<CalendarInfo>> GetCalendarsAsync(CancellationToken cancellationToken = default)
    {
        if (GetCalendarsFunc is not null)
        {
            return GetCalendarsFunc(cancellationToken);
        }

        return Task.FromResult<IReadOnlyList<CalendarInfo>>([]);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        if (_getEvents is not null)
        {
            return _getEvents(from, to, cancellationToken);
        }

        return Task.FromResult<IReadOnlyList<CalendarEvent>>([]);
    }

    /// <inheritdoc />
    public Task<bool> RefreshAuthAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(RefreshResult);
    }

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        WasDisconnected = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        WasDisposed = true;
        return ValueTask.CompletedTask;
    }
}
