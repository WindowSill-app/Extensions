using Microsoft.Extensions.Logging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;
using WindowSill.API;

namespace WindowSill.ClipboardHistory.Core;

/// <summary>
/// Detects whether a window handle belongs to File Explorer, the Desktop shell, or a File Dialog.
/// Also tracks the last non-self foreground window via a system event hook.
/// </summary>
internal static class ExplorerDetector
{
    private const string ExplorerClassName = "CabinetWClass";
    private const string ProgmanClassName = "Progman";
    private const string WorkerWClassName = "WorkerW";
    private const string DialogClassName = "#32770";
    private const string ShellDefViewClassName = "SHELLDLL_DefView";

    private static readonly uint currentProcessId = PInvoke.GetCurrentProcessId();
    private static long lastNonSelfForegroundWindow;
    private static HWINEVENTHOOK eventHook;
    private static int subscriberCount;

    // Must keep a reference to prevent GC from collecting the delegate
    private static readonly WINEVENTPROC winEventDelegate = WinEventProc;

    /// <summary>
    /// Starts tracking foreground window changes. Call when the extension activates.
    /// Must be called so the hook is registered on the UI thread (which has a message pump).
    /// Reference-counted — safe to call multiple times.
    /// </summary>
    internal static async ValueTask StartTrackingAsync()
    {
        if (Interlocked.Increment(ref subscriberCount) == 1)
        {
            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                // Seed with current foreground if it's not ours
                HWND current = PInvoke.GetForegroundWindow();
                if (current != HWND.Null && !IsOwnWindow(current))
                {
                    Interlocked.Exchange(ref lastNonSelfForegroundWindow, (long)(nint)current);
                }

                unsafe
                {
                    eventHook = PInvoke.SetWinEventHook(
                        0x0003, // EVENT_SYSTEM_FOREGROUND
                        0x0003,
                        HMODULE.Null,
                        winEventDelegate,
                        0,
                        0,
                        0x0000 | 0x0002); // WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS
                }
            });
        }
    }

    /// <summary>
    /// Stops tracking foreground window changes. Call when the extension deactivates.
    /// </summary>
    internal static void StopTracking()
    {
        if (Interlocked.Decrement(ref subscriberCount) == 0)
        {
            if (!eventHook.IsNull)
            {
                PInvoke.UnhookWinEvent(eventHook);
                eventHook = default;
            }
        }
    }

    /// <summary>
    /// Returns the last foreground window that was NOT owned by our process.
    /// This is the window the user was interacting with before clicking the WindowSill bar.
    /// Returns <c>0</c> if no such window has been observed.
    /// </summary>
    internal static nint GetLastActiveWindow()
    {
        HWND foreground = PInvoke.GetForegroundWindow();

        // If foreground is not ours (global shortcut case), return it directly
        if (foreground != HWND.Null && !IsOwnWindow(foreground))
        {
            return foreground;
        }

        // Return the tracked last non-self foreground window
        return (nint)Interlocked.Read(ref lastNonSelfForegroundWindow);
    }

    /// <summary>
    /// Returns <c>true</c> if the given window (or its root ancestor) is an Explorer window,
    /// the Desktop, or a File Dialog containing a shell view.
    /// </summary>
    internal static bool IsExplorerLikeWindow(nint hwnd)
    {
        if (hwnd == 0)
        {
            return false;
        }

        return IsExplorerWindow(hwnd) || IsDesktopWindow(hwnd) || IsFileDialogWindow(hwnd);
    }

    /// <summary>
    /// Returns <c>true</c> if the root ancestor of <paramref name="hwnd"/> has class name <c>CabinetWClass</c>.
    /// </summary>
    internal static bool IsExplorerWindow(nint hwnd)
    {
        HWND root = PInvoke.GetAncestor(new HWND(hwnd), GET_ANCESTOR_FLAGS.GA_ROOT);
        return GetClassName(root) == ExplorerClassName;
    }

    /// <summary>
    /// Returns <c>true</c> if the root ancestor is the Desktop shell (<c>Progman</c> or <c>WorkerW</c>).
    /// </summary>
    internal static bool IsDesktopWindow(nint hwnd)
    {
        HWND root = PInvoke.GetAncestor(new HWND(hwnd), GET_ANCESTOR_FLAGS.GA_ROOT);
        string className = GetClassName(root);
        if (className is ProgmanClassName or WorkerWClassName)
        {
            return true;
        }

        // Sometimes the desktop view is parented under a WorkerW
        HWND parent = PInvoke.GetParent(root);
        if (parent != HWND.Null)
        {
            string parentClass = GetClassName(parent);
            if (parentClass is ProgmanClassName or WorkerWClassName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns <c>true</c> if the root ancestor is a <c>#32770</c> dialog containing a <c>SHELLDLL_DefView</c> child.
    /// This pattern matches File Open/Save dialogs.
    /// </summary>
    internal static bool IsFileDialogWindow(nint hwnd)
    {
        HWND root = PInvoke.GetAncestor(new HWND(hwnd), GET_ANCESTOR_FLAGS.GA_ROOT);
        string className = GetClassName(root);
        if (className != DialogClassName)
        {
            return false;
        }

        return HasShellDefViewChild(root);
    }

    private static void WinEventProc(
        HWINEVENTHOOK hWinEventHook,
        uint @event,
        HWND hwnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint dwmsEventTime)
    {
        if (hwnd != HWND.Null && !IsOwnWindow(hwnd))
        {
            Interlocked.Exchange(ref lastNonSelfForegroundWindow, (long)(nint)hwnd);
        }
    }

    private static unsafe bool IsOwnWindow(HWND hwnd)
    {
        uint processId;
        PInvoke.GetWindowThreadProcessId(hwnd, &processId);
        return processId == currentProcessId;
    }

    private static bool HasShellDefViewChild(HWND parent)
    {
        bool found = false;
        PInvoke.EnumChildWindows(parent, (hwnd, _) =>
        {
            if (GetClassName(hwnd) == ShellDefViewClassName)
            {
                found = true;
                return false; // stop enumeration
            }
            return true; // continue
        }, 0);
        return found;
    }

    private static string GetClassName(HWND hwnd)
    {
        unsafe
        {
            char* buffer = stackalloc char[256];
            int length = PInvoke.GetClassName(hwnd, buffer, 256);
            return length > 0 ? new string(buffer, 0, length) : string.Empty;
        }
    }
}
