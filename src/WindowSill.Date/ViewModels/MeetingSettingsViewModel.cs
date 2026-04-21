using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WindowSill.API;
using WindowSill.Date.Core.Services;
using WindowSill.Date.Settings;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// ViewModel for the Meetings settings tab.
/// </summary>
internal sealed partial class MeetingSettingsViewModel : ObservableObject
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly MeetingStateService? _meetingStateService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeetingSettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsProvider">The settings provider.</param>
    /// <param name="meetingStateService">The shared meeting state service for on-demand sync.</param>
    public MeetingSettingsViewModel(ISettingsProvider settingsProvider, MeetingStateService? meetingStateService = null)
    {
        _settingsProvider = settingsProvider;
        _meetingStateService = meetingStateService;
    }

    // ── Upcoming meeting sills ──

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
    /// Gets or sets whether full-screen notifications are enabled.
    /// </summary>
    public bool EnableFullScreenNotification
    {
        get => _settingsProvider.GetSetting(Settings.Settings.EnableFullScreenNotification);
        set => _settingsProvider.SetSetting(Settings.Settings.EnableFullScreenNotification, value);
    }

    /// <summary>
    /// Gets or sets whether toast notifications are enabled.
    /// </summary>
    public bool EnableToastNotification
    {
        get => _settingsProvider.GetSetting(Settings.Settings.EnableToastNotification);
        set => _settingsProvider.SetSetting(Settings.Settings.EnableToastNotification, value);
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

    // ── Sync ──

    /// <summary>
    /// Gets the available poll interval options.
    /// </summary>
    public IReadOnlyList<FormatOptionItem<int>> PollIntervalOptions { get; } =
    [
        new(60, "/WindowSill.Date/Meetings/PollInterval1Min".GetLocalizedString()),
        new(180, "/WindowSill.Date/Meetings/PollInterval3Min".GetLocalizedString()),
        new(300, "/WindowSill.Date/Meetings/PollInterval5Min".GetLocalizedString()),
        new(900, "/WindowSill.Date/Meetings/PollInterval15Min".GetLocalizedString()),
        new(1800, "/WindowSill.Date/Meetings/PollInterval30Min".GetLocalizedString()),
        new(3600, "/WindowSill.Date/Meetings/PollInterval1Hour".GetLocalizedString()),
        new(7200, "/WindowSill.Date/Meetings/PollInterval2Hours".GetLocalizedString()),
    ];

    /// <summary>
    /// Gets or sets the selected poll interval item.
    /// </summary>
    public FormatOptionItem<int>? SelectedPollInterval
    {
        get => PollIntervalOptions.FirstOrDefault(i => i.Value == _settingsProvider.GetSetting(Settings.Settings.MeetingPollIntervalSeconds));
        set
        {
            if (value is not null
                && value.Value != _settingsProvider.GetSetting(Settings.Settings.MeetingPollIntervalSeconds))
            {
                _settingsProvider.SetSetting(Settings.Settings.MeetingPollIntervalSeconds, value.Value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Triggers an immediate refresh of upcoming meetings.
    /// </summary>
    [RelayCommand]
    private void SyncNow()
    {
        _meetingStateService?.RequestRefresh();
    }

    // ── Travel time ──

    /// <summary>
    /// Gets or sets the OpenRouteService API key.
    /// </summary>
    public string OpenRouteServiceApiKey
    {
        get => _settingsProvider.GetSetting(Settings.Settings.OpenRouteServiceApiKey);
        set => _settingsProvider.SetSetting(Settings.Settings.OpenRouteServiceApiKey, value);
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
