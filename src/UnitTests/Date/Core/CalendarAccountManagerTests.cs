using FluentAssertions;
using UnitTests.Date.Core.Fakes;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;
using Path = System.IO.Path;

namespace UnitTests.Date.Core;

public class CalendarAccountManagerTests : IDisposable
{
    private readonly string _tempFolder;
    private readonly CalendarDataStore _dataStore;

    public CalendarAccountManagerTests()
    {
        LoggingSetup.EnsureInitialized();
        _tempFolder = Path.Combine(Path.GetTempPath(), $"WindowSillTests_{Guid.NewGuid():N}");
        _dataStore = new CalendarDataStore(_tempFolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempFolder))
        {
            Directory.Delete(_tempFolder, recursive: true);
        }
    }

    #region GetAccountsAsync

    [Fact]
    public async Task GetAccountsAsync_NoAccounts_ReturnsEmpty()
    {
        using CalendarAccountManager manager = CreateManager();

        IReadOnlyList<CalendarAccount> accounts = await manager.GetAccountsAsync();

        accounts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAccountsAsync_AfterRegister_ReturnsAccount()
    {
        using CalendarAccountManager manager = CreateManager();
        CalendarAccount account = CreateAccount("test_1");

        await manager.RegisterAccountAsync(account, CancellationToken.None);
        IReadOnlyList<CalendarAccount> accounts = await manager.GetAccountsAsync();

        accounts.Should().HaveCount(1);
        accounts[0].Id.Should().Be("test_1");
    }

    [Fact]
    public async Task GetAccountsAsync_LoadsPersistedAccounts()
    {
        // Pre-persist an account.
        CalendarAccount account = CreateAccount("persisted_1");
        await _dataStore.SaveAsync(account);

        // Create a new manager that should load the persisted account.
        using CalendarAccountManager manager = CreateManager();
        IReadOnlyList<CalendarAccount> accounts = await manager.GetAccountsAsync();

        accounts.Should().HaveCount(1);
        accounts[0].Id.Should().Be("persisted_1");
    }

    #endregion

    #region RegisterAccountAsync

    [Fact]
    public async Task RegisterAccountAsync_PersistsToStore()
    {
        using CalendarAccountManager manager = CreateManager();
        CalendarAccount account = CreateAccount("persist_test");

        await manager.RegisterAccountAsync(account, CancellationToken.None);

        // Verify via a fresh data store read.
        CalendarAccount[] stored = await _dataStore.LoadAllAsync();
        stored.Should().ContainSingle(a => a.Id == "persist_test");
    }

    [Fact]
    public async Task RegisterAccountAsync_RaisesAccountAdded()
    {
        using CalendarAccountManager manager = CreateManager();
        CalendarAccount account = CreateAccount("event_test");
        CalendarAccount? addedAccount = null;
        manager.AccountAdded += (_, a) => addedAccount = a;

        await manager.RegisterAccountAsync(account, CancellationToken.None);

        addedAccount.Should().NotBeNull();
        addedAccount!.Id.Should().Be("event_test");
    }

    [Fact]
    public async Task RegisterAccountAsync_UnsupportedProvider_Throws()
    {
        // Manager with no providers registered.
        using CalendarAccountManager manager = CreateManager(
            new Dictionary<CalendarProviderType, ICalendarProvider>());

        CalendarAccount account = CreateAccount("no_provider");

        Func<Task> act = () => manager.RegisterAccountAsync(account, CancellationToken.None);

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    #endregion

    #region RemoveAccountAsync

    [Fact]
    public async Task RemoveAccountAsync_RemovesAndRaisesEvent()
    {
        using CalendarAccountManager manager = CreateManager();
        CalendarAccount account = CreateAccount("remove_test");
        await manager.RegisterAccountAsync(account, CancellationToken.None);

        CalendarAccount? removedAccount = null;
        manager.AccountRemoved += (_, a) => removedAccount = a;

        await manager.RemoveAccountAsync("remove_test", CancellationToken.None);

        IReadOnlyList<CalendarAccount> accounts = await manager.GetAccountsAsync();
        accounts.Should().BeEmpty();
        removedAccount.Should().NotBeNull();
        removedAccount!.Id.Should().Be("remove_test");
    }

    [Fact]
    public async Task RemoveAccountAsync_CallsDisconnectOnClient()
    {
        FakeCalendarAccountClient? capturedClient = null;
        FakeCalendarProvider provider = new(CalendarProviderType.Outlook, (account, _) =>
        {
            capturedClient = new FakeCalendarAccountClient(account);
            return capturedClient;
        });

        using CalendarAccountManager manager = CreateManager(
            new Dictionary<CalendarProviderType, ICalendarProvider>
            {
                [CalendarProviderType.Outlook] = provider,
            });

        CalendarAccount account = CreateAccount("disconnect_test");
        await manager.RegisterAccountAsync(account, CancellationToken.None);

        await manager.RemoveAccountAsync("disconnect_test", CancellationToken.None);

        capturedClient.Should().NotBeNull();
        capturedClient!.WasDisconnected.Should().BeTrue();
        capturedClient.WasDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveAccountAsync_DeletesFromStore()
    {
        using CalendarAccountManager manager = CreateManager();
        CalendarAccount account = CreateAccount("delete_store");
        await manager.RegisterAccountAsync(account, CancellationToken.None);

        await manager.RemoveAccountAsync("delete_store", CancellationToken.None);

        CalendarAccount[] stored = await _dataStore.LoadAllAsync();
        stored.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveAccountAsync_NonExistentAccount_IsNoOp()
    {
        using CalendarAccountManager manager = CreateManager();

        Func<Task> act = () => manager.RemoveAccountAsync("nonexistent", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region GetClientForAccount

    [Fact]
    public async Task GetClientForAccount_RegisteredAccount_ReturnsClient()
    {
        using CalendarAccountManager manager = CreateManager();
        CalendarAccount account = CreateAccount("client_test");
        await manager.RegisterAccountAsync(account, CancellationToken.None);

        CalendarAccountClientDecorator client = manager.GetClientForAccount("client_test");

        client.Should().NotBeNull();
        client.Account.Id.Should().Be("client_test");
    }

    [Fact]
    public void GetClientForAccount_UnknownAccount_Throws()
    {
        using CalendarAccountManager manager = CreateManager();

        Action act = () => manager.GetClientForAccount("unknown");

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetClientForAccount_PersistedAccount_LazilyCreatesClient()
    {
        // Pre-persist an account.
        CalendarAccount account = CreateAccount("lazy_client");
        await _dataStore.SaveAsync(account);

        using CalendarAccountManager manager = CreateManager();
        // Wait for initialization to complete.
        await manager.GetAccountsAsync();

        CalendarAccountClientDecorator client = manager.GetClientForAccount("lazy_client");

        client.Should().NotBeNull();
        client.Account.Id.Should().Be("lazy_client");
    }

    #endregion

    #region GetUpcomingEventsAsync

    [Fact]
    public async Task GetUpcomingEventsAsync_NoAccounts_ReturnsEmpty()
    {
        using CalendarAccountManager manager = CreateManager();

        IReadOnlyList<CalendarEvent> events = await manager.GetUpcomingEventsAsync(
            TimeSpan.FromHours(24), CancellationToken.None);

        events.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUpcomingEventsAsync_MergesEventsFromMultipleAccounts()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        CalendarEvent event1 = CreateEvent("e1", "Meeting A", now.AddHours(1), now.AddHours(2), "acc_1");
        CalendarEvent event2 = CreateEvent("e2", "Meeting B", now.AddHours(3), now.AddHours(4), "acc_2");

        FakeCalendarProvider provider = new(CalendarProviderType.Outlook, (account, _) =>
            new FakeCalendarAccountClient(account, (_, _, _) =>
                Task.FromResult<IReadOnlyList<CalendarEvent>>(
                    account.Id == "acc_1" ? [event1] : [event2])));

        using CalendarAccountManager manager = CreateManager(
            new Dictionary<CalendarProviderType, ICalendarProvider>
            {
                [CalendarProviderType.Outlook] = provider,
            });

        await manager.RegisterAccountAsync(CreateAccount("acc_1"), CancellationToken.None);
        await manager.RegisterAccountAsync(CreateAccount("acc_2"), CancellationToken.None);

        IReadOnlyList<CalendarEvent> events = await manager.GetUpcomingEventsAsync(
            TimeSpan.FromHours(24), CancellationToken.None);

        events.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUpcomingEventsAsync_SortsByStartTime()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        CalendarEvent laterEvent = CreateEvent("e1", "Later", now.AddHours(3), now.AddHours(4), "acc_1");
        CalendarEvent earlierEvent = CreateEvent("e2", "Earlier", now.AddHours(1), now.AddHours(2), "acc_1");

        FakeCalendarProvider provider = new(CalendarProviderType.Outlook, (account, _) =>
            new FakeCalendarAccountClient(account, (_, _, _) =>
                Task.FromResult<IReadOnlyList<CalendarEvent>>([laterEvent, earlierEvent])));

        using CalendarAccountManager manager = CreateManager(
            new Dictionary<CalendarProviderType, ICalendarProvider>
            {
                [CalendarProviderType.Outlook] = provider,
            });

        await manager.RegisterAccountAsync(CreateAccount("acc_1"), CancellationToken.None);

        IReadOnlyList<CalendarEvent> events = await manager.GetUpcomingEventsAsync(
            TimeSpan.FromHours(24), CancellationToken.None);

        events.Should().HaveCount(2);
        events[0].Title.Should().Be("Earlier");
        events[1].Title.Should().Be("Later");
    }

    [Fact]
    public async Task GetUpcomingEventsAsync_DeduplicatesMatchingEvents()
    {
        DateTimeOffset start = DateTimeOffset.Now.AddHours(1);
        DateTimeOffset end = start.AddHours(1);
        string organizer = "org@test.com";

        CalendarEvent dup1 = CreateEvent("e1", "Team Sync", start, end, "acc_1", organizer);
        CalendarEvent dup2 = CreateEvent("e2", "Team Sync", start, end, "acc_2", organizer);

        FakeCalendarProvider provider = new(CalendarProviderType.Outlook, (account, _) =>
            new FakeCalendarAccountClient(account, (_, _, _) =>
                Task.FromResult<IReadOnlyList<CalendarEvent>>(
                    account.Id == "acc_1" ? [dup1] : [dup2])));

        using CalendarAccountManager manager = CreateManager(
            new Dictionary<CalendarProviderType, ICalendarProvider>
            {
                [CalendarProviderType.Outlook] = provider,
            });

        await manager.RegisterAccountAsync(CreateAccount("acc_1"), CancellationToken.None);
        await manager.RegisterAccountAsync(CreateAccount("acc_2"), CancellationToken.None);

        IReadOnlyList<CalendarEvent> events = await manager.GetUpcomingEventsAsync(
            TimeSpan.FromHours(24), CancellationToken.None);

        events.Should().HaveCount(1);
        events[0].Title.Should().Be("Team Sync");
    }

    [Fact]
    public async Task GetUpcomingEventsAsync_SwallowsClientExceptions()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        CalendarEvent goodEvent = CreateEvent("e1", "Good", now.AddHours(1), now.AddHours(2), "good");

        FakeCalendarProvider provider = new(CalendarProviderType.Outlook, (account, _) =>
            new FakeCalendarAccountClient(account, (_, _, _) =>
            {
                if (account.Id == "bad")
                {
                    throw new InvalidOperationException("API failure");
                }

                return Task.FromResult<IReadOnlyList<CalendarEvent>>([goodEvent]);
            }));

        using CalendarAccountManager manager = CreateManager(
            new Dictionary<CalendarProviderType, ICalendarProvider>
            {
                [CalendarProviderType.Outlook] = provider,
            });

        await manager.RegisterAccountAsync(CreateAccount("good"), CancellationToken.None);
        await manager.RegisterAccountAsync(CreateAccount("bad"), CancellationToken.None);

        IReadOnlyList<CalendarEvent> events = await manager.GetUpcomingEventsAsync(
            TimeSpan.FromHours(24), CancellationToken.None);

        events.Should().HaveCount(1);
        events[0].Title.Should().Be("Good");
    }

    #endregion

    #region CreateConnectExperience

    [Fact]
    public void CreateConnectExperience_UnknownProvider_Throws()
    {
        using CalendarAccountManager manager = CreateManager(
            new Dictionary<CalendarProviderType, ICalendarProvider>());

        Action act = () => manager.CreateConnectExperience(CalendarProviderType.Outlook);

        act.Should().Throw<NotSupportedException>();
    }

    #endregion

    #region Dispose

    [Fact]
    public async Task Dispose_DisposesClients()
    {
        FakeCalendarAccountClient? capturedClient = null;
        FakeCalendarProvider provider = new(CalendarProviderType.Outlook, (account, _) =>
        {
            capturedClient = new FakeCalendarAccountClient(account);
            return capturedClient;
        });

        CalendarAccountManager manager = CreateManager(
            new Dictionary<CalendarProviderType, ICalendarProvider>
            {
                [CalendarProviderType.Outlook] = provider,
            });

        await manager.RegisterAccountAsync(CreateAccount("dispose_test"), CancellationToken.None);
        manager.Dispose();

        capturedClient.Should().NotBeNull();
        capturedClient!.WasDisposed.Should().BeTrue();
    }

    #endregion

    #region AuthData Persistence Callback

    [Fact]
    public async Task AuthDataCallback_UpdatesPersistedData()
    {
        Func<IReadOnlyDictionary<string, string>, CancellationToken, Task>? capturedCallback = null;
        FakeCalendarProvider provider = new(CalendarProviderType.Outlook, (account, onAuthChanged) =>
        {
            capturedCallback = onAuthChanged;
            return new FakeCalendarAccountClient(account);
        });

        using CalendarAccountManager manager = CreateManager(
            new Dictionary<CalendarProviderType, ICalendarProvider>
            {
                [CalendarProviderType.Outlook] = provider,
            });

        await manager.RegisterAccountAsync(CreateAccount("auth_test"), CancellationToken.None);
        capturedCallback.Should().NotBeNull();

        // Simulate provider reporting updated auth data.
        await capturedCallback!(new Dictionary<string, string> { ["token"] = "refreshed" }, CancellationToken.None);

        // Verify in-memory account is updated.
        IReadOnlyList<CalendarAccount> accounts = await manager.GetAccountsAsync();
        accounts[0].AuthData.Should().ContainKey("token").WhoseValue.Should().Be("refreshed");

        // Verify persisted to store.
        CalendarAccount[] stored = await _dataStore.LoadAllAsync();
        stored[0].AuthData.Should().ContainKey("token").WhoseValue.Should().Be("refreshed");
    }

    [Fact]
    public async Task AuthDataCallback_AfterRemoval_IsNoOp()
    {
        Func<IReadOnlyDictionary<string, string>, CancellationToken, Task>? capturedCallback = null;
        FakeCalendarProvider provider = new(CalendarProviderType.Outlook, (account, onAuthChanged) =>
        {
            capturedCallback = onAuthChanged;
            return new FakeCalendarAccountClient(account);
        });

        using CalendarAccountManager manager = CreateManager(
            new Dictionary<CalendarProviderType, ICalendarProvider>
            {
                [CalendarProviderType.Outlook] = provider,
            });

        await manager.RegisterAccountAsync(CreateAccount("removed_auth"), CancellationToken.None);
        await manager.RemoveAccountAsync("removed_auth", CancellationToken.None);

        // Callback after removal should not throw or persist.
        Func<Task> act = () => capturedCallback!(
            new Dictionary<string, string> { ["stale"] = "data" }, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Providers property

    [Fact]
    public void Providers_ReturnsRegisteredProviders()
    {
        using CalendarAccountManager manager = CreateManager();

        manager.Providers.Should().HaveCount(1);
        manager.Providers[0].ProviderType.Should().Be(CalendarProviderType.Outlook);
    }

    #endregion

    #region Helpers

    private CalendarAccountManager CreateManager() =>
        CreateManager(new Dictionary<CalendarProviderType, ICalendarProvider>
        {
            [CalendarProviderType.Outlook] = new FakeCalendarProvider(CalendarProviderType.Outlook),
        });

    private CalendarAccountManager CreateManager(
        Dictionary<CalendarProviderType, ICalendarProvider> providers) =>
        new(_dataStore, providers);

    private static CalendarAccount CreateAccount(string id, CalendarProviderType providerType = CalendarProviderType.Outlook)
    {
        return new CalendarAccount
        {
            Id = id,
            DisplayName = $"Test {id}",
            Email = $"{id}@test.com",
            ProviderType = providerType,
        };
    }

    private static CalendarEvent CreateEvent(
        string id, string title, DateTimeOffset start, DateTimeOffset end,
        string accountId, string? organizerEmail = null)
    {
        return new CalendarEvent
        {
            Id = id,
            CalendarId = "cal_1",
            AccountId = accountId,
            Title = title,
            StartTime = start,
            EndTime = end,
            ProviderType = CalendarProviderType.Outlook,
            Organizer = organizerEmail is not null
                ? new CalendarEventAttendee(null, organizerEmail, AttendeeResponseStatus.Accepted, true)
                : null,
        };
    }

    #endregion
}
