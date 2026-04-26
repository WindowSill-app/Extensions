using System.Collections.ObjectModel;

using WindowSill.API;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Services;
using WindowSill.Date.Core.UI;
using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

/// <summary>
/// Per-DateSill-instance adapter that creates unique UI elements for each meeting
/// and manages their lifecycle in the ViewList. Subscribes to the singleton
/// <see cref="MeetingStateService"/> for state changes.
/// Does NOT own timers, VMs, hidden state, or notification logic.
/// </summary>
internal sealed class MeetingViewListAdapter : SillViewListAdapterBase
{
    private readonly MeetingStateService _stateService;
    private readonly WorldClockService _worldClockService;

    /// <summary>
    /// Maps MeetingKey → per-instance UI elements.
    /// VMs are shared references, NOT owned here.
    /// </summary>
    private readonly Dictionary<MeetingKey, ViewListEntry> _entries = [];

    private Func<SillListViewItem, int>? _resolveInsertIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeetingViewListAdapter"/> class.
    /// </summary>
    internal MeetingViewListAdapter(
        MeetingStateService stateService,
        WorldClockService worldClockService,
        ISettingsProvider settingsProvider,
        ObservableCollection<SillListViewItem> viewList)
        : base(settingsProvider, viewList)
    {
        _stateService = stateService;
        _worldClockService = worldClockService;

        _stateService.MeetingsChanged += OnMeetingsChanged;
    }

    /// <summary>
    /// Sets the callback that decides where a new meeting sill should be inserted into the
    /// <see cref="ViewList"/>. The callback receives the new item and returns the target index.
    /// Returning a value outside of <c>[0, ViewList.Count]</c> falls back to appending.
    /// </summary>
    internal Func<SillListViewItem, int>? ResolveInsertIndex { set => _resolveInsertIndex = value; }

    /// <summary>
    /// Gets the sill items owned by this adapter for ordering purposes.
    /// </summary>
    internal IReadOnlyCollection<SillListViewItem> GetSillItems()
        => _entries.Values.Select(e => (SillListViewItem)e.SillItem).ToList();

    /// <summary>
    /// Performs the initial sync with whatever meetings already exist.
    /// </summary>
    internal void Start()
    {
        OnMeetingsChanged();
    }

    /// <inheritdoc/>
    protected override IEnumerable<Control> GetBarContents()
        => _entries.Values.Select(e => (Control)e.BarContent);

    /// <inheritdoc/>
    protected override void OnDisposing()
    {
        _stateService.MeetingsChanged -= OnMeetingsChanged;

        foreach (ViewListEntry entry in _entries.Values)
        {
            UnsubscribeEntry(entry);
            ViewList.Remove(entry.SillItem);
        }

        _entries.Clear();
    }

    /// <summary>
    /// Called when the singleton's meeting list changes.
    /// Diffs the canonical list against our local UI entries and adds/removes as needed.
    /// Runs on the UI thread (MeetingsChanged is raised from the DispatcherQueueTimer).
    /// </summary>
    private void OnMeetingsChanged()
    {
        if (Disposed)
        {
            return;
        }

        IReadOnlyList<MeetingSillItemViewModel> activeMeetings = _stateService.GetActiveMeetings();

        var activeKeys = activeMeetings
            .Select(vm => vm.Key)
            .ToHashSet();

        // Remove stale entries (meeting ended, hidden, or no longer active).
        var toRemove = _entries.Keys
            .Where(k => !activeKeys.Contains(k))
            .ToList();

        foreach (MeetingKey key in toRemove)
        {
            if (_entries.Remove(key, out ViewListEntry? entry))
            {
                UnsubscribeEntry(entry);
                ViewList.Remove(entry.SillItem);
            }
        }

        // Add new entries (meetings in state that we don't have UI elements for yet).
        bool enableFlashing = SettingsProvider.GetSetting(Settings.Settings.EnableSillFlashing);

        foreach (MeetingSillItemViewModel vm in activeMeetings)
        {
            if (_entries.ContainsKey(vm.Key))
            {
                continue;
            }

            // Create per-instance UI elements bound to the SHARED VM.
            var barContent = MeetingBarContent.Create(vm);
            var previewFlyout = new MeetingPreviewFlyout(vm);

            // Build the flyout lazily on each open so it always reflects current state
            // (travel time arrives asynchronously, countdown text changes every second).
            MeetingKey capturedKey = vm.Key;
            var menuFlyout = new MenuFlyout();
            menuFlyout.Opening += (_, _) =>
            {
                menuFlyout.Items.Clear();
                MeetingFlyoutBuilder.PopulateItems(
                    menuFlyout,
                    vm,
                    _worldClockService,
                    SettingsProvider,
                    onHide: () => _stateService.HideMeeting(capturedKey));
            };

            SillListViewMenuFlyoutItem sillItem
                = new SillListViewMenuFlyoutItem(
                    barContent,
                    null,
                    menuFlyout)
                .PreviewFlyoutContent(previewFlyout);

            barContent.ApplyOrientationState(ComputeCurrentOrientation());

            // Wire flashing: shared VM event → per-instance sill item.
            // Track the handler so we can unsubscribe on dispose.
            Action? flashHandler = null;
            if (enableFlashing)
            {
                flashHandler = () => sillItem.StartFlashing();
                vm.FlashRequested += flashHandler;
            }

            // Resolve the correct insertion index BEFORE registering the entry, so the
            // resolver sees only the items that already live in ViewList.
            int insertIndex = ClampInsertIndex(_resolveInsertIndex?.Invoke(sillItem) ?? ViewList.Count);

            _entries[vm.Key] = new ViewListEntry(vm, sillItem, barContent, flashHandler);

            if (!ViewList.Contains(sillItem))
            {
                ViewList.Insert(insertIndex, sillItem);
            }
        }
    }

    /// <summary>
    /// Unsubscribes event handlers from a shared VM to prevent leaks.
    /// </summary>
    private static void UnsubscribeEntry(ViewListEntry entry)
    {
        if (entry.FlashHandler is not null)
        {
            entry.Vm.FlashRequested -= entry.FlashHandler;
        }
    }

    /// <summary>
    /// Tracks the per-instance sill item, bar content, and event handlers for cleanup.
    /// </summary>
    private sealed record ViewListEntry(
        MeetingSillItemViewModel Vm,
        SillListViewMenuFlyoutItem SillItem,
        MeetingBarContent BarContent,
        Action? FlashHandler);
}
