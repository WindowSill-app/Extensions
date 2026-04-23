using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.UI.Dispatching;

using WindowSill.API;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Services;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// ViewModel for the Date popup. Orchestrates calendar day selection, event loading,
/// world clock display, and the time-travel slider.
/// </summary>
internal sealed partial class DatePopupViewModel : ObservableObject, IDisposable
{
    private readonly CalendarAccountManager _calendarAccountManager;
    private readonly WorldClockService _worldClockService;
    private readonly ISettingsProvider _settingsProvider;

    private DispatcherQueueTimer? _timer;
    private CancellationTokenSource? _loadEventsCts;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatePopupViewModel"/> class.
    /// </summary>
    /// <param name="calendarAccountManager">The calendar account manager for fetching events.</param>
    /// <param name="worldClockService">The world clock service for timezone data.</param>
    /// <param name="settingsProvider">The settings provider for reading display preferences.</param>
    public DatePopupViewModel(
        CalendarAccountManager calendarAccountManager,
        WorldClockService worldClockService,
        ISettingsProvider settingsProvider)
    {
        _calendarAccountManager = calendarAccountManager;
        _worldClockService = worldClockService;
        _settingsProvider = settingsProvider;

        _calendarAccountManager.CalendarDisplayChanged += OnCalendarDisplayChanged;
    }

    /// <summary>
    /// Gets the events for the selected day.
    /// </summary>
    public ObservableCollection<EventItemViewModel> Events { get; } = [];

    /// <summary>
    /// Gets the world clock items.
    /// </summary>
    public ObservableCollection<WorldClockItemViewModel> WorldClocks { get; } = [];

    /// <summary>
    /// Gets or sets the selected date.
    /// </summary>
    [ObservableProperty]
    public partial DateTimeOffset SelectedDate { get; set; } = DateTimeOffset.Now.Date;

