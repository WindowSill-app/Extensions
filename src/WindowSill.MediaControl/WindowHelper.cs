using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WindowSill.MediaControl;

internal static class WindowHelper
{
    internal static void ActivateWindow(nint windowHandle)
    {
        var windowHwnd = new HWND(windowHandle);

        // Check if the window is visible first
        if (PInvoke.IsWindowVisible(windowHwnd))
        {
            // Window is visible, try to activate it directly
            TryActivateWindow(windowHwnd);
            return;
        }

        // If the window is not visible, find the first visible window for the same process
        uint processId;
        unsafe
        {
            PInvoke.GetWindowThreadProcessId(windowHwnd, &processId);
        }

        HWND visibleWindow = FindFirstVisibleWindowForProcess(processId);
        if (visibleWindow != HWND.Null)
        {
            TryActivateWindow(visibleWindow);
        }
    }

    private static bool TryActivateWindow(HWND windowHwnd)
    {
        try
        {
            // Check if the window is minimized and restore it if necessary
            if (PInvoke.IsIconic(windowHwnd))
            {
                PInvoke.ShowWindow(windowHwnd, SHOW_WINDOW_CMD.SW_RESTORE);
            }

            uint threadId1;
            uint threadId2;

            unsafe
            {
                uint processId;
                threadId1 = PInvoke.GetWindowThreadProcessId(PInvoke.GetForegroundWindow(), &processId);
                threadId2 = PInvoke.GetWindowThreadProcessId(windowHwnd, &processId);
            }

            if (threadId1 != threadId2)
            {
                PInvoke.AttachThreadInput(threadId1, threadId2, true);
                bool success = PInvoke.SetForegroundWindow(windowHwnd);
                PInvoke.AttachThreadInput(threadId1, threadId2, false);
                return success;
            }
            else
            {
                return PInvoke.SetForegroundWindow(windowHwnd);
            }
        }
        catch
        {
            return false;
        }
    }

    private static HWND FindFirstVisibleWindowForProcess(uint targetProcessId)
    {
        HWND foundWindow = HWND.Null;

        // Enumerate all windows to find the first visible one for the target process
        unsafe
        {
            PInvoke.EnumWindows((hWnd, lParam) =>
            {
                uint processId;
                PInvoke.GetWindowThreadProcessId(hWnd, &processId);

                // Check if this window belongs to our target process and is visible
                if (processId == targetProcessId && PInvoke.IsWindowVisible(hWnd))
                {
                    foundWindow = hWnd;
                    return false; // Stop enumeration - we found our window
                }

                return true; // Continue enumeration
            }, 0);
        }

        return foundWindow;
    }
}
