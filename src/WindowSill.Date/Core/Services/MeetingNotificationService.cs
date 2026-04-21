using Microsoft.Extensions.Logging;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

using WindowSill.API;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Views;

namespace WindowSill.Date.Core.Services;

/// <summary>
/// Orchestrates showing full-screen meeting notifications across all monitors.
/// </summary>
internal sealed class MeetingNotificationService
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeetingNotificationService"/> class.
    /// </summary>
    internal MeetingNotificationService()
    {
        _logger = this.Log();
    }

    /// <summary>
    /// Shows a full-screen meeting notification on all monitors.
    /// Audio plays on the first monitor only. Dismissing any window closes all.
    /// </summary>
    /// <param name="calendarEvent">The meeting event to notify about.</param>
    /// <param name="playAudio">Whether to play the notification sound.</param>
    internal async Task ShowNotificationAsync(CalendarEvent calendarEvent, bool playAudio = true)
    {
        await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            try
            {
                var monitors = EnumerateMonitors();

                if (monitors.Count == 0)
                {
                    // Fallback to single maximized window.
                    var fallbackWindow = new MeetingNotificationWindow(calendarEvent, playAudio: playAudio);
                    await fallbackWindow.ShowAsync();
                }
                else
                {
                    var windows = new List<MeetingNotificationWindow>();
                    Action<MeetingNotificationWindow> closeAllWindows = (MeetingNotificationWindow closedWindow) =>
                    {
                        foreach (MeetingNotificationWindow window in windows)
                        {
                            if (window != closedWindow)
                            {
                                window.Close();
                            }
                        }
                    };

                    bool isFirstWindow = true;
                    foreach (RECT monitorRect in monitors)
                    {
                        var window = new MeetingNotificationWindow(
                            calendarEvent,
                            monitorRect,
                            closeAllWindows,
                            playAudio: isFirstWindow && playAudio);
                        windows.Add(window);
                        isFirstWindow = false;
                    }

                    Task[] tasks = windows.Select(w => w.ShowAsync()).ToArray();
                    await Task.WhenAny(tasks);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show meeting notification for: {Title}", calendarEvent.Title);
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
