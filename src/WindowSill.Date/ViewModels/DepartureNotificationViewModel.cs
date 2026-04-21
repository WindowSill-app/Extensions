using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Windows.System;

using WindowSill.API;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Settings;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// ViewModel for the full-screen departure notification window.
/// Shows location, travel time, and "Open in Maps" action.
/// </summary>
internal sealed partial class DepartureNotificationViewModel : ObservableObject
{
    private readonly Action _closeAction;

    /// <summary>
    /// Initializes a new instance of the <see cref="DepartureNotificationViewModel"/> class.
    /// </summary>
    /// <param name="calendarEvent">The meeting event.</param>
    /// <param name="travelTimeText">The formatted travel time text (e.g., "~25 min travel").</param>
    /// <param name="mapsProvider">The user's preferred maps provider.</param>
    /// <param name="closeAction">Action to close the notification window.</param>
    public DepartureNotificationViewModel(
        CalendarEvent calendarEvent,
        string? travelTimeText,
        MapsProvider mapsProvider,
        Action closeAction)
    {
        _closeAction = closeAction;

        MeetingTitle = calendarEvent.Title;
        Location = calendarEvent.Location ?? string.Empty;
        TravelTimeText = travelTimeText ?? string.Empty;
        HasTravelTime = !string.IsNullOrEmpty(travelTimeText);
        MeetingTimeText = calendarEvent.StartTime.LocalDateTime
            .ToString("h:mm tt", System.Globalization.CultureInfo.CurrentCulture);

        if (!string.IsNullOrWhiteSpace(calendarEvent.Location))
        {
            MapsUrl = mapsProvider.BuildDirectionsUrl(calendarEvent.Location);
        }
    }

    /// <summary>
    /// Gets the meeting title.
    /// </summary>
    public string MeetingTitle { get; }

    /// <summary>
    /// Gets the meeting location address.
    /// </summary>
    public string Location { get; }

    /// <summary>
    /// Gets the travel time display text.
    /// </summary>
    public string TravelTimeText { get; }

    /// <summary>
    /// Gets a value indicating whether travel time info is available.
    /// </summary>
    public bool HasTravelTime { get; }

    /// <summary>
    /// Gets the meeting start time text (e.g., "10:00 AM").
    /// </summary>
    public string MeetingTimeText { get; }

    /// <summary>
    /// Gets the maps directions URL, if a location is available.
    /// </summary>
    public Uri? MapsUrl { get; }

    /// <summary>
    /// Gets a value indicating whether the "Open in Maps" button should be visible.
    /// </summary>
    public bool HasMapsUrl => MapsUrl is not null;

    /// <summary>
    /// Opens the maps app with directions and dismisses the notification.
    /// </summary>
    [RelayCommand]
    private async Task OpenInMapsAndDismissAsync()
    {
        if (MapsUrl is not null)
        {
            await Launcher.LaunchUriAsync(MapsUrl);
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
