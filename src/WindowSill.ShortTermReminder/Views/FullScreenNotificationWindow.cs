using CommunityToolkit.Diagnostics;

using Microsoft.UI.Windowing;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

using WindowSill.API;
using WindowSill.ShortTermReminder.Core;
using WindowSill.ShortTermReminder.ViewModels;

using WinUIEx;

namespace WindowSill.ShortTermReminder.Views;

/// <summary>
/// Thin orchestrator that creates a full-screen acrylic window and sets
/// the XAML content for a reminder notification. Manages PInvoke window
/// configuration and lifecycle.
/// </summary>
internal sealed class FullScreenNotificationWindow
{
    private readonly AcrylicWindowFrameworkElement _view;
    private readonly TaskCompletionSource<bool> _windowClosedTaskCompletionSource = new();
    private readonly Action<FullScreenNotificationWindow>? _onWindowClosed;
    private readonly RECT? _monitorRect;

    internal FullScreenNotificationWindow(
        Reminder reminder,
        IReminderService reminderService,
        RECT? monitorRect = null,
        Action<FullScreenNotificationWindow>? onWindowClosed = null,
        bool playAudio = true)
    {
        _monitorRect = monitorRect;
        _onWindowClosed = onWindowClosed;

        _view = new AcrylicWindowFrameworkElement();

        var viewModel = new FullScreenNotificationViewModel(
            reminder,
            reminderService,
            () => _view.UnderlyingWindow.Close());

        var content = new FullScreenNotificationContent(viewModel, playAudio);
        _view.Content = content;

        ConfigureWindow();

        _view.UnderlyingWindow.Closed += UnderlyingWindow_Closed;
    }

    /// <summary>
    /// Shows the window positioned on the target monitor and waits until it is closed.
    /// </summary>
    internal async Task ShowAsync()
    {
        if (_monitorRect.HasValue)
        {
            RECT rect = _monitorRect.Value;
            _view.UnderlyingWindow.MoveAndResize(
                rect.left,
                rect.top,
                rect.right - rect.left,
                rect.bottom - rect.top);
        }
        else
        {
            _view.UnderlyingWindow.Maximize();
        }

        _view.UnderlyingWindow.Show();
        _view.UnderlyingWindow.BringToFront();
        _view.UnderlyingWindow.Activate();
        _view.UnderlyingWindow.SetForegroundWindow();

        await _windowClosedTaskCompletionSource.Task;
    }

    /// <summary>
    /// Closes the notification window.
    /// </summary>
    internal void Close()
    {
        _view.UnderlyingWindow.Close();
    }

    private void ConfigureWindow()
    {
        _view.UnderlyingWindow.IsMinimizable = false;
        _view.UnderlyingWindow.IsMaximizable = false;
        _view.UnderlyingWindow.IsResizable = false;
        _view.UnderlyingWindow.IsTitleBarVisible = false;
        _view.UnderlyingWindow.IsShownInSwitchers = false;
        _view.UnderlyingWindow.IsAlwaysOnTop = true;

        if (_view.UnderlyingWindow.PresenterKind == AppWindowPresenterKind.Overlapped
            && _view.UnderlyingWindow.Presenter is OverlappedPresenter overlappedPresenter)
        {
            overlappedPresenter.SetBorderAndTitleBar(false, false);

            WindowStyle style = _view.UnderlyingWindow.GetWindowStyle();
            style &= ~WindowStyle.DlgFrame;
            _view.UnderlyingWindow.SetWindowStyle(style);
        }

        unsafe
        {
            if (IsWindows11OrGreater())
            {
                uint cornerPreference = (uint)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DONOTROUND;
                Guard.IsTrue(
                    PInvoke.DwmSetWindowAttribute(
                        (HWND)_view.UnderlyingWindow.GetWindowHandle(),
                        DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE,
                        &cornerPreference,
                        sizeof(uint))
                    .Succeeded);
            }

            int renderPolicy = (int)DWMNCRENDERINGPOLICY.DWMNCRP_ENABLED;
            Guard.IsTrue(
                PInvoke.DwmSetWindowAttribute(
                    (HWND)_view.UnderlyingWindow.GetWindowHandle(),
                    DWMWINDOWATTRIBUTE.DWMWA_EXCLUDED_FROM_PEEK,
                    &renderPolicy,
                    sizeof(int))
                .Succeeded);
        }
    }

    private void UnderlyingWindow_Closed(object sender, WindowEventArgs e)
    {
        _windowClosedTaskCompletionSource.TrySetResult(true);
        _onWindowClosed?.Invoke(this);
    }

    private static bool IsWindows11OrGreater()
    {
        return Environment.OSVersion.Version >= new Version(10, 0, 22000);
    }
}
