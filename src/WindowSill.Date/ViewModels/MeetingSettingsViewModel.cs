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
    /// Gets the available poll interval options in seconds.
    /// </summary>
    public int[] PollIntervalOptions { get; } = [15, 30, 60, 120, 300];

    /// <summary>
    /// Gets or sets the poll interval in seconds.
    /// </summary>
    public int PollIntervalSeconds
    {
        get => _settingsProvider.GetSetting(Settings.Settings.MeetingPollIntervalSeconds);
        set
        {
            if (value != _settingsProvider.GetSetting(Settings.Settings.MeetingPollIntervalSeconds))
            {
                _settingsProvider.SetSetting(Settings.Settings.MeetingPollIntervalSeconds, value);
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
    /// Gets or sets the fallback commute time in minutes.
    /// </summary>
    public int FallbackCommuteMinutes
    {
        get => _settingsProvider.GetSetting(Settings.Settings.FallbackCommuteMinutes);
        set => _settingsProvider.SetSetting(Settings.Settings.FallbackCommuteMinutes, value);
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
    /// Gets the available maps provider options.
    /// </summary>
    public MapsProvider[] MapsProviderOptions { get; } =
        [MapsProvider.GoogleMaps, MapsProvider.BingMaps, MapsProvider.AppleMaps];

    /// <summary>
    /// Gets or sets the preferred maps provider.
    /// </summary>
    public MapsProvider PreferredMapsProvider
    {
        get => _settingsProvider.GetSetting(Settings.Settings.PreferredMapsProvider);
        set
        {
            if (value != _settingsProvider.GetSetting(Settings.Settings.PreferredMapsProvider))
            {
                _settingsProvider.SetSetting(Settings.Settings.PreferredMapsProvider, value);
                OnPropertyChanged();
            }
        }
    }
}
