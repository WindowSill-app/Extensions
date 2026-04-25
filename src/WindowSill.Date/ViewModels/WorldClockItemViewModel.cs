using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;

using NodaTime;

using WindowSill.API;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// ViewModel representing a single world clock entry in the popup.
/// Computes display time, UTC offset, and day/night status.
/// </summary>
internal sealed partial class WorldClockItemViewModel : ObservableObject
{
    private readonly DateTimeZone _zone;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldClockItemViewModel"/> class.
    /// </summary>
    /// <param name="entry">The world clock entry configuration.</param>
    /// <param name="zone">The NodaTime timezone for this entry.</param>
    public WorldClockItemViewModel(WorldClockEntry entry, DateTimeZone zone)
    {
        Entry = entry;
        _zone = zone;

        // Initial update with culture default; the popup VM will call Update() with the user's preferred format.
        string defaultFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
        Update(timeTravelOffsetMinutes: 0, timeFormatString: defaultFormat);
    }

    /// <summary>
    /// Gets the underlying world clock entry.
    /// </summary>
    public WorldClockEntry Entry { get; }

    /// <summary>
    /// Gets the display name for this clock.
    /// </summary>
    public string DisplayName => Entry.DisplayName;

    /// <summary>
    /// Gets the formatted current time in this timezone.
    /// </summary>
    [ObservableProperty]
    public partial string CurrentTime { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the relative day text (e.g., "Today", "Tomorrow", "Yesterday", or a date).
    /// </summary>
    [ObservableProperty]
    public partial string DayText { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the UTC offset text relative to local time (e.g., "+5h", "-8h 30m").
    /// </summary>
    [ObservableProperty]
    public partial string OffsetText { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether it is daytime (6 AM–6 PM) in this timezone.
    /// </summary>
    [ObservableProperty]
    public partial bool IsDaytime { get; private set; }

    /// <summary>
    /// Updates the displayed time and offset, optionally applying a time-travel offset.
    /// </summary>
    /// <param name="timeTravelOffsetMinutes">The time-travel offset in minutes from "now".</param>
    /// <param name="timeFormatString">The .NET time format string to use (e.g., "h:mm tt", "HH:mm").</param>
    public void Update(int timeTravelOffsetMinutes, string timeFormatString)
    {
        Instant now = SystemClock.Instance.GetCurrentInstant();

        if (timeTravelOffsetMinutes != 0)
        {
            now = now.Plus(NodaTime.Duration.FromMinutes(timeTravelOffsetMinutes));
        }

        ZonedDateTime remoteTime = now.InZone(_zone);

        // Format time using the user's preferred format.
        CurrentTime = remoteTime.ToDateTimeUnspecified()
            .ToString(timeFormatString, CultureInfo.CurrentCulture);

        // Compute relative day text — always relative to the real today,
        // not the time-traveled local date.
        LocalDate remoteDate = remoteTime.Date;
        LocalDate todayLocal = SystemClock.Instance.GetCurrentInstant()
            .InZone(DateTimeZoneProviders.Tzdb.GetSystemDefault()).Date;
        int dayDiff = Period.Between(todayLocal, remoteDate, PeriodUnits.Days).Days;

        DayText = FormatDayDiff(dayDiff, remoteTime.ToDateTimeUnspecified());

        // Compute offset relative to local time.
        Offset remoteOffset = _zone.GetUtcOffset(now);
        Offset localOffset = DateTimeZoneProviders.Tzdb.GetSystemDefault().GetUtcOffset(now);
        long diffMinutes = (remoteOffset.Milliseconds - localOffset.Milliseconds) / 60_000;

        if (diffMinutes == 0)
        {
            OffsetText = string.Empty;
        }
        else
        {
            string sign = diffMinutes > 0 ? "+" : "−";
            long absDiff = Math.Abs(diffMinutes);
            long hours = absDiff / 60;
            long mins = absDiff % 60;

            OffsetText = mins == 0
                ? $"{sign}{hours}h"
                : $"{sign}{hours}h {mins}m";
        }

        // Day/night heuristic: 6 AM to 6 PM.
        int hour = remoteTime.Hour;
        IsDaytime = hour >= 6 && hour < 18;
    }

    /// <summary>
    /// Formats a day difference into a human-readable string.
    /// </summary>
    /// <param name="dayDiff">The day difference from <see cref="ComputeDayDiff"/>.</param>
    /// <param name="remoteDateTimeForFallback">The remote DateTime used for formatting dates beyond ±1 day.</param>
    /// <returns>A localized day text (e.g., "Today", "Tomorrow", or a formatted date).</returns>
    private static string FormatDayDiff(int dayDiff, DateTime remoteDateTimeForFallback)
    {
        return dayDiff switch
        {
            0 => "/WindowSill.Date/Popup/Today".GetLocalizedString(),
            1 => "/WindowSill.Date/Popup/Tomorrow".GetLocalizedString(),
            -1 => "/WindowSill.Date/Popup/Yesterday".GetLocalizedString(),
            _ => remoteDateTimeForFallback.ToString("ddd, d MMM", CultureInfo.CurrentCulture),
        };
    }
}
