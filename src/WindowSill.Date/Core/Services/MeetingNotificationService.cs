using System.ComponentModel.Composition;

using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

using WindowSill.API;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Settings;
using WindowSill.Date.ViewModels;
using WindowSill.Date.Views;

namespace WindowSill.Date.Core.Services;

/// <summary>
/// Orchestrates showing full-screen notifications across all monitors.
/// Creates the appropriate content (meeting-start or departure) and delegates
/// to <see cref="FullScreenNotificationWindow"/> for window management.
/// </summary>
[Export]
internal sealed class MeetingNotificationService
{
    private readonly ILogger _logger;

    [ImportingConstructor]
    internal MeetingNotificationService()
    {
        _logger = this.Log();
    }

    /// <summary>
    /// Shows a full-screen meeting-start notification on all monitors.
    /// </summary>
    internal Task ShowNotificationAsync(CalendarEvent calendarEvent, bool playAudio = true)
    {
        return ShowOnAllMonitorsAsync(
            (isFirst, closeWindow) => new MeetingNotificationContent(
                new MeetingNotificationViewModel(calendarEvent, closeWindow),
                playAudio: isFirst && playAudio),
            calendarEvent.Title);
    }

    /// <summary>
    /// Shows a full-screen departure notification on all monitors.
    /// </summary>
    internal Task ShowDepartureNotificationAsync(
        CalendarEvent calendarEvent,
        string? travelTimeText,
        MapsProvider mapsProvider,
        TravelMode travelMode,
        bool playAudio = true)
    {
        return ShowOnAllMonitorsAsync(
            (isFirst, closeWindow) => new DepartureNotificationContent(
                new DepartureNotificationViewModel(calendarEvent, travelTimeText, mapsProvider, travelMode, closeWindow),
                playAudio: isFirst && playAudio),
            calendarEvent.Title);
    }

    /// <summary>
    /// Shows a toast notification for a meeting that is starting.
    /// </summary>
    internal void ShowToastNotification(CalendarEvent calendarEvent)
    {
        try
        {
            var builder = new AppNotificationBuilder()
                .AddText(calendarEvent.Title)
                .AddText(FormatMeetingTime(calendarEvent))
                .SetAudioEvent(AppNotificationSoundEvent.Reminder);

            if (calendarEvent.VideoCall?.JoinUrl is Uri joinUrl)
            {
                builder.AddButton(new AppNotificationButton(
                    calendarEvent.VideoCall.Provider.GetJoinButtonText())
                    .SetInvokeUri(joinUrl));
            }

            AppNotification notification = builder.BuildNotification();
            notification.ExpiresOnReboot = true;
            notification.Priority = AppNotificationPriority.High;

            AppNotificationManager.Default.Show(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show toast for: {Title}", calendarEvent.Title);
        }
    }

    /// <summary>
    /// Shows a toast notification for a departure reminder.
    /// </summary>
    internal void ShowDepartureToastNotification(CalendarEvent calendarEvent, string? travelTimeText)
    {
        try
        {
            string subtitle = travelTimeText is not null
                ? $"{FormatMeetingTime(calendarEvent)} · {travelTimeText}"
                : FormatMeetingTime(calendarEvent);

            AppNotification notification = new AppNotificationBuilder()
                .AddText(calendarEvent.Title)
                .AddText(subtitle)
                .SetAudioEvent(AppNotificationSoundEvent.Reminder)
                .BuildNotification();
            notification.ExpiresOnReboot = true;
            notification.Priority = AppNotificationPriority.High;

            AppNotificationManager.Default.Show(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show departure toast for: {Title}", calendarEvent.Title);
        }
    }

    private static string FormatMeetingTime(CalendarEvent calendarEvent)
    {
        string start = calendarEvent.StartTime.LocalDateTime.ToString("t", System.Globalization.CultureInfo.CurrentCulture);
        string end = calendarEvent.EndTime.LocalDateTime.ToString("t", System.Globalization.CultureInfo.CurrentCulture);
        return $"{start} – {end}";
    }

    /// <summary>
    /// Shows a full-screen notification on every monitor.
    /// Audio plays on the first monitor only. Dismissing any window closes all.
    /// </summary>
    /// <param name="createContent">
    /// Factory: (bool isFirstMonitor, Action closeThisWindow) → UserControl.
    /// </param>
    /// <param name="titleForLogging">Meeting title for error logging.</param>
    private async Task ShowOnAllMonitorsAsync(
        Func<bool, Action, UserControl> createContent,
        string titleForLogging)
    {
        await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            try
            {
                List<RECT> monitors = EnumerateMonitors();
                var windows = new List<FullScreenNotificationWindow>();

                Action<FullScreenNotificationWindow> closeAll = (FullScreenNotificationWindow closed) =>
                {
                    foreach (FullScreenNotificationWindow w in windows)
                    {
                        if (w != closed)
                        {
                            w.Close();
                        }
                    }
                };

                if (monitors.Count == 0)
                {
                    // Capture the window reference so the close action can find it.
                    FullScreenNotificationWindow? windowRef = null;
                    var content = createContent(true, () => windowRef?.Close());
                    windowRef = new FullScreenNotificationWindow(content, onWindowClosed: closeAll);
                    windows.Add(windowRef);
                }
                else
                {
                    bool isFirst = true;
                    foreach (RECT monitorRect in monitors)
                    {
                        FullScreenNotificationWindow? windowRef = null;
                        var content = createContent(isFirst, () => windowRef?.Close());
                        windowRef = new FullScreenNotificationWindow(content, monitorRect, closeAll);
                        windows.Add(windowRef);
                        isFirst = false;
                    }
                }

                Task[] tasks = windows.Select(w => w.ShowAsync()).ToArray();
                await Task.WhenAny(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show notification for: {Title}", titleForLogging);
            }
        });
    }

    private static List<RECT> EnumerateMonitors()
    {
        var monitors = new List<RECT>();
        unsafe
        {
            PInvoke.EnumDisplayMonitors(
                (HDC)default,
                null,
                (HMONITOR hMonitor, HDC hdcMonitor, RECT* lprcMonitor, LPARAM dwData) =>
                {
                    if (lprcMonitor != null)
                    {
                        monitors.Add(*lprcMonitor);
                    }
                    return true;
                },
                (LPARAM)0);
        }
        return monitors;
    }
}