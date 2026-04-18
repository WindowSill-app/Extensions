using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using Microsoft.Extensions.Logging;
using WindowSill.API;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Providers.CalDav;
using WindowSill.Date.Core.Providers.Google;
using WindowSill.Date.Core.Providers.ICloud;
using WindowSill.Date.Core.Providers.Outlook;

namespace WindowSill.Date.Core;

/// <summary>
/// Manages connected calendar accounts across all providers. Owns the persistence
/// lifecycle — providers never access storage directly.
/// </summary>
[Export]
internal sealed class CalendarAccountManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly IReadOnlyDictionary<CalendarProviderType, ICalendarProvider> _providers;
    private readonly CalendarDataStore _dataStore;
    private readonly ConcurrentDictionary<string, AccountEntry> _entries = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Lazy<Task> _initializationTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarAccountManager"/> class.
    /// </summary>
    /// <param name="pluginInfo">Plugin info for accessing the data folder.</param>
    [ImportingConstructor]
    public CalendarAccountManager(IPluginInfo pluginInfo)
        : this(
            new CalendarDataStore(pluginInfo.GetPluginDataFolder()),
            new Dictionary<CalendarProviderType, ICalendarProvider>
            {
                [CalendarProviderType.Outlook] = new OutlookCalendarProvider(),
                [CalendarProviderType.Google] = new GoogleCalendarProvider(),
                [CalendarProviderType.ICloud] = new ICloudCalendarProvider(),
                [CalendarProviderType.CalDav] = new CalDavCalendarProvider(),
            })
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarAccountManager"/> class
    /// with explicit dependencies, for testability.
    /// </summary>
    /// <param name="dataStore">The data store for persisting accounts.</param>
    /// <param name="providers">The registered calendar providers.</param>
    internal CalendarAccountManager(
        CalendarDataStore dataStore,
        IReadOnlyDictionary<CalendarProviderType, ICalendarProvider> providers)
    {
        _logger = this.Log();
        _dataStore = dataStore;
        _providers = providers;
        _initializationTask = new Lazy<Task>(() => LoadAccountsAsync(_cancellationTokenSource.Token));
    }

    /// <summary>
    /// Gets the registered calendar providers in display order.
    /// </summary>
    public IReadOnlyList<ICalendarProvider> Providers => [.. _providers.Values];

    /// <summary>
    /// Raised when a new account is successfully connected.
    /// </summary>
    public event EventHandler<CalendarAccount>? AccountAdded;

    /// <summary>
    /// Raised when an account is disconnected and removed.
    /// </summary>
    public event EventHandler<CalendarAccount>? AccountRemoved;

    /// <summary>
    /// Gets all currently connected accounts.
    /// </summary>
    /// <returns>A read-only list of connected calendar accounts.</returns>
    public async Task<IReadOnlyList<CalendarAccount>> GetAccountsAsync()
    {
        await _initializationTask.Value;
        return _entries.Values.Select(e => e.Account).ToList();
    }

    /// <summary>
    /// Creates a <see cref="ConnectExperience"/> for adding a new account of the specified provider type.
    /// The caller is responsible for showing the experience in a dialog and calling
    /// <see cref="RegisterAccountAsync"/> with the resulting account.
    /// </summary>
    /// <param name="providerType">The type of calendar provider to connect.</param>
    /// <returns>A connect experience that drives the authentication flow.</returns>
    public ConnectExperience CreateConnectExperience(CalendarProviderType providerType)
    {
        if (!_providers.TryGetValue(providerType, out ICalendarProvider? provider))
        {
            throw new NotSupportedException($"No provider registered for {providerType}.");
        }

        return provider.CreateConnectExperience();
    }

    /// <summary>
    /// Registers a newly connected account, persists it, and creates its client.
    /// Call this after a <see cref="ConnectExperience"/> completes successfully.
    /// </summary>
    /// <param name="account">The account returned by the connect experience.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RegisterAccountAsync(CalendarAccount account, CancellationToken cancellationToken)
    {
        if (!_providers.TryGetValue(account.ProviderType, out ICalendarProvider? provider))
        {
            throw new NotSupportedException($"No provider registered for {account.ProviderType}.");
        }

        CalendarAccountClientDecorator client = new(
            provider.CreateClient(account, CreatePersistCallback(account.Id)));

        await _initializationTask.Value;
        _entries[account.Id] = new AccountEntry(account, client);

        await _dataStore.SaveAsync(account, cancellationToken);
        AccountAdded?.Invoke(this, account);
    }

    /// <summary>
    /// Removes a connected account and cleans up associated resources.
    /// </summary>
    /// <param name="accountId">The identifier of the account to remove.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RemoveAccountAsync(string accountId, CancellationToken cancellationToken)
    {
        if (_entries.TryRemove(accountId, out AccountEntry? entry))
        {
            if (entry.Client is not null)
            {
                await entry.Client.Client.DisconnectAsync(cancellationToken);
                await entry.Client.DisposeAsync();
            }

            await _dataStore.DeleteAsync(accountId, cancellationToken);
            AccountRemoved?.Invoke(this, entry.Account);
        }
    }

    /// <summary>
    /// Gets the decorated client for a specific connected account.
    /// </summary>
    /// <param name="accountId">The identifier of the account.</param>
    /// <returns>The decorated client scoped to the specified account.</returns>
    public CalendarAccountClientDecorator GetClientForAccount(string accountId)
    {
        if (!_entries.TryGetValue(accountId, out AccountEntry? entry))
        {
            throw new KeyNotFoundException($"No account found with ID '{accountId}'.");
        }

        if (entry.Client is not null)
        {
            return entry.Client;
        }

        // Lazily create a client from persisted data.
        if (_providers.TryGetValue(entry.Account.ProviderType, out ICalendarProvider? provider))
        {
            CalendarAccountClientDecorator client = new(
                provider.CreateClient(entry.Account, CreatePersistCallback(accountId)));
            _entries[accountId] = entry with { Client = client };
            return client;
        }

        throw new NotSupportedException($"No provider registered for {entry.Account.ProviderType}.");
    }

    /// <summary>
    /// Retrieves upcoming events across all connected accounts within the specified look-ahead window.
    /// Events are sorted by start time and deduplicated across calendars.
    /// </summary>
    /// <param name="lookAhead">How far ahead to look for events.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A sorted, deduplicated list of upcoming events.</returns>
    public async Task<IReadOnlyList<CalendarEvent>> GetUpcomingEventsAsync(
        TimeSpan lookAhead,
        CancellationToken cancellationToken)
    {
        await _initializationTask.Value;

        DateTimeOffset now = DateTimeOffset.Now;
        DateTimeOffset until = now.Add(lookAhead);

        Task<IReadOnlyList<CalendarEvent>>[] tasks = _entries.Keys
            .Select(async accountId =>
            {
                try
                {
                    CalendarAccountClientDecorator client = GetClientForAccount(accountId);
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
        _cancellationTokenSource.Cancel();

        _initializationTask.Value.GetAwaiter().GetResult();

        foreach (AccountEntry entry in _entries.Values)
        {
            entry.Client?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        _entries.Clear();

        _cancellationTokenSource.Dispose();
    }

    /// <summary>
    /// Loads previously saved accounts from encrypted storage. Call once on
    /// app startup.
    /// </summary>
    private async Task LoadAccountsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(async () =>
        {
            try
            {
                CalendarAccount[] accounts = await _dataStore.LoadAllAsync(cancellationToken);
                foreach (CalendarAccount account in accounts)
                {
                    _entries[account.Id] = new AccountEntry(account);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load calendar accounts from storage. Starting with no accounts.");
            }
        });
    }

    /// <summary>
    /// Creates a callback that providers call when their auth data changes.
    /// Becomes a no-op if the account has been removed.
    /// </summary>
    private Func<IReadOnlyDictionary<string, string>, CancellationToken, Task> CreatePersistCallback(string accountId)
    {
        return async (updatedAuthData, cancellationToken) =>
        {
            if (!_entries.TryGetValue(accountId, out AccountEntry? entry))
            {
                return;
            }

            CalendarAccount updated = entry.Account.WithAuthData(new Dictionary<string, string>(updatedAuthData));
            _entries[accountId] = entry with { Account = updated };

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

    /// <summary>
    /// Tracks a connected account and its lazily-created client.
    /// </summary>
    private sealed record AccountEntry(CalendarAccount Account, CalendarAccountClientDecorator? Client = null);
}
