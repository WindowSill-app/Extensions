using System.Collections.ObjectModel;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

using WindowSill.API;
using WindowSill.Date.Core.Models;
using WindowSill.Date.ViewModels;
using WindowSill.Date.Views;

namespace WindowSill.Date.Core.Services;

/// <summary>
/// Manages the lifecycle of meeting sill items: fetches upcoming events on a slow
/// reconciliation loop and drives 1-second countdown ticks for active meetings.
/// Meeting items are inserted before the date bar item in the ViewList.
/// </summary>
internal sealed class MeetingCountdownService : IDisposable
{
    private readonly ILogger _logger;
    private readonly CalendarAccountManager _calendarAccountManager;
    private readonly WorldClockService _worldClockService;
    private readonly MeetingNotificationService _notificationService;
    private readonly Lazy<ITravelTimeEstimator> _travelTimeEstimator;
    private readonly ISettingsProvider _settingsProvider;
    private readonly ObservableCollection<SillListViewItem> _viewList;

    /// <summary>
    /// Maps active meeting keys to their entry (ViewModel + sill item).
    /// </summary>
    private readonly Dictionary<MeetingKey, MeetingItemEntry> _activeMeetings = [];

    /// <summary>
    /// Meetings hidden by the user (in-memory, resets on deactivate).
    /// </summary>
    private readonly HashSet<MeetingKey> _hiddenMeetings = [];

    private DispatcherQueueTimer? _timer;
    private CancellationTokenSource? _refreshCts;
    private DateTimeOffset _lastRefresh = DateTimeOffset.MinValue;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeetingCountdownService"/> class.
    /// </summary>
    public MeetingCountdownService(
        CalendarAccountManager calendarAccountManager,
        WorldClockService worldClockService,
        MeetingNotificationService notificationService,
        ISettingsProvider settingsProvider,
        ObservableCollection<SillListViewItem> viewList,
        Lazy<ITravelTimeEstimator> travelTimeEstimator)
    {
        _logger = this.Log();
        _calendarAccountManager = calendarAccountManager;
        _worldClockService = worldClockService;
        _notificationService = notificationService;
        _settingsProvider = settingsProvider;
        _viewList = viewList;
        _travelTimeEstimator = travelTimeEstimator;
    }

    /// <summary>
    /// Starts the service: initial fetch + 1-second timer.
    /// </summary>
    /// <param name="dispatcherQueue">The UI thread dispatcher queue.</param>
    public void Start(DispatcherQueue dispatcherQueue)
    {
        _timer = dispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTick;
        _timer.Start();

        // Initial fetch.
        RefreshMeetingsAsync().ForgetSafely();
    }

    /// <summary>
    /// Triggers an on-demand refresh (e.g., after popup close or settings change).
    /// </summary>
    public void RequestRefresh()
    {
        _lastRefresh = DateTimeOffset.MinValue;
    }

