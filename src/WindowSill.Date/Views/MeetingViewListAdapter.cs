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

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeetingViewListAdapter"/> class.
    /// </summary>
    /// <param name="stateService">The shared meeting state singleton.</param>
    /// <param name="worldClockService">The world clock service for flyout time zones.</param>
    /// <param name="settingsProvider">The settings provider.</param>
    /// <param name="viewList">This DateSill instance's ViewList.</param>
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

            MeetingKey capturedKey = vm.Key;
            MenuFlyout menuFlyout = MeetingFlyoutBuilder.Build(
                vm,
                _worldClockService,
                onHide: () => _stateService.HideMeeting(capturedKey));

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
            if (enableFlashing)
            {
                vm.FlashRequested += () => sillItem.StartFlashing();
            }

            // NOTE: No NotificationRequested subscription.
            // Notifications are dispatched centrally by MeetingStateService.

            _entries[vm.Key] = new ViewListEntry(sillItem);

            // Insert before the date bar item (which is always last).
            int insertIndex = Math.Max(0, _viewList.Count - 1);
            _viewList.Insert(insertIndex, sillItem);
        }
    }

    /// <summary>
    /// Tracks the per-instance sill item for a meeting.
    /// </summary>
    private sealed record ViewListEntry(SillListViewMenuFlyoutItem SillItem);
}
