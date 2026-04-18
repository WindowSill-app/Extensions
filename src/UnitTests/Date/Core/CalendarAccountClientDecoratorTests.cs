using System.Net;
using FluentAssertions;
using UnitTests.Date.Core.Fakes;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;

namespace UnitTests.Date.Core;

public class CalendarAccountClientDecoratorTests
{
    private static readonly DateTimeOffset From = DateTimeOffset.Now;
    private static readonly DateTimeOffset To = From.AddHours(24);

    public CalendarAccountClientDecoratorTests()
    {
        LoggingSetup.EnsureInitialized();
    }

    #region GetEventsAsync

    [Fact]
    public async Task GetEventsAsync_Success_DoesNotRefresh()
    {
        CalendarEvent expected = CreateEvent("e1");
        int callCount = 0;
        FakeCalendarAccountClient inner = new(CreateAccount(), (_, _, _) =>
        {
            callCount++;
            return Task.FromResult<IReadOnlyList<CalendarEvent>>([expected]);
        });

        CalendarAccountClientDecorator decorator = new(inner);

        IReadOnlyList<CalendarEvent> result = await decorator.GetEventsAsync(From, To);

        result.Should().HaveCount(1);
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetEventsAsync_AuthFailureThenSuccess_RetriesAfterRefresh()
    {
        CalendarEvent expected = CreateEvent("e1");
        int callCount = 0;
        FakeCalendarAccountClient inner = new(CreateAccount(), (_, _, _) =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized);
            }

            return Task.FromResult<IReadOnlyList<CalendarEvent>>([expected]);
        });

        CalendarAccountClientDecorator decorator = new(inner);

        IReadOnlyList<CalendarEvent> result = await decorator.GetEventsAsync(From, To);

        result.Should().HaveCount(1);
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task GetEventsAsync_AuthFailureAndRefreshFails_Throws()
    {
        FakeCalendarAccountClient inner = new(
            CreateAccount(),
            (_, _, _) => throw new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized))
        {
            RefreshResult = false,
        };

        CalendarAccountClientDecorator decorator = new(inner);

        Func<Task> act = () => decorator.GetEventsAsync(From, To);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetEventsAsync_NonAuthFailure_DoesNotRetry()
    {
        int callCount = 0;
        FakeCalendarAccountClient inner = new(CreateAccount(), (_, _, _) =>
        {
            callCount++;
            throw new HttpRequestException("Server Error", null, HttpStatusCode.InternalServerError);
        });

        CalendarAccountClientDecorator decorator = new(inner);

        Func<Task> act = () => decorator.GetEventsAsync(From, To);

        await act.Should().ThrowAsync<HttpRequestException>();
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetEventsAsync_ForbiddenError_RetriesAfterRefresh()
    {
        int callCount = 0;
        FakeCalendarAccountClient inner = new(CreateAccount(), (_, _, _) =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new HttpRequestException("Forbidden", null, HttpStatusCode.Forbidden);
            }

            return Task.FromResult<IReadOnlyList<CalendarEvent>>([]);
        });

        CalendarAccountClientDecorator decorator = new(inner);

        IReadOnlyList<CalendarEvent> result = await decorator.GetEventsAsync(From, To);

        callCount.Should().Be(2);
    }

    [Fact]
    public async Task GetEventsAsync_InvalidOperationWithTokenMessage_RetriesAfterRefresh()
    {
        int callCount = 0;
        FakeCalendarAccountClient inner = new(CreateAccount(), (_, _, _) =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new InvalidOperationException("No access token available. Call RefreshAuthAsync first.");
            }

            return Task.FromResult<IReadOnlyList<CalendarEvent>>([]);
        });

        CalendarAccountClientDecorator decorator = new(inner);

        IReadOnlyList<CalendarEvent> result = await decorator.GetEventsAsync(From, To);

        callCount.Should().Be(2);
    }

    [Fact]
    public async Task GetEventsAsync_CancellationException_DoesNotRetry()
    {
        using CancellationTokenSource cts = new();
        FakeCalendarAccountClient inner = new(CreateAccount(), (_, _, ct) =>
        {
            cts.Cancel();
            ct.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<CalendarEvent>>([]);
        });

        CalendarAccountClientDecorator decorator = new(inner);

        Func<Task> act = () => decorator.GetEventsAsync(From, To, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region GetCalendarsAsync

    [Fact]
    public async Task GetCalendarsAsync_AuthFailure_RetriesAfterRefresh()
    {
        int callCount = 0;
        CalendarAccount account = CreateAccount();
        FakeCalendarAccountClient inner = new(account);
        inner.GetCalendarsFunc = _ =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized);
            }

            return Task.FromResult<IReadOnlyList<CalendarInfo>>([]);
        };

        CalendarAccountClientDecorator decorator = new(inner);

        IReadOnlyList<CalendarInfo> result = await decorator.GetCalendarsAsync();

        callCount.Should().Be(2);
    }

    #endregion

    #region Passthrough

    [Fact]
    public async Task Client_ExposesInnerClient()
    {
        FakeCalendarAccountClient inner = new(CreateAccount());
        CalendarAccountClientDecorator decorator = new(inner);

        await decorator.Client.DisconnectAsync();

        inner.WasDisconnected.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_DelegatesToInner()
    {
        FakeCalendarAccountClient inner = new(CreateAccount());
        CalendarAccountClientDecorator decorator = new(inner);

        await decorator.DisposeAsync();

        inner.WasDisposed.Should().BeTrue();
    }

    [Fact]
    public void Account_DelegatesToInner()
    {
        CalendarAccount account = CreateAccount();
        FakeCalendarAccountClient inner = new(account);
        CalendarAccountClientDecorator decorator = new(inner);

        decorator.Account.Should().BeSameAs(account);
    }

    #endregion

    #region Helpers

    private static CalendarAccount CreateAccount() => new()
    {
        Id = "test_account",
        DisplayName = "Test",
        Email = "test@test.com",
        ProviderType = CalendarProviderType.Outlook,
    };

    private static CalendarEvent CreateEvent(string id) => new()
    {
        Id = id,
        CalendarId = "cal_1",
        AccountId = "test_account",
        Title = "Test Event",
        StartTime = From.AddHours(1),
        EndTime = From.AddHours(2),
        ProviderType = CalendarProviderType.Outlook,
    };

    #endregion
}