    /// <summary>
    /// Hides a meeting from the sill (user action). Won't reappear until deactivation.
    /// </summary>
    /// <param name="key">The meeting key to hide.</param>
    public void HideMeeting(MeetingKey key)
    {
        _hiddenMeetings.Add(key);

        if (_activeMeetings.Remove(key, out MeetingItemEntry? entry))
        {
            RemoveSillItem(entry);
            entry.ViewModel.Dispose();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _timer?.Stop();
        if (_timer is not null)
        {
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }

        _refreshCts?.Cancel();
        _refreshCts?.Dispose();

        foreach (MeetingItemEntry entry in _activeMeetings.Values)
        {
            RemoveSillItem(entry);
            entry.ViewModel.Dispose();
        }

        _activeMeetings.Clear();
        _hiddenMeetings.Clear();
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        DateTimeOffset now = DateTimeOffset.Now;

        // Slow reconciliation: periodic event fetch.
        int pollSeconds = _settingsProvider.GetSetting(Settings.Settings.MeetingPollIntervalSeconds);
        if (now - _lastRefresh >= TimeSpan.FromSeconds(pollSeconds))
        {
            RefreshMeetingsAsync().ForgetSafely();
        }

        // Fast tick: update countdowns for all active meetings.
        bool showJoinButton = _settingsProvider.GetSetting(Settings.Settings.ShowJoinButton);
        int departureBuffer = _settingsProvider.GetSetting(Settings.Settings.DepartureBufferMinutes);
        List<MeetingKey>? toRemove = null;

        foreach ((MeetingKey key, MeetingItemEntry entry) in _activeMeetings)
        {
            entry.ViewModel.UpdateCountdown(now, showJoinButton, departureBuffer);

            if (entry.ViewModel.Phase == MeetingPhase.Ended)
            {
                toRemove ??= [];
                toRemove.Add(key);
            }
        }

        // Remove ended meetings.
        if (toRemove is not null)
        {
            foreach (MeetingKey key in toRemove)
            {
                if (_activeMeetings.Remove(key, out MeetingItemEntry? entry))
                {
                    RemoveSillItem(entry);
                    entry.ViewModel.Dispose();
                }
            }
        }
    }

    private async Task RefreshMeetingsAsync()
    {
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = new CancellationTokenSource();
        CancellationToken ct = _refreshCts.Token;

        _lastRefresh = DateTimeOffset.Now;

        try
        {
            int reminderMinutes = _settingsProvider.GetSetting(Settings.Settings.ReminderWindowMinutes);
            int fallbackCommute = _settingsProvider.GetSetting(Settings.Settings.FallbackCommuteMinutes);
            int departureBuffer = _settingsProvider.GetSetting(Settings.Settings.DepartureBufferMinutes);
            int maxSills = _settingsProvider.GetSetting(Settings.Settings.MaxMeetingSills);

            // Extend lookAhead to account for travel time + buffer: reminder + commute + buffer.
            int lookAheadMinutes = reminderMinutes + fallbackCommute + departureBuffer;
            bool showAllDay = _settingsProvider.GetSetting(Settings.Settings.ShowAllDayMeetings);
            bool showJoinButton = _settingsProvider.GetSetting(Settings.Settings.ShowJoinButton);
            bool enableFlashing = _settingsProvider.GetSetting(Settings.Settings.EnableSillFlashing);
            bool enableFullScreen = _settingsProvider.GetSetting(Settings.Settings.EnableFullScreenNotification);

            IReadOnlyList<CalendarEvent> events = await _calendarAccountManager.GetUpcomingEventsAsync(
                TimeSpan.FromMinutes(lookAheadMinutes),
                ct);

            if (ct.IsCancellationRequested || _disposed)
            {
                return;
            }

            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                if (_disposed)
                {
                    return;
                }

                SyncViewList(events, maxSills, showAllDay, showJoinButton, enableFlashing, enableFullScreen);
            });
        }
        catch (OperationCanceledException)
        {
            // Expected when a new refresh supersedes this one.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh upcoming meetings.");
        }
    }

    /// <summary>
    /// Synchronizes the ViewList with the fetched events.
    /// Adds new meetings, keeps existing ones, removes stale ones.
    /// </summary>
    private void SyncViewList(
        IReadOnlyList<CalendarEvent> events,
        int maxSills,
        bool showAllDay,
        bool showJoinButton,
        bool enableFlashing,
        bool enableFullScreen)
    {
        // Filter events.
        List<CalendarEvent> filtered = events
            .Where(e => e.Status != CalendarEventStatus.Cancelled
                && e.ResponseStatus != AttendeeResponseStatus.Declined
                && (showAllDay || !e.IsAllDay))
            .Take(maxSills)
            .ToList();

        // Build set of keys in the new data.
        HashSet<MeetingKey> newKeys = filtered
            .Select(MeetingKey.FromEvent)
            .ToHashSet();

        // Remove items no longer in the fetched list.
        List<MeetingKey> staleKeys = _activeMeetings.Keys
            .Where(k => !newKeys.Contains(k))
            .ToList();

        foreach (MeetingKey key in staleKeys)
        {
            if (_activeMeetings.Remove(key, out MeetingItemEntry? entry))
            {
                RemoveSillItem(entry);
                entry.ViewModel.Dispose();
            }
        }

        // Add new items.
        DateTimeOffset now = DateTimeOffset.Now;
        foreach (CalendarEvent evt in filtered)
        {
            MeetingKey key = MeetingKey.FromEvent(evt);

            if (_activeMeetings.ContainsKey(key) || _hiddenMeetings.Contains(key))
            {
                continue;
            }

            var vm = new MeetingSillItemViewModel(evt);
            vm.UpdateCountdown(now, showJoinButton);

            if (vm.Phase == MeetingPhase.Ended)
            {
                vm.Dispose();
                continue;
            }

            // Create bar content, preview flyout, and menu flyout.
            MeetingBarContent barContent = MeetingBarContent.Create(vm);
            var previewFlyout = new MeetingPreviewFlyout(vm);

            MeetingKey capturedKey = key;
            MenuFlyout menuFlyout = MeetingFlyoutBuilder.Build(
                vm,
                _worldClockService,
                onHide: () => HideMeeting(capturedKey));

            var sillItem = new SillListViewMenuFlyoutItem(
                barContent,
                null,
                menuFlyout)
                .PreviewFlyoutContent(previewFlyout);

            // Wire orientation changes.
            sillItem.IsSillOrientationOrSizeChanged += (_, _) =>
            {
                barContent.ApplyOrientationState(sillItem.SillOrientationAndSize);
            };
            barContent.ApplyOrientationState(sillItem.SillOrientationAndSize);

            // Wire edge-triggered side effects.
            if (enableFlashing)
            {
                vm.FlashRequested += () => sillItem.StartFlashing();
            }

            if (enableFullScreen)
            {
                vm.NotificationRequested += () =>
                {
                    _notificationService.ShowNotificationAsync(evt).ForgetSafely();
                };
            }

            MeetingItemEntry entry = new(vm, sillItem);
            _activeMeetings[key] = entry;

            // Insert before the date bar item (which is always last).
            int insertIndex = Math.Max(0, _viewList.Count - 1);
            _viewList.Insert(insertIndex, sillItem);

            // Request travel time estimate asynchronously for meetings with a location.
            if (vm.HasLocation)
            {
                RequestTravelTimeAsync(vm, evt).ForgetSafely();
            }
        }
    }

    /// <summary>
    /// Requests a travel time estimate for a meeting and stores the result in the VM.
    /// </summary>
    private async Task RequestTravelTimeAsync(MeetingSillItemViewModel vm, CalendarEvent evt)
    {
        try
        {
            TravelTimeEstimateResult result = await _travelTimeEstimator.Value.EstimateTravelTimeAsync(evt);
            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                if (!_disposed)
                {
                    vm.TravelTimeEstimate = result;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to estimate travel time for: {Title}", evt.Title);
        }
    }

    private void RemoveSillItem(MeetingItemEntry entry)
    {
        _viewList.Remove(entry.SillItem);
    }

    /// <summary>
    /// Tracks a meeting's ViewModel and its corresponding sill item.
    /// </summary>
    private sealed record MeetingItemEntry(
        MeetingSillItemViewModel ViewModel,
        SillListViewMenuFlyoutItem SillItem);
}
