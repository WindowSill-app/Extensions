using System.Collections.ObjectModel;

using WindowSill.API;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Services;
using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

/// <summary>
/// Per-DateSill-instance adapter that creates unique UI elements for each meeting
/// and manages their lifecycle in the ViewList. Subscribes to the singleton
/// <see cref="MeetingStateService"/> for state changes.
/// Does NOT own timers, VMs, hidden state, or notification logic.
/// </summary>
internal sealed class MeetingViewListAdapter : IDisposable
{
    private readonly MeetingStateService _stateService;
    private readonly WorldClockService _worldClockService;
    private readonly ISettingsProvider _settingsProvider;
    private readonly ObservableCollection<SillListViewItem> _viewList;

    /// <summary>
    /// Maps MeetingKey → per-instance UI elements.
    /// VMs are shared references, NOT owned here.
    /// </summary>
    private readonly Dictionary<MeetingKey, ViewListEntry> _entries = [];

    private Action? _requestReorder;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeetingViewListAdapter"/> class.
    /// </summary>
    internal MeetingViewListAdapter(
        MeetingStateService stateService,
        WorldClockService worldClockService,
        ISettingsProvider settingsProvider,
        ObservableCollection<SillListViewItem> viewList)
    {
        _stateService = stateService;
        _worldClockService = worldClockService;
        _settingsProvider = settingsProvider;
        _viewList = viewList;

        _stateService.MeetingsChanged += OnMeetingsChanged;
    }

    /// <summary>
    /// Sets the callback invoked after items are added or removed to reorder the ViewList.
    /// </summary>
    internal Action? RequestReorder { set => _requestReorder = value; }

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
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stateService.MeetingsChanged -= OnMeetingsChanged;

        foreach (ViewListEntry entry in _entries.Values)
        {
            UnsubscribeEntry(entry);
            _viewList.Remove(entry.SillItem);
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
        if (_disposed)
        {
            return;
        }

        IReadOnlyList<MeetingSillItemViewModel> activeMeetings = _stateService.GetActiveMeetings();

        HashSet<MeetingKey> activeKeys = activeMeetings
            .Select(vm => vm.Key)
            .ToHashSet();

        // Remove stale entries (meeting ended, hidden, or no longer active).
        List<MeetingKey> toRemove = _entries.Keys
            .Where(k => !activeKeys.Contains(k))
            .ToList();

        foreach (MeetingKey key in toRemove)
        {
            if (_entries.Remove(key, out ViewListEntry? entry))
            {
                UnsubscribeEntry(entry);
                _viewList.Remove(entry.SillItem);
            }
        }

        // Add new entries (meetings in state that we don't have UI elements for yet).
        bool enableFlashing = _settingsProvider.GetSetting(Settings.Settings.EnableSillFlashing);

        foreach (MeetingSillItemViewModel vm in activeMeetings)
        {
            if (_entries.ContainsKey(vm.Key))
            {
                continue;
            }

            // Create per-instance UI elements bound to the SHARED VM.
            MeetingBarContent barContent = MeetingBarContent.Create(vm);
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
                    _settingsProvider,
                    onHide: () => _stateService.HideMeeting(capturedKey));
            };

            var sillItem = new SillListViewMenuFlyoutItem(
                barContent,
                null,
                menuFlyout)
                .PreviewFlyoutContent(previewFlyout);

            // Wire orientation changes (per-instance).
            sillItem.IsSillOrientationOrSizeChanged += (_, _) =>
            {
                barContent.ApplyOrientationState(sillItem.SillOrientationAndSize);
            };
            barContent.ApplyOrientationState(sillItem.SillOrientationAndSize);

            // Wire flashing: shared VM event → per-instance sill item.
            // Track the handler so we can unsubscribe on dispose.
            Action? flashHandler = null;
            if (enableFlashing)
            {
                flashHandler = () => sillItem.StartFlashing();
                vm.FlashRequested += flashHandler;
            }

            // Track PropertyChanged for preview flyout live updates.
            // MeetingPreviewFlyout subscribes internally — we track VM ref for cleanup.

            // NOTE: No NotificationRequested subscription.
            // Notifications are dispatched centrally by MeetingStateService.

            _entries[vm.Key] = new ViewListEntry(vm, sillItem, flashHandler);

            if (!_viewList.Contains(sillItem))
            {
                _viewList.Add(sillItem);
            }
        }

        _requestReorder?.Invoke();
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
    /// Tracks the per-instance sill item and event handlers for cleanup.
    /// </summary>
    private sealed record ViewListEntry(
        MeetingSillItemViewModel Vm,
        SillListViewMenuFlyoutItem SillItem,
        Action? FlashHandler);
}
