using System.ComponentModel.Composition;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

using WindowSill.API;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Settings;
using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Core.Services;

/// <summary>
/// MEF singleton that manages the canonical state of upcoming meetings.
/// Owns the single timer, event fetch cycle, VMs, hidden set, and notification dedup.
/// </summary>
[Export]
internal sealed class MeetingStateService : IDisposable
{
    private readonly ILogger _logger;
    private readonly CalendarAccountManager _calendarAccountManager;
    private readonly MeetingNotificationService _notificationService;
    private readonly ISettingsProvider _settingsProvider;
    private readonly Lazy<ITravelTimeEstimator> _travelTimeEstimator;

    private readonly Dictionary<MeetingKey, MeetingSillItemViewModel> _meetings = [];
    private readonly HashSet<MeetingKey> _hiddenMeetings = [];
    private readonly HashSet<MeetingKey> _departureNotified = [];
    private readonly HashSet<MeetingKey> _liveNotified = [];

    private DispatcherQueueTimer? _timer;
    private CancellationTokenSource? _refreshCts;
    private DateTimeOffset _lastRefresh = DateTimeOffset.MinValue;
    private bool _started;
    private bool _disposed;

    [ImportingConstructor]
    public MeetingStateService(
        CalendarAccountManager calendarAccountManager,
        MeetingNotificationService notificationService,
        ISettingsProvider settingsProvider,
        Lazy<ITravelTimeEstimator> travelTimeEstimator)
    {
        _logger = this.Log();
        _calendarAccountManager = calendarAccountManager;
        _notificationService = notificationService;
        _settingsProvider = settingsProvider;
        _travelTimeEstimator = travelTimeEstimator;
    }

    /// <inheritdoc/>
    public event Action? MeetingsChanged;

    /// <inheritdoc/>
    public IReadOnlyList<MeetingSillItemViewModel> GetActiveMeetings()
    {
        return [.. _meetings.Values];
    }

    /// <inheritdoc/>
    public void Start(DispatcherQueue dispatcherQueue)
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _timer = dispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTick;
        _timer.Start();

