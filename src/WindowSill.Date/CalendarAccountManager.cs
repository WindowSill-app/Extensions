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
/// Manages connected calendar accounts across all providers and provides
/// aggregated access to calendar events. Persists account information and
/// auth tokens to DPAPI-encrypted files in the plugin data folder.
/// </summary>
[Export(typeof(ICalendarAccountManager))]
internal sealed class CalendarAccountManager : ICalendarAccountManager, IDisposable
{
    private readonly IReadOnlyDictionary<CalendarProviderType, ICalendarProvider> _providers;
    private readonly CalendarDataStore _dataStore;
    private readonly ConcurrentDictionary<string, CalendarAccount> _accounts = new();
    private readonly ConcurrentDictionary<string, ICalendarAccountClient> _clients = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarAccountManager"/> class.
    /// </summary>
    /// <param name="pluginInfo">Plugin info for accessing the data folder.</param>
    [ImportingConstructor]
    public CalendarAccountManager(IPluginInfo pluginInfo)
    {
        _dataStore = new CalendarDataStore(pluginInfo.GetPluginDataFolder());

        // Build providers, injecting the shared data store for both
        // provider caches and per-account credentials.
        _providers = new Dictionary<CalendarProviderType, ICalendarProvider>
        {
            [CalendarProviderType.Outlook] = new OutlookCalendarProvider(_dataStore),
            [CalendarProviderType.Google] = new GoogleCalendarProvider(_dataStore),
            [CalendarProviderType.CalDav] = new CalDavCalendarProvider(_dataStore),
            [CalendarProviderType.ICloud] = new ICloudCalendarProvider(_dataStore),
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
    /// Loads previously saved accounts from encrypted storage. Call this once
    /// during app startup to restore accounts from a prior session. This does
    /// not trigger <see cref="AccountAdded"/> events — it silently re-hydrates
    /// the in-memory state.
    /// </summary>
    public async Task LoadAccountsAsync(CancellationToken cancellationToken = default)
    {
        CalendarAccount[] accounts = await _dataStore.LoadAccountsAsync(cancellationToken);
        foreach (CalendarAccount account in accounts)
        {
            _accounts[account.Id] = account;
        }
    }

    /// <inheritdoc />
    public async Task<CalendarAccount> AddAccountAsync(CalendarProviderType providerType, CancellationToken cancellationToken)
    {
        if (!_providers.TryGetValue(providerType, out ICalendarProvider? provider))
        {
            throw new NotSupportedException($"No provider registered for {providerType}.");
        }

        CalendarAccount account = await provider.ConnectAccountAsync(cancellationToken);

        _accounts[account.Id] = account;
        _clients[account.Id] = provider.CreateClient(account);

        PersistAccountAsync(account).ConfigureAwait(false);
        AccountAdded?.Invoke(this, account);
        return account;
    }

    /// <inheritdoc />
    public async Task RemoveAccountAsync(string accountId, CancellationToken cancellationToken)
    {
        if (_clients.TryRemove(accountId, out ICalendarAccountClient? client))
        {
            await client.DisconnectAsync(cancellationToken);
            await client.DisposeAsync();
        }

        if (_accounts.TryRemove(accountId, out CalendarAccount? account))
        {
            _dataStore.DeleteAccount(accountId);
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

        // Lazily create a client from the stored account.
        if (_accounts.TryGetValue(accountId, out CalendarAccount? account)
            && _providers.TryGetValue(account.ProviderType, out ICalendarProvider? provider))
        {
            client = provider.CreateClient(account);
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

        // Fetch events from all accounts in parallel.
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
    }

    /// <summary>
    /// Persists a single account to its encrypted file.
    /// </summary>
    private async Task PersistAccountAsync(CalendarAccount account)
    {
        await _dataStore.SaveAccountAsync(account);
    }

    /// <summary>
    /// Deduplicates events that appear in multiple calendars (same title, time, and organizer)
    /// and sorts by start time.
    /// </summary>
    private static List<CalendarEvent> DeduplicateAndSort(IEnumerable<CalendarEvent> events)
    {
        return events
            .GroupBy(e => (e.Title, e.StartTime, e.EndTime, e.Organizer?.Email))
            .Select(g => g.First())
            .OrderBy(e => e.StartTime)
            .ToList();
    }
}
