using CommunityToolkit.Mvvm.ComponentModel;

using WindowSill.API;
using WindowSill.Date.Settings;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// ViewModel for the Date &amp; Time settings tab.
/// Manages display mode, date/time format, and popup display preferences.
/// </summary>
internal sealed partial class DateTimeSettingsViewModel : ObservableObject
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly IReadOnlyList<FormatOptionItem<DateFormat>> _allDateFormats;
    private readonly IReadOnlyList<FormatOptionItem<TimeFormat>> _allTimeFormats;

    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeSettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsProvider">The settings provider for persisting preferences.</param>
    public DateTimeSettingsViewModel(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;

        AvailableDisplayModes = BuildDisplayModeItems();
        _allDateFormats = BuildDateFormatItems();
        _allTimeFormats = BuildTimeFormatItems();
    }

    /// <summary>
    /// Gets the available display mode options.
    /// </summary>
    public IReadOnlyList<FormatOptionItem<SillDisplayMode>> AvailableDisplayModes { get; }

    /// <summary>
    /// Gets the available date format options.
    /// </summary>
    public IReadOnlyList<FormatOptionItem<DateFormat>> AvailableDateFormats
        => _settingsProvider.GetSetting(Settings.Settings.TimeFormat) == TimeFormat.None
            ? _allDateFormats.Where(i => i.Value != DateFormat.None).ToList()
            : _allDateFormats;

    /// <summary>
    /// Gets the available time format options.
    /// </summary>
    public IReadOnlyList<FormatOptionItem<TimeFormat>> AvailableTimeFormats
        => _settingsProvider.GetSetting(Settings.Settings.DateFormat) == DateFormat.None
            ? _allTimeFormats.Where(i => i.Value != TimeFormat.None).ToList()
            : _allTimeFormats;

    /// <summary>
    /// Gets or sets the selected display mode item.
    /// </summary>
    public FormatOptionItem<SillDisplayMode>? SelectedDisplayMode
    {
        get => AvailableDisplayModes.FirstOrDefault(i => i.Value == _settingsProvider.GetSetting(Settings.Settings.DisplayMode));
        set
        {
            if (value is not null
                && value.Value != _settingsProvider.GetSetting(Settings.Settings.DisplayMode))
            {
                _settingsProvider.SetSetting(Settings.Settings.DisplayMode, value.Value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDateTimeMode));
                OnPropertyChanged(nameof(IsShowSecondsVisible));
                OnPropertyChanged(nameof(IsIconModeInfoVisible));
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected date format item.
    /// </summary>
    public FormatOptionItem<DateFormat>? SelectedDateFormat
    {
        get => _allDateFormats.FirstOrDefault(i => i.Value == _settingsProvider.GetSetting(Settings.Settings.DateFormat));
        set
        {
            if (value is null
                || value.Value == _settingsProvider.GetSetting(Settings.Settings.DateFormat))
            {
                return;
            }

            _settingsProvider.SetSetting(Settings.Settings.DateFormat, value.Value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(AvailableTimeFormats));
            OnPropertyChanged(nameof(SelectedTimeFormat));
        }
    }

    /// <summary>
    /// Gets or sets the selected time format item.
    /// </summary>
    public FormatOptionItem<TimeFormat>? SelectedTimeFormat
    {
        get => _allTimeFormats.FirstOrDefault(i => i.Value == _settingsProvider.GetSetting(Settings.Settings.TimeFormat));
        set
        {
            if (value is null
                || value.Value == _settingsProvider.GetSetting(Settings.Settings.TimeFormat))
            {
                return;
            }

            _settingsProvider.SetSetting(Settings.Settings.TimeFormat, value.Value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsShowSecondsVisible));
            OnPropertyChanged(nameof(AvailableDateFormats));
            OnPropertyChanged(nameof(SelectedDateFormat));
        }
    }

    /// <summary>
    /// Gets or sets whether to show seconds in the time display.
    /// </summary>
    public bool ShowSeconds
    {
        get => _settingsProvider.GetSetting(Settings.Settings.ShowSeconds);
        set => _settingsProvider.SetSetting(Settings.Settings.ShowSeconds, value);
    }

    /// <summary>
    /// Gets whether the date/time settings section should be visible.
    /// </summary>
    public bool IsDateTimeMode
        => _settingsProvider.GetSetting(Settings.Settings.DisplayMode) == SillDisplayMode.DateTime;

    /// <summary>
    /// Gets whether the "Show seconds" toggle should be visible.
    /// </summary>
    public bool IsShowSecondsVisible
        => IsDateTimeMode
        && _settingsProvider.GetSetting(Settings.Settings.TimeFormat) != TimeFormat.None;

    /// <summary>
    /// Gets whether the icon-mode info message should be visible.
    /// </summary>
    public bool IsIconModeInfoVisible
        => _settingsProvider.GetSetting(Settings.Settings.DisplayMode) == SillDisplayMode.Icon;

    /// <summary>
    /// Gets or sets whether past events are shown in the popup event list.
    /// </summary>
    public bool ShowPastEvents
    {
        get => _settingsProvider.GetSetting(Settings.Settings.ShowPastEvents);
        set => _settingsProvider.SetSetting(Settings.Settings.ShowPastEvents, value);
    }

    private static IReadOnlyList<FormatOptionItem<SillDisplayMode>> BuildDisplayModeItems()
    {
        return
        [
            new FormatOptionItem<SillDisplayMode>(
                SillDisplayMode.Icon,
                "/WindowSill.Date/Display/DisplayModeIcon".GetLocalizedString()),
            new FormatOptionItem<SillDisplayMode>(
                SillDisplayMode.DateTime,
                "/WindowSill.Date/Display/DisplayModeDateTime".GetLocalizedString()),
        ];
    }

    private static IReadOnlyList<FormatOptionItem<DateFormat>> BuildDateFormatItems()
    {
        DateTime now = DateTime.Now;
        var items = new List<FormatOptionItem<DateFormat>>
        {
            new(DateFormat.None, "/WindowSill.Date/Display/FormatNone".GetLocalizedString()),
        };

        DateFormat[] formats =
        [
            DateFormat.AbbreviatedDayMonth,
            DateFormat.ShortMonthDay,
            DateFormat.DayShortMonth,
            DateFormat.FullDayMonth,
            DateFormat.MonthSlashDayCompact,
            DateFormat.MonthSlashDay,
            DateFormat.DaySlashMonthCompact,
            DateFormat.DaySlashMonth,
            DateFormat.MonthDayYear,
            DateFormat.DayMonthYear,
            DateFormat.Iso8601,
        ];

        foreach (DateFormat format in formats)
        {
            string preview = format.FormatDate(now);
            string? suffix = format.GetLabelSuffix();
            string displayName = suffix is null ? preview : $"{preview}  {suffix}";
            items.Add(new FormatOptionItem<DateFormat>(format, displayName));
        }

        return items;
    }

    private static IReadOnlyList<FormatOptionItem<TimeFormat>> BuildTimeFormatItems()
    {
        DateTime now = DateTime.Now;
        return
        [
            new FormatOptionItem<TimeFormat>(
                TimeFormat.None,
                "/WindowSill.Date/Display/FormatNone".GetLocalizedString()),
            new FormatOptionItem<TimeFormat>(
                TimeFormat.TwelveHour,
                TimeFormat.TwelveHour.FormatTime(now, showSeconds: false)),
            new FormatOptionItem<TimeFormat>(
                TimeFormat.TwentyFourHour,
                TimeFormat.TwentyFourHour.FormatTime(now, showSeconds: false)),
        ];
    }
}
