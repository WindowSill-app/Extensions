using System.Collections.ObjectModel;

using Microsoft.UI.Dispatching;

using WindowSill.API;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Services;
using WindowSill.Date.Core.UI;
using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

/// <summary>
/// Per-DateSill-instance adapter that creates sill items for pinned world clocks
/// and keeps them in sync with <see cref="WorldClockService"/>. Owns a 1-second
/// timer to update displayed times.
/// </summary>
internal sealed partial class WorldClockViewListAdapter : SillViewListAdapterBase
{
    private readonly WorldClockService _worldClockService;

    private readonly Dictionary<string, ViewListEntry> _entries = [];
    private DispatcherQueueTimer? _timer;
    private Func<SillListViewItem, WorldClockSillItemViewModel, int>? _resolveInsertIndex;

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
        : base(settingsProvider, viewList)
    {
        _worldClockService = worldClockService;

        _worldClockService.EntriesChanged += OnEntriesChanged;
    }

    /// <summary>
    /// Sets the callback that decides where a new world-clock sill should be inserted into
    /// the <see cref="ViewList"/>. The callback receives the new item and its view-model
    /// (needed for timezone-based placement) and returns the target index. Returning a
    /// value outside of <c>[0, ViewList.Count]</c> falls back to appending.
    /// </summary>
    internal Func<SillListViewItem, WorldClockSillItemViewModel, int>? ResolveInsertIndex
    {
        set => _resolveInsertIndex = value;
    }

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
    protected override IEnumerable<Control> GetBarContents()
        => _entries.Values.Select(e => (Control)e.BarContent);

    /// <inheritdoc/>
    protected override void OnDisposing()
    {
        _worldClockService.EntriesChanged -= OnEntriesChanged;

        _timer?.Stop();
        if (_timer is not null)
        {
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }

        foreach (ViewListEntry entry in _entries.Values)
        {
            ViewList.Remove(entry.SillItem);
        }

        _entries.Clear();
    }

    private void OnEntriesChanged(object? sender, EventArgs e)
    {
        if (!Disposed)
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
        var pinnedIds = pinned.Select(e => e.TimeZoneId).ToHashSet();

        // Remove entries that are no longer pinned.
        var toRemove = _entries.Keys.Where(k => !pinnedIds.Contains(k)).ToList();
        foreach (string id in toRemove)
        {
            if (_entries.Remove(id, out ViewListEntry? entry))
            {
                ViewList.Remove(entry.SillItem);
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

            var barContent = WorldClockBarContent.Create(vm);
            var previewFlyout = new WorldClockPreviewFlyout(vm);

            var sillItem = new SillListViewPopupItem
            {
                Content = barContent,
                PreviewFlyoutContent = previewFlyout,
            };

            barContent.ApplyOrientationState(ComputeCurrentOrientation());

            // Resolve the correct insertion index BEFORE registering the entry, so the
            // resolver sees only the items that already live in ViewList.
            int insertIndex = ClampInsertIndex(
                _resolveInsertIndex?.Invoke(sillItem, vm) ?? ViewList.Count);

            _entries[wcEntry.TimeZoneId] = new ViewListEntry(vm, sillItem, barContent);

            if (!ViewList.Contains(sillItem))
            {
                ViewList.Insert(insertIndex, sillItem);
            }
        }
    }

    private string GetTimeFormatString() => TimeFormatHelper.GetTimeFormatString(SettingsProvider);
}
