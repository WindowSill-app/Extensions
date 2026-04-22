using System.Collections.ObjectModel;
using System.Globalization;

using Microsoft.UI.Dispatching;

using WindowSill.API;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Services;
using WindowSill.Date.Settings;
using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

/// <summary>
/// Per-DateSill-instance adapter that creates sill items for pinned world clocks
/// and keeps them in sync with <see cref="WorldClockService"/>. Owns a 1-second
/// timer to update displayed times.
/// </summary>
internal sealed partial class WorldClockViewListAdapter : IDisposable
{
    private readonly WorldClockService _worldClockService;
    private readonly ISettingsProvider _settingsProvider;
    private readonly ObservableCollection<SillListViewItem> _viewList;

    private readonly Dictionary<string, ViewListEntry> _entries = [];
    private DispatcherQueueTimer? _timer;
    private Action? _requestReorder;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldClockViewListAdapter"/> class.
    /// </summary>
    /// <param name="worldClockService">The world clock service.</param>
    /// <param name="settingsProvider">The settings provider for time format.</param>
    /// <param name="viewList">This DateSill instance's ViewList.</param>
    internal WorldClockViewListAdapter(
        WorldClockService worldClockService,
        ISettingsProvider settingsProvider,
        ObservableCollection<SillListViewItem> viewList)
    {
        _worldClockService = worldClockService;
        _settingsProvider = settingsProvider;
        _viewList = viewList;

        _worldClockService.EntriesChanged += OnEntriesChanged;
    }

    /// <summary>
    /// Sets the callback invoked after items are added or removed to reorder the ViewList.
    /// </summary>
    internal Action? RequestReorder { set => _requestReorder = value; }

    /// <summary>
    /// Gets the current world clock sill entries for ordering purposes.
    /// </summary>
    internal IReadOnlyCollection<ViewListEntry> GetEntries() => _entries.Values;

    /// <summary>
    /// Performs the initial sync and starts the update timer.
    /// </summary>
    /// <param name="dispatcherQueue">The UI thread dispatcher queue.</param>
    internal void Start(DispatcherQueue dispatcherQueue)
    {
        SyncPinnedEntries();

        _timer = dispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _worldClockService.EntriesChanged -= OnEntriesChanged;

        _timer?.Stop();
        if (_timer is not null)
        {
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }

        foreach (ViewListEntry entry in _entries.Values)
        {
            _viewList.Remove(entry.SillItem);
        }

        _entries.Clear();
    }

    private void OnEntriesChanged(object? sender, EventArgs e)
    {
        if (!_disposed)
        {
            SyncPinnedEntries();
        }
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        string timeFormat = GetTimeFormatString();
        foreach (ViewListEntry entry in _entries.Values)
        {
            entry.Vm.Update(timeFormat);
        }
    }

    /// <summary>
    /// Diffs the current pinned entries against our tracked sill items.
    /// Adds new ones, removes stale ones.
    /// </summary>
    private void SyncPinnedEntries()
    {
        IReadOnlyList<WorldClockEntry> pinned = _worldClockService.GetPinnedEntries();
        HashSet<string> pinnedIds = pinned.Select(e => e.TimeZoneId).ToHashSet();

        // Remove entries that are no longer pinned.
        List<string> toRemove = _entries.Keys.Where(k => !pinnedIds.Contains(k)).ToList();
        foreach (string id in toRemove)
        {
            if (_entries.Remove(id, out ViewListEntry? entry))
            {
                _viewList.Remove(entry.SillItem);
            }
        }

        // Add new pinned entries and refresh existing ones.
        string timeFormat = GetTimeFormatString();
        foreach (WorldClockEntry wcEntry in pinned)
        {
            if (_entries.TryGetValue(wcEntry.TimeZoneId, out ViewListEntry? existing))
            {
                existing.Vm.RefreshDisplayName();
                continue;
            }

            NodaTime.DateTimeZone zone = _worldClockService.GetTimeZone(wcEntry.TimeZoneId);
            var vm = new WorldClockSillItemViewModel(wcEntry, zone);
            vm.Update(timeFormat);

            WorldClockBarContent barContent = WorldClockBarContent.Create(vm);
            var previewFlyout = new WorldClockPreviewFlyout(vm);

            var sillItem = new SillListViewPopupItem
            {
                Content = barContent,
                PreviewFlyoutContent = previewFlyout,
            };

            sillItem.IsSillOrientationOrSizeChanged += (_, _) =>
            {
                barContent.ApplyOrientationState(sillItem.SillOrientationAndSize);
            };
            barContent.ApplyOrientationState(sillItem.SillOrientationAndSize);

            _entries[wcEntry.TimeZoneId] = new ViewListEntry(vm, sillItem);

            if (!_viewList.Contains(sillItem))
            {
                _viewList.Add(sillItem);
            }
        }

        _requestReorder?.Invoke();
    }

    /// <summary>
    /// Gets the user's preferred time format string.
    /// </summary>
    private string GetTimeFormatString()
    {
        Settings.TimeFormat userFormat = _settingsProvider.GetSetting(Settings.Settings.TimeFormat);
        return userFormat == Settings.TimeFormat.None
            ? CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern
            : userFormat.ToFormatString(showSeconds: false);
    }
}
