using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Windows.System;

using WindowSill.API;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// ViewModel for the full-screen meeting notification window.
/// Provides dismiss and join actions.
/// </summary>
internal sealed partial class MeetingNotificationViewModel : ObservableObject
{
    private readonly Action _closeAction;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeetingNotificationViewModel"/> class.
    /// </summary>
    /// <param name="calendarEvent">The meeting event that triggered the notification.</param>
    /// <param name="closeAction">The action to close the notification window.</param>
    public MeetingNotificationViewModel(CalendarEvent calendarEvent, Action closeAction)
    {
        _closeAction = closeAction;

        MeetingTitle = calendarEvent.Title;
        TimeText = string.Format(
            "{0} – {1}",
            calendarEvent.StartTime.LocalDateTime.ToString("h:mm tt", System.Globalization.CultureInfo.CurrentCulture),
            calendarEvent.EndTime.LocalDateTime.ToString("h:mm tt", System.Globalization.CultureInfo.CurrentCulture));
        HasVideoCall = calendarEvent.VideoCall is not null;
        VideoCallUrl = calendarEvent.VideoCall?.JoinUrl;
        VideoCallProviderName = calendarEvent.VideoCall?.Provider.ToString();
    }

    /// <summary>
    /// Gets the meeting title.
    /// </summary>
    [ObservableProperty]
    internal partial string MeetingTitle { get; set; }

    /// <summary>
    /// Gets the formatted time range text.
    /// </summary>
    public string TimeText { get; }

    /// <summary>
    /// Gets a value indicating whether this meeting has a video call link.
    /// </summary>
    public bool HasVideoCall { get; }

    /// <summary>
    /// Gets the video call URL, if available.
    /// </summary>
    public Uri? VideoCallUrl { get; }

    /// <summary>
    /// Gets the video call provider name (e.g., "MicrosoftTeams"), if available.
    /// </summary>
    public string? VideoCallProviderName { get; }

    /// <summary>
    /// Joins the video call and dismisses the notification.
    /// </summary>
    [RelayCommand]
    private async Task JoinAndDismissAsync()
    {
        if (VideoCallUrl is not null)
        {
            await Launcher.LaunchUriAsync(VideoCallUrl);
        }

        _closeAction();
    }

    /// <summary>
    /// Dismisses the notification.
    /// </summary>
    [RelayCommand]
    private void Dismiss()
    {
        _closeAction();
    }
}
