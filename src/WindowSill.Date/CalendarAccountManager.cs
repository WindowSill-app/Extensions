using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using WindowSill.API;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Settings;

namespace WindowSill.Date;

/// <summary>
/// Manages connected calendar accounts across all providers and provides
/// aggregated access to calendar events. Uses MEF to discover available providers.
/// Persists account information to <see cref="ISettingsProvider"/> so accounts
/// survive app restarts.
/// </summary>
[Export(typeof(ICalendarAccountManager))]
internal sealed class CalendarAccountManager : ICalendarAccountManager, IDisposable
{
    private readonly IReadOnlyDictionary<CalendarProviderType, ICalendarProvider> _providers;
    private readonly ISettingsProvider _settingsProvider;
    private readonly ConcurrentDictionary<string, CalendarAccount> _accounts = new();
    private readonly ConcurrentDictionary<string, ICalendarAccountClient> _clients = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarAccountManager"/> class.
    /// </summary>
    /// <param name="providers">All available calendar providers, discovered via MEF.</param>
    /// <param name="settingsProvider">The settings provider for persisting accounts.</param>
    [ImportingConstructor]
    public CalendarAccountManager(
        [ImportMany] IEnumerable<ICalendarProvider> providers,
        ISettingsProvider settingsProvider)
    {
        _providers = providers.ToDictionary(p => p.ProviderType);
        _settingsProvider = settingsProvider;
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
    /// Loads previously saved accounts from settings. Call this once during
    /// app startup to restore accounts from a prior session. This does not
    /// trigger <see cref="AccountAdded"/> events — it silently re-hydrates
    /// the in-memory state.
    /// </summary>
    public void LoadAccounts()
    {
        AccountRecord[] records = _settingsProvider.GetSetting(Settings.Settings.ConnectedAccounts);
        if (records is null or { Length: 0 })
        {
            return;
        }

        foreach (AccountRecord record in records)
        {
            CalendarAccount account = record.ToCalendarAccount();
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

        PersistAccounts();
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
            PersistAccounts();
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
    /// Persists the current account list to settings.
    /// </summary>
    private void PersistAccounts()
    {
        AccountRecord[] records = _accounts.Values
            .Select(AccountRecord.FromCalendarAccount)
            .ToArray();
        _settingsProvider.SetSetting(Settings.Settings.ConnectedAccounts, records);
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