    /// <summary>
    /// Gets or sets the time travel offset in minutes.
    /// </summary>
    public int TimeTravelOffsetMinutes
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                UpdateTimeTravelLabel();
                RefreshWorldClockTimes();
            }
        }
    }

    /// <summary>
    /// Gets the time travel label text (e.g., "Now", "+3h", "-2h 15m").
    /// </summary>
    [ObservableProperty]
    public partial string TimeTravelLabel { get; private set; } = "Now";

    /// <summary>
    /// Gets the projected local time text when time-traveling (e.g., "02:00").
    /// </summary>
    [ObservableProperty]
    public partial string TimeTravelProjectedTime { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the projected relative day text when time-traveling (e.g., "Tomorrow", "Today").
    /// </summary>
    [ObservableProperty]
    public partial string TimeTravelProjectedDay { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the projected date text when time-traveling (e.g., "Wed, 1 Mar").
    /// </summary>
    [ObservableProperty]
    public partial string TimeTravelProjectedDate { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the event list is empty.
    /// </summary>
    [ObservableProperty]
    public partial bool HasNoEvents { get; private set; } = true;

    /// <summary>
    /// Gets a value indicating whether events are currently loading.
    /// </summary>
    [ObservableProperty]
    public partial bool IsLoadingEvents { get; private set; }

    /// <summary>
    /// Gets the header text for the event list (e.g., "Today", "Tomorrow", or a date).
    /// </summary>
    [ObservableProperty]
    public partial string EventListHeader { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether there are world clocks configured.
    /// </summary>
    public bool HasWorldClocks => WorldClocks.Count > 0;

    /// <summary>
    /// Gets or sets whether the calendar section is expanded in the popup.
    /// Persisted to settings as the default state for next open.
    /// </summary>
    [ObservableProperty]
    public partial bool IsCalendarVisible { get; set; }

    /// <summary>
    /// Gets or sets whether the world clocks section is expanded in the popup.
    /// Persisted to settings as the default state for next open.
    /// </summary>
    [ObservableProperty]
    public partial bool IsWorldClocksVisible { get; set; }

    partial void OnIsCalendarVisibleChanged(bool value)
    {
        _settingsProvider.SetSetting(Settings.Settings.ShowCalendarInPopup, value);
    }

    partial void OnIsWorldClocksVisibleChanged(bool value)
    {
        _settingsProvider.SetSetting(Settings.Settings.ShowWorldClocksInPopup, value);
    }

    /// <summary>
    /// Called when the popup opens. Initializes data and starts the timer.
    /// </summary>
    /// <param name="dispatcherQueue">The UI thread dispatcher queue.</param>
    public void OnPopupOpening(DispatcherQueue dispatcherQueue)
    {
        TimeTravelOffsetMinutes = 0;
        IsLoadingEvents = true;
        SelectedDate = DateTimeOffset.Now.Date;

        IsCalendarVisible = _settingsProvider.GetSetting(Settings.Settings.ShowCalendarInPopup);
        IsWorldClocksVisible = _settingsProvider.GetSetting(Settings.Settings.ShowWorldClocksInPopup);

        UpdateTimeTravelLabel();
        RefreshWorldClocks();
        UpdateEventListHeader();
        LoadEventsForSelectedDayAsync().ForgetSafely();

        _timer?.Stop();
        _timer = dispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    /// <summary>
    /// Raised when the popup closes, allowing subscribers to trigger actions (e.g., meeting refresh).
    /// </summary>
    public event Action? PopupClosed;

    /// <summary>
    /// Called when the popup closes. Stops the timer and resets time travel.
    /// </summary>
    public void OnPopupClosing()
    {
        _timer?.Stop();
        if (_timer is not null)
        {
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }

        _loadEventsCts?.Cancel();
        TimeTravelOffsetMinutes = 0;
        PopupClosed?.Invoke();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _calendarAccountManager.CalendarDisplayChanged -= OnCalendarDisplayChanged;
        OnPopupClosing();
        _loadEventsCts?.Dispose();
    }

    private void OnCalendarDisplayChanged(object? sender, EventArgs e)
    {
        LoadEventsForSelectedDayAsync().ForgetSafely();
    }

    partial void OnSelectedDateChanged(DateTimeOffset value)
    {
        UpdateEventListHeader();
        LoadEventsForSelectedDayAsync().ForgetSafely();
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        RefreshWorldClockTimes();
        UpdateTimeTravelLabel();
    }

    private async Task LoadEventsForSelectedDayAsync()
    {
        _loadEventsCts?.Cancel();
        _loadEventsCts?.Dispose();
        _loadEventsCts = new CancellationTokenSource();
        CancellationToken ct = _loadEventsCts.Token;

        IsLoadingEvents = true;

        try
        {
            DateTimeOffset dayStart = SelectedDate.Date;
            DateTimeOffset dayEnd = dayStart.AddDays(1);

            IReadOnlyList<CalendarEvent> events = await _calendarAccountManager.GetEventsAsync(dayStart, dayEnd, ct);

            if (ct.IsCancellationRequested)
            {
                return;
            }

            Events.Clear();

            // Filter out cancelled events.
            IEnumerable<CalendarEvent> filtered = events
                .Where(e => e.Status != CalendarEventStatus.Cancelled);

            // When viewing today and the setting is off, hide events that have already ended.
            bool isToday = DateOnly.FromDateTime(SelectedDate.Date) == DateOnly.FromDateTime(DateTime.Today);
            if (isToday && !_settingsProvider.GetSetting(Settings.Settings.ShowPastEvents))
            {
                DateTimeOffset now = DateTimeOffset.Now;
                filtered = filtered.Where(e => e.IsAllDay || e.EndTime > now);
            }

            // Sort: all-day first, then by start time.
            IEnumerable<CalendarEvent> sorted = filtered
                .OrderBy(e => e.IsAllDay ? 0 : 1)
                .ThenBy(e => e.StartTime);

            foreach (CalendarEvent calEvent in sorted)
            {
                Events.Add(new EventItemViewModel(calEvent));
            }

            HasNoEvents = Events.Count == 0;
        }
        catch (OperationCanceledException)
        {
            // Expected when the selected day changes rapidly.
        }
        catch
        {
            Events.Clear();
            HasNoEvents = true;
        }
        finally
        {
            // Only clear the loading state if this load was not superseded by a newer one.
            if (!ct.IsCancellationRequested)
            {
                IsLoadingEvents = false;
            }
        }
    }

    private void RefreshWorldClocks()
    {
        WorldClocks.Clear();

        IReadOnlyList<WorldClockEntry> entries = _worldClockService.GetEntries();
        foreach (WorldClockEntry entry in entries)
        {
            NodaTime.DateTimeZone zone = _worldClockService.GetTimeZone(entry.TimeZoneId);
            WorldClocks.Add(new WorldClockItemViewModel(entry, zone));
        }

        OnPropertyChanged(nameof(HasWorldClocks));
    }

    private void RefreshWorldClockTimes()
    {
        string timeFormat = GetEffectiveTimeFormatString();
        foreach (WorldClockItemViewModel clock in WorldClocks)
        {
            clock.Update(TimeTravelOffsetMinutes, timeFormat);
        }
    }

    private void UpdateTimeTravelLabel()
    {
        // Offset label
        if (TimeTravelOffsetMinutes == 0)
        {
            TimeTravelLabel = "Now";
        }
        else
        {
            string sign = TimeTravelOffsetMinutes > 0 ? "+" : "−";
            int absMinutes = Math.Abs(TimeTravelOffsetMinutes);
            int hours = absMinutes / 60;
            int mins = absMinutes % 60;

            TimeTravelLabel = mins == 0
                ? $"{sign}{hours}h"
                : $"{sign}{hours}h {mins}m";
        }

        // Projected (or current) local time
        DateTime projected = DateTime.Now.AddMinutes(TimeTravelOffsetMinutes);
        string timeFormat = GetEffectiveTimeFormatString();
        TimeTravelProjectedTime = projected.ToString(timeFormat, System.Globalization.CultureInfo.CurrentCulture);

        // Projected relative day
        var projectedDate = DateOnly.FromDateTime(projected);
        var today = DateOnly.FromDateTime(DateTime.Today);
        int dayDiff = projectedDate.DayNumber - today.DayNumber;

        TimeTravelProjectedDay = dayDiff switch
        {
            0 => "/WindowSill.Date/Popup/Today".GetLocalizedString(),
            1 => "/WindowSill.Date/Popup/Tomorrow".GetLocalizedString(),
            -1 => "/WindowSill.Date/Popup/Yesterday".GetLocalizedString(),
            _ => projectedDate.ToString("dddd", System.Globalization.CultureInfo.CurrentCulture),
        };

        // Projected date
        TimeTravelProjectedDate = projected.ToString("ddd, d MMM", System.Globalization.CultureInfo.CurrentCulture);
    }

    private string GetEffectiveTimeFormatString()
        => Core.TimeFormatHelper.GetTimeFormatString(_settingsProvider);

    private void UpdateEventListHeader()
    {
        var selected = DateOnly.FromDateTime(SelectedDate.Date);
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (selected == today)
        {
            EventListHeader = "/WindowSill.Date/Popup/Today".GetLocalizedString();
        }
        else if (selected == today.AddDays(1))
        {
            EventListHeader = "/WindowSill.Date/Popup/Tomorrow".GetLocalizedString();
        }
        else if (selected == today.AddDays(-1))
        {
            EventListHeader = "/WindowSill.Date/Popup/Yesterday".GetLocalizedString();
        }
        else
        {
            EventListHeader = SelectedDate.Date.ToString("dddd, MMMM d", System.Globalization.CultureInfo.CurrentCulture);
        }
    }
}
