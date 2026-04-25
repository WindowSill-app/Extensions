using CommunityToolkit.Mvvm.ComponentModel;

using WindowSill.API;
using WindowSill.Date.Settings;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// ViewModel for the Meetings settings tab.
/// </summary>
internal sealed partial class MeetingSettingsViewModel : ObservableObject
{
    private readonly ISettingsProvider _settingsProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeetingSettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsProvider">The settings provider.</param>
    public MeetingSettingsViewModel(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
    }

    // ── Upcoming meeting sills ──

    /// <summary>
    /// Gets the available meeting placement options.
    /// </summary>
    public IReadOnlyList<FormatOptionItem<MeetingPlacement>> PlacementOptions { get; } =
    [
        new(MeetingPlacement.BeforeAll, "/WindowSill.Date/Meetings/PlacementBefore".GetLocalizedString()),
        new(MeetingPlacement.AfterAll, "/WindowSill.Date/Meetings/PlacementAfter".GetLocalizedString()),
    ];

    /// <summary>
    /// Gets or sets the selected meeting placement.
    /// </summary>
    public FormatOptionItem<MeetingPlacement>? SelectedPlacement
    {
        get => PlacementOptions.FirstOrDefault(i => i.Value == _settingsProvider.GetSetting(Settings.Settings.MeetingPlacement));
        set
        {
            if (value is not null
                && value.Value != _settingsProvider.GetSetting(Settings.Settings.MeetingPlacement))
            {
                _settingsProvider.SetSetting(Settings.Settings.MeetingPlacement, value.Value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets the available reminder window options in minutes.
    /// </summary>
    public int[] ReminderWindowOptions { get; } = [5, 10, 15, 30, 60];

    /// <summary>
    /// Gets or sets the selected reminder window in minutes.
    /// </summary>
    public int ReminderWindowMinutes
    {
        get => _settingsProvider.GetSetting(Settings.Settings.ReminderWindowMinutes);
        set
        {
            if (value != _settingsProvider.GetSetting(Settings.Settings.ReminderWindowMinutes))
            {
                _settingsProvider.SetSetting(Settings.Settings.ReminderWindowMinutes, value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of meeting sill items.
    /// </summary>
    public int MaxMeetingSills
    {
        get => _settingsProvider.GetSetting(Settings.Settings.MaxMeetingSills);
        set => _settingsProvider.SetSetting(Settings.Settings.MaxMeetingSills, value);
    }

    /// <summary>
    /// Gets or sets whether all-day events are shown as meeting sill items.
    /// </summary>
    public bool ShowAllDayMeetings
    {
        get => _settingsProvider.GetSetting(Settings.Settings.ShowAllDayMeetings);
        set => _settingsProvider.SetSetting(Settings.Settings.ShowAllDayMeetings, value);
    }

    // ── Notifications ──

    /// <summary>
    /// Gets the notification mode options for the combo box.
    /// </summary>
    public IReadOnlyList<FormatOptionItem<NotificationMode>> NotificationModeOptions { get; } =
    [
        new(NotificationMode.None, "/WindowSill.Date/Meetings/NotificationModeNone".GetLocalizedString()),
        new(NotificationMode.FullScreen, "/WindowSill.Date/Meetings/NotificationModeFullScreen".GetLocalizedString()),
        new(NotificationMode.Toast, "/WindowSill.Date/Meetings/NotificationModeToast".GetLocalizedString()),
    ];

    /// <summary>
    /// Gets or sets the notification mode for confirmed meetings.
    /// </summary>
    public FormatOptionItem<NotificationMode>? SelectedConfirmedNotificationMode
    {
        get => NotificationModeOptions.FirstOrDefault(i => i.Value == _settingsProvider.GetSetting(Settings.Settings.ConfirmedNotificationMode));
        set
        {
            if (value is not null
                && value.Value != _settingsProvider.GetSetting(Settings.Settings.ConfirmedNotificationMode))
            {
                _settingsProvider.SetSetting(Settings.Settings.ConfirmedNotificationMode, value.Value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the notification mode for tentative / not-responded meetings.
    /// </summary>
    public FormatOptionItem<NotificationMode>? SelectedTentativeNotificationMode
    {
        get => NotificationModeOptions.FirstOrDefault(i => i.Value == _settingsProvider.GetSetting(Settings.Settings.TentativeNotificationMode));
        set
        {
            if (value is not null
                && value.Value != _settingsProvider.GetSetting(Settings.Settings.TentativeNotificationMode))
            {
                _settingsProvider.SetSetting(Settings.Settings.TentativeNotificationMode, value.Value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether sill flashing is enabled.
    /// </summary>
    public bool EnableSillFlashing
    {
        get => _settingsProvider.GetSetting(Settings.Settings.EnableSillFlashing);
        set => _settingsProvider.SetSetting(Settings.Settings.EnableSillFlashing, value);
    }

    /// <summary>
    /// Gets or sets whether the Join button is shown.
    /// </summary>
    public bool ShowJoinButton
    {
        get => _settingsProvider.GetSetting(Settings.Settings.ShowJoinButton);
        set => _settingsProvider.SetSetting(Settings.Settings.ShowJoinButton, value);
    }

    // ── Travel time ──

    /// <summary>
    /// Gets or sets whether travel time estimation is enabled.
    /// </summary>
    public bool EnableTravelTime
    {
        get => _settingsProvider.GetSetting(Settings.Settings.EnableTravelTime);
        set
        {
            if (value != _settingsProvider.GetSetting(Settings.Settings.EnableTravelTime))
            {
                _settingsProvider.SetSetting(Settings.Settings.EnableTravelTime, value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets the available travel mode options with localized display names.
    /// </summary>
    public IReadOnlyList<FormatOptionItem<TravelMode>> TravelModeOptions { get; } =
    [
        new(TravelMode.Driving, "/WindowSill.Date/Meetings/TravelModeDriving".GetLocalizedString()),
        new(TravelMode.Walking, "/WindowSill.Date/Meetings/TravelModeWalking".GetLocalizedString()),
        new(TravelMode.Cycling, "/WindowSill.Date/Meetings/TravelModeCycling".GetLocalizedString()),
    ];

    /// <summary>
    /// Gets or sets the selected travel mode.
    /// </summary>
    public FormatOptionItem<TravelMode>? SelectedTravelMode
    {
        get => TravelModeOptions.FirstOrDefault(i => i.Value == _settingsProvider.GetSetting(Settings.Settings.TravelMode));
        set
        {
            if (value is not null
                && value.Value != _settingsProvider.GetSetting(Settings.Settings.TravelMode))
            {
                _settingsProvider.SetSetting(Settings.Settings.TravelMode, value.Value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the departure buffer in minutes (extra time to get ready).
    /// </summary>
    public int DepartureBufferMinutes
    {
        get => _settingsProvider.GetSetting(Settings.Settings.DepartureBufferMinutes);
        set => _settingsProvider.SetSetting(Settings.Settings.DepartureBufferMinutes, value);
    }

    /// <summary>
    /// Gets the available maps provider options with localized display names.
    /// </summary>
    public IReadOnlyList<FormatOptionItem<MapsProvider>> MapsProviderOptions { get; } =
    [
        new(MapsProvider.GoogleMaps, "/WindowSill.Date/Meetings/MapsProviderGoogle".GetLocalizedString()),
        new(MapsProvider.BingMaps, "/WindowSill.Date/Meetings/MapsProviderBing".GetLocalizedString()),
        new(MapsProvider.AppleMaps, "/WindowSill.Date/Meetings/MapsProviderApple".GetLocalizedString()),
    ];

    /// <summary>
    /// Gets or sets the selected maps provider item.
    /// </summary>
    public FormatOptionItem<MapsProvider>? SelectedMapsProvider
    {
        get => MapsProviderOptions.FirstOrDefault(i => i.Value == _settingsProvider.GetSetting(Settings.Settings.PreferredMapsProvider));
        set
        {
            if (value is not null
                && value.Value != _settingsProvider.GetSetting(Settings.Settings.PreferredMapsProvider))
            {
                _settingsProvider.SetSetting(Settings.Settings.PreferredMapsProvider, value.Value);
                OnPropertyChanged();
            }
        }
    }
}
