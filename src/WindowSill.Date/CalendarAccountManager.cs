using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using WindowSill.API;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Providers.CalDav;
using WindowSill.Date.Providers.Google;
using WindowSill.Date.Providers.ICloud;
using WindowSill.Date.Providers.Outlook;

namespace WindowSill.Date;

/// <summary>
/// Manages connected calendar accounts across all providers. Owns the persistence
/// lifecycle — providers never access storage directly.
/// </summary>
[Export(typeof(ICalendarAccountManager))]
internal sealed class CalendarAccountManager : ICalendarAccountManager, IDisposable
{
    private readonly IReadOnlyDictionary<CalendarProviderType, ICalendarProvider> _providers;
    private readonly CalendarDataStore _dataStore;
    private readonly ConcurrentDictionary<string, CalendarAccount> _accounts = new();
    private readonly ConcurrentDictionary<string, AccountData> _accountData = new();
    private readonly ConcurrentDictionary<string, ICalendarAccountClient> _clients = new();
    private readonly ConcurrentDictionary<string, bool> _deleted = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarAccountManager"/> class.
    /// </summary>
    /// <param name="pluginInfo">Plugin info for accessing the data folder.</param>
    [ImportingConstructor]
    public CalendarAccountManager(IPluginInfo pluginInfo)
    {
        _dataStore = new CalendarDataStore(pluginInfo.GetPluginDataFolder());

        _providers = new Dictionary<CalendarProviderType, ICalendarProvider>
        {
            [CalendarProviderType.Outlook] = new OutlookCalendarProvider(),
            [CalendarProviderType.Google] = new GoogleCalendarProvider(),
            [CalendarProviderType.CalDav] = new CalDavCalendarProvider(),
            [CalendarProviderType.ICloud] = new ICloudCalendarProvider(),
        };
    }

    /// <inheritdoc />
    public event EventHandler<CalendarAccount>? AccountAdded;

    /// <inheritdoc />
    public event EventHandler<CalendarAccount>? AccountRemoved;

    /// <inheritdoc />
    public IReadOnlyList<CalendarAccount> GetAccounts()
    {
        return _accounts.Values.ToList();
    }

    /// <summary>
    /// Loads previously saved accounts from encrypted storage. Call once on
    /// app startup. Does not trigger <see cref="AccountAdded"/> events.
    /// </summary>
    public async Task LoadAccountsAsync(CancellationToken cancellationToken = default)
    {
        AccountData[] allData = await _dataStore.LoadAllAsync(cancellationToken);
        foreach (AccountData data in allData)
        {
            CalendarAccount account = data.ToCalendarAccount();
            _accounts[account.Id] = account;
            _accountData[account.Id] = data;
        }
    }

    /// <inheritdoc />
    public async Task<CalendarAccount> AddAccountAsync(CalendarProviderType providerType, CancellationToken cancellationToken)
    {
        if (!_providers.TryGetValue(providerType, out ICalendarProvider? provider))
        {
            throw new NotSupportedException($"No provider registered for {providerType}.");
        }

        (CalendarAccount account, Dictionary<string, string> authData) = await provider.ConnectAccountAsync(cancellationToken);

        AccountData data = AccountData.FromAccount(account, authData);

        _accounts[account.Id] = account;
        _accountData[account.Id] = data;
        _clients[account.Id] = provider.CreateClient(account, authData, CreatePersistCallback(account.Id));

        await _dataStore.SaveAsync(data, cancellationToken);
        AccountAdded?.Invoke(this, account);
        return account;
    }

    /// <inheritdoc />
    public async Task RemoveAccountAsync(string accountId, CancellationToken cancellationToken)
    {
        // Mark as deleted so the persist callback becomes a no-op.
        _deleted[accountId] = true;

        if (_clients.TryRemove(accountId, out ICalendarAccountClient? client))
        {
            await client.DisconnectAsync(cancellationToken);
            await client.DisposeAsync();
        }

        _accountData.TryRemove(accountId, out _);

        if (_accounts.TryRemove(accountId, out CalendarAccount? account))
        {
            await _dataStore.DeleteAsync(accountId, cancellationToken);
            AccountRemoved?.Invoke(this, account);
        }
    }

    /// <inheritdoc />
    public ICalendarAccountClient GetClientForAccount(string accountId)
    {
        if (_clients.TryGetValue(accountId, out ICalendarAccountClient? client))
        {
            return client;
        }

        // Lazily create a client from persisted data.
        if (_accounts.TryGetValue(accountId, out CalendarAccount? account)
            && _accountData.TryGetValue(accountId, out AccountData? data)
            && _providers.TryGetValue(account.ProviderType, out ICalendarProvider? provider))
        {
            client = provider.CreateClient(account, data.AuthData, CreatePersistCallback(accountId));
            _clients[accountId] = client;
            return client;
        }

        throw new KeyNotFoundException($"No account found with ID '{accountId}'.");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CalendarEvent>> GetUpcomingEventsAsync(
        TimeSpan lookAhead,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.Now;
        DateTimeOffset until = now.Add(lookAhead);

        Task<IReadOnlyList<CalendarEvent>>[] tasks = _accounts.Keys
            .Select(async accountId =>
            {
                try
                {
                    ICalendarAccountClient client = GetClientForAccount(accountId);
                    return await client.GetEventsAsync(now, until, cancellationToken);
                }
                catch (Exception)
                {
                    return (IReadOnlyList<CalendarEvent>)[];
                }
            })
            .ToArray();

        IReadOnlyList<CalendarEvent>[] results = await Task.WhenAll(tasks);

        return DeduplicateAndSort(results.SelectMany(r => r));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (ICalendarAccountClient client in _clients.Values)
        {
            client.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        _clients.Clear();
        _accounts.Clear();
        _accountData.Clear();
    }

    /// <summary>
    /// Creates a callback that providers call when their auth data changes
    /// (e.g., token refresh). The callback updates the in-memory data and
    /// persists to disk. Becomes a no-op if the account has been deleted.
    /// </summary>
    private Func<IReadOnlyDictionary<string, string>, CancellationToken, Task> CreatePersistCallback(string accountId)
    {
        return async (updatedAuthData, cancellationToken) =>
        {
            if (_deleted.ContainsKey(accountId))
            {
                return;
            }

            if (!_accountData.TryGetValue(accountId, out AccountData? existing))
            {
                return;
            }

            // Create updated AccountData with new auth data.
            AccountData updated = AccountData.FromAccount(existing.ToCalendarAccount(), new Dictionary<string, string>(updatedAuthData));
            _accountData[accountId] = updated;

            await _dataStore.SaveAsync(updated, cancellationToken);
        };
    }

    private static List<CalendarEvent> DeduplicateAndSort(IEnumerable<CalendarEvent> events)
    {
        return events
            .GroupBy(e => (e.Title, e.StartTime, e.EndTime, e.Organizer?.Email))
            .Select(g => g.First())
            .OrderBy(e => e.StartTime)
            .ToList();
    }
}
