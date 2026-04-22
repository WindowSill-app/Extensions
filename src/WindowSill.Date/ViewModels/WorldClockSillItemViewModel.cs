using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;

using NodaTime;

using WindowSill.Date.Core.Models;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// ViewModel for a pinned world clock sill item in the bar.
/// Updated every second by the adapter's timer.
/// </summary>
internal sealed partial class WorldClockSillItemViewModel : ObservableObject
{
    private readonly DateTimeZone _zone;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldClockSillItemViewModel"/> class.
    /// </summary>
    /// <param name="entry">The world clock entry.</param>
    /// <param name="zone">The resolved NodaTime timezone.</param>
    public WorldClockSillItemViewModel(WorldClockEntry entry, DateTimeZone zone)
    {
        Entry = entry;
        _zone = zone;
    }

    /// <summary>
    /// Gets the underlying entry.
    /// </summary>
    public WorldClockEntry Entry { get; }

    /// <summary>
    /// Gets the display name for the bar.
    /// </summary>
    public string DisplayName => Entry.DisplayName;

    /// <summary>
    /// Raises property-changed for <see cref="DisplayName"/> after the entry is renamed.
    /// </summary>
    public void RefreshDisplayName() => OnPropertyChanged(nameof(DisplayName));

    /// <summary>
    /// Gets the timezone identifier.
    /// </summary>
    public string TimeZoneId => Entry.TimeZoneId;

    /// <summary>
    /// Gets the formatted current time (e.g., "11:42 PM").
    /// </summary>
    [ObservableProperty]
    public partial string CurrentTime { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the formatted current date (e.g., "Tue, 22 Apr").
    /// </summary>
    [ObservableProperty]
    public partial string CurrentDate { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the UTC offset text relative to local time (e.g., "+9h").
    /// </summary>
    [ObservableProperty]
    public partial string OffsetText { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether it is daytime (6 AM–6 PM).
    /// </summary>
    [ObservableProperty]
    public partial bool IsDaytime { get; private set; }

    /// <summary>
    /// Updates all display properties based on the current instant.
    /// </summary>
    /// <param name="timeFormatString">The user's time format string.</param>
    public void Update(string timeFormatString)
    {
        Instant now = SystemClock.Instance.GetCurrentInstant();
        ZonedDateTime remoteTime = now.InZone(_zone);

        CurrentTime = remoteTime.ToDateTimeUnspecified()
            .ToString(timeFormatString, CultureInfo.CurrentCulture);

        CurrentDate = remoteTime.ToDateTimeUnspecified()
            .ToString("ddd, d MMM", CultureInfo.CurrentCulture);

        // Offset relative to local time.
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
            OffsetText = mins == 0 ? $"{sign}{hours}h" : $"{sign}{hours}h {mins}m";
        }

        int hour = remoteTime.Hour;
        IsDaytime = hour >= 6 && hour < 18;
    }
}