        RefreshMeetingsAsync().ForgetSafely();
    }

    /// <inheritdoc/>
    public void HideMeeting(MeetingKey key)
    {
        _hiddenMeetings.Add(key);

        if (_meetings.Remove(key, out MeetingSillItemViewModel? vm))
        {
            vm.Dispose();
        }

        MeetingsChanged?.Invoke();
    }

    /// <inheritdoc/>
    public void RequestRefresh()
    {
        _lastRefresh = DateTimeOffset.MinValue;
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

        foreach (MeetingSillItemViewModel vm in _meetings.Values)
        {
            vm.Dispose();
        }

        _meetings.Clear();
        _hiddenMeetings.Clear();
        _departureNotified.Clear();
        _liveNotified.Clear();
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        DateTimeOffset now = DateTimeOffset.Now;

        // Slow reconciliation.
        int pollSeconds = _settingsProvider.GetSetting(Settings.Settings.MeetingPollIntervalSeconds);
        if (now - _lastRefresh >= TimeSpan.FromSeconds(pollSeconds))
        {
            RefreshMeetingsAsync().ForgetSafely();
        }

        // Fast tick: update countdowns for all meetings.
        bool showJoinButton = _settingsProvider.GetSetting(Settings.Settings.ShowJoinButton);
        int departureBuffer = _settingsProvider.GetSetting(Settings.Settings.DepartureBufferMinutes);
        NotificationMode confirmedMode = _settingsProvider.GetSetting(Settings.Settings.ConfirmedNotificationMode);
        NotificationMode tentativeMode = _settingsProvider.GetSetting(Settings.Settings.TentativeNotificationMode);
        bool changed = false;
        List<MeetingKey>? toRemove = null;

        foreach ((MeetingKey key, MeetingSillItemViewModel vm) in _meetings)
        {
            MeetingPhase previousPhase = vm.Phase;
            vm.UpdateCountdown(now, showJoinButton, departureBuffer);

            if (vm.Phase != previousPhase)
            {
                changed = true;
            }

            if (vm.Phase == MeetingPhase.Ended)
            {
                toRemove ??= [];
                toRemove.Add(key);
            }

            // Pick the notification mode based on event response status.
            bool isTentative = vm.Event.ResponseStatus
                is AttendeeResponseStatus.Tentative
                or AttendeeResponseStatus.NotResponded;
            NotificationMode notificationMode = isTentative ? tentativeMode : confirmedMode;

            // Centralized notification dispatch — different notification per phase.
            if (notificationMode != NotificationMode.None && vm.Phase != previousPhase)
            {
                if (vm.Phase == MeetingPhase.Departure && _departureNotified.Add(key))
                {
                    MapsProvider mapsProvider = _settingsProvider.GetSetting(Settings.Settings.PreferredMapsProvider);
                    Settings.TravelMode travelMode = _settingsProvider.GetSetting(Settings.Settings.TravelMode);

                    if (notificationMode == NotificationMode.FullScreen)
                    {
                        _notificationService.ShowDepartureNotificationAsync(
                            vm.Event, vm.TravelTimeText, mapsProvider, travelMode).ForgetSafely();
                    }
                    else
                    {
                        _notificationService.ShowDepartureToastNotification(vm.Event, vm.TravelTimeText);
                    }
                }
                else if (vm.Phase == MeetingPhase.Live && _liveNotified.Add(key))
                {
                    if (notificationMode == NotificationMode.FullScreen)
                    {
                        _notificationService.ShowNotificationAsync(vm.Event).ForgetSafely();
                    }
                    else
                    {
                        _notificationService.ShowToastNotification(vm.Event);
                    }
                }
            }
        }

        if (toRemove is not null)
        {
            foreach (MeetingKey key in toRemove)
            {
                if (_meetings.Remove(key, out MeetingSillItemViewModel? vm))
                {
                    vm.Dispose();
                }
            }

            changed = true;
        }

        if (changed)
        {
            MeetingsChanged?.Invoke();
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
            int departureBuffer = _settingsProvider.GetSetting(Settings.Settings.DepartureBufferMinutes);
            int maxSills = _settingsProvider.GetSetting(Settings.Settings.MaxMeetingSills);
            bool showAllDay = _settingsProvider.GetSetting(Settings.Settings.ShowAllDayMeetings);
            bool showJoinButton = _settingsProvider.GetSetting(Settings.Settings.ShowJoinButton);

            int lookAheadMinutes = reminderMinutes + departureBuffer;

            IReadOnlyList<CalendarEvent> events = await _calendarAccountManager.GetUpcomingEventsAsync(
                TimeSpan.FromMinutes(lookAheadMinutes), ct);

            if (ct.IsCancellationRequested || _disposed)
            {
                return;
            }

            var filtered = events
                .Where(e => e.Status != CalendarEventStatus.Cancelled
                    && e.ResponseStatus != AttendeeResponseStatus.Declined
                    && (showAllDay || !e.IsAllDay))
                .Take(maxSills)
                .ToList();

            bool changed = SyncMeetings(filtered, showJoinButton, departureBuffer);

            // Request travel time for new meetings with locations.
            if (_settingsProvider.GetSetting(Settings.Settings.EnableTravelTime))
            {
                var needsTravel = _meetings.Values
                    .Where(vm => vm.HasLocation && vm.TravelTimeEstimate is null)
                    .ToList();

                foreach (MeetingSillItemViewModel vm in needsTravel)
                {
                    RequestTravelTimeAsync(vm).ForgetSafely();
                }
            }

            if (changed)
            {
                MeetingsChanged?.Invoke();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh upcoming meetings.");
        }
    }

    private bool SyncMeetings(List<CalendarEvent> events, bool showJoinButton, int departureBuffer)
    {
        var newKeys = events.Select(MeetingKey.FromEvent).ToHashSet();
        bool changed = false;

        // Remove stale.
        var staleKeys = _meetings.Keys.Where(k => !newKeys.Contains(k)).ToList();
        foreach (MeetingKey key in staleKeys)
        {
            if (_meetings.Remove(key, out MeetingSillItemViewModel? vm))
            {
                vm.Dispose();
                changed = true;
            }
        }

        // Add new.
        DateTimeOffset now = DateTimeOffset.Now;
        foreach (CalendarEvent evt in events)
        {
            var key = MeetingKey.FromEvent(evt);
            if (_meetings.ContainsKey(key) || _hiddenMeetings.Contains(key))
            {
                continue;
            }

            var vm = new MeetingSillItemViewModel(evt);
            vm.UpdateCountdown(now, showJoinButton, departureBuffer);

            if (vm.Phase == MeetingPhase.Ended)
            {
                vm.Dispose();
                continue;
            }

            _meetings[key] = vm;
            changed = true;
        }

        return changed;
    }

    private async Task RequestTravelTimeAsync(MeetingSillItemViewModel vm)
    {
        try
        {
            TravelTimeEstimateResult? result = await _travelTimeEstimator.Value.EstimateTravelTimeAsync(vm.Event);
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
            _logger.LogWarning(ex, "Failed to estimate travel time for: {Title}", vm.Event.Title);
        }
    }
}
