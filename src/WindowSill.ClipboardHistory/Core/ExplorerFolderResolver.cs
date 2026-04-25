using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Extensions.Logging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using WindowSill.API;

namespace WindowSill.ClipboardHistory.Core;

/// <summary>
/// Resolves the current folder path from an Explorer, Desktop, or File Dialog window handle.
/// Uses CsWin32-generated <see cref="IShellWindows"/> and <see cref="IWebBrowser2"/> for Explorer,
/// and manual COM interop for File Dialogs via the Running Object Table.
/// </summary>
internal static class ExplorerFolderResolver
{
    private static readonly ILogger logger = typeof(ExplorerFolderResolver).Log();
    private static Guid shellBrowserGuid = new("000214E2-0000-0000-C000-000000000046");

    /// <summary>
    /// Returns the file system folder path currently shown in the given window,
    /// or <c>null</c> if the folder cannot be resolved (e.g., virtual folders).
    /// </summary>
    internal static string? GetCurrentFolderPath(nint hwnd)
    {
        if (hwnd == 0)
        {
            return null;
        }

        try
        {
            if (ExplorerDetector.IsDesktopWindow(hwnd))
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            }

            HWND root = PInvoke.GetAncestor(new HWND(hwnd), GET_ANCESTOR_FLAGS.GA_ROOT);
            if (root == HWND.Null)
            {
                root = new HWND(hwnd);
            }

            string? path = GetPathViaShellWindows(root);
            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }

            path = GetPathViaFileDialog(root);
            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resolve folder path for window handle {Hwnd}.", hwnd);
            return null;
        }
    }

    /// <summary>
    /// Enumerates Shell windows via CsWin32 <see cref="IShellWindows"/> / <see cref="IWebBrowser2"/>
    /// to find an Explorer window matching the given top-level handle, then uses <c>LocationURL</c>
    /// to retrieve the current folder path. Handles multi-tab Explorer via <c>ShellTabWindowClass</c>.
    /// </summary>
    private static unsafe string? GetPathViaShellWindows(HWND topLevel)
    {
        var comObjects = new HashSet<object?>();
        string? result = null;

        try
        {
            var clsidShellWindows = new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39");
            Guid iidIShellWindows = typeof(IShellWindows).GUID;

            HRESULT hr = PInvoke.CoCreateInstance(
                &clsidShellWindows,
                null,
                CLSCTX.CLSCTX_ALL,
                &iidIShellWindows,
                out object? shellWindowsObj);

            if (hr.Failed || shellWindowsObj is null)
            {
                return null;
            }

            var shellWindows = (IShellWindows)shellWindowsObj;
            try
            {
                // First, find the explorer window matching our top-level handle
                int count = shellWindows.Count;
                bool found = false;
                for (int i = 0; i < count && !found; i++)
                {
                    object? item = shellWindows.Item(i);
                    IWebBrowser2? explorer = null;
                    try
                    {
                        explorer = item as IWebBrowser2;
                        if (explorer is not null && explorer.HWND == (nint)topLevel)
                        {
                            found = true;

                            // Find active tab handle for multi-tab Explorer
                            HWND activeTab = PInvoke.FindWindowEx(topLevel, HWND.Null, "ShellTabWindowClass", null);
                            if (!activeTab.IsNull)
                            {
                                // Multi-tab: find the IWebBrowser2 for the active tab via IShellBrowser.GetWindow
                                IWebBrowser2? tabBrowser = FindBrowserByTabHandle(comObjects, shellWindows, activeTab);
                                if (tabBrowser is not null)
                                {
                                    result = GetLocation(tabBrowser);
                                    break;
                                }
                            }

                            // Single tab or fallback
                            result = GetLocation(explorer);
                        }
                    }
                    catch
                    {
                        // Ignored — COM calls may fail for certain shell windows
                    }
                    finally
                    {
                        comObjects.Add(explorer);
                    }
                }
            }
            finally
            {
                comObjects.Add(shellWindows);
            }
        }
        catch (COMException ex)
        {
            logger.LogDebug(ex, "IShellWindows COM call failed.");
        }
        finally
        {
            foreach (object? comObject in comObjects)
            {
                ReleaseComObject(comObject);
            }
        }

        return result;
    }

    /// <summary>
    /// Finds the <see cref="IWebBrowser2"/> whose <see cref="IShellBrowser"/> window handle
    /// matches the given Explorer tab handle.
    /// </summary>
    private static IWebBrowser2? FindBrowserByTabHandle(HashSet<object?> comObjects, IShellWindows shellWindows, HWND tabHandle)
    {
        int count = shellWindows.Count;
        for (int i = 0; i < count; i++)
        {
            object? item = shellWindows.Item(i);
            IWebBrowser2? explorer = null;
            IComServiceProvider? sp = null;
            IShellBrowserManual? shellBrowser = null;
            try
            {
                explorer = item as IWebBrowser2;
                sp = explorer as IComServiceProvider;
                if (explorer is not null && sp is not null)
                {
                    sp.QueryService(ref shellBrowserGuid, ref shellBrowserGuid, out shellBrowser);
                    if (shellBrowser is not null)
                    {
                        shellBrowser.GetWindow(out nint hWnd);
                        if (hWnd == tabHandle)
                        {
                            return explorer;
                        }
                    }
                }
            }
            catch
            {
                // Ignored — COM calls may fail for certain shell windows
            }
            finally
            {
                comObjects.Add(sp);
                comObjects.Add(explorer);
                comObjects.Add(shellBrowser);
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the file system path from an <see cref="IWebBrowser2"/> via its <c>LocationURL</c>.
    /// </summary>
    private static string? GetLocation(IWebBrowser2 browser)
    {
        string locationUrl = browser.LocationURL.ToString();
        if (string.IsNullOrWhiteSpace(locationUrl))
        {
            return null;
        }

        return NormalizeLocation(locationUrl);
    }

    /// <summary>
    /// Normalizes a <c>file:///</c> URL into a local file system path.
    /// Returns <c>null</c> for non-filesystem locations (shell folders, etc.).
    /// </summary>
    private static string? NormalizeLocation(string location)
    {
        if (location.Contains('%'))
        {
            location = Uri.UnescapeDataString(location);
        }

        if (location.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
        {
            location = location[8..];
        }
        else if (location.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            // UNC paths: file://server/share → \\server\share
            location = @"\\" + location[7..];
        }
        else if (location.StartsWith("::", StringComparison.Ordinal)
            || location.StartsWith("{", StringComparison.Ordinal))
        {
            // Virtual/shell folder — not a filesystem path
            return null;
        }
        else if (!System.IO.Path.IsPathRooted(location))
        {
            return null;
        }

        location = location.Trim(' ', '/', '\\', '\n', '\'', '"');
        location = location.Replace('/', '\\');

        return System.IO.Directory.Exists(location) ? location : null;
    }

    /// <summary>
    /// Queries the Running Object Table (ROT) for <c>IFileDialog</c> instances
    /// whose window matches the given top-level handle, then retrieves the current folder.
    /// </summary>
    private static string? GetPathViaFileDialog(HWND topLevel)
    {
        try
        {
            int hr = Ole32GetRunningObjectTable(0, out IRunningObjectTable? rot);
            if (hr < 0 || rot is null)
            {
                return null;
            }

            rot.EnumRunning(out IEnumMoniker? enumMoniker);
            if (enumMoniker is null)
            {
                return null;
            }

            var monikers = new IMoniker[1];
            while (enumMoniker.Next(1, monikers, nint.Zero) == 0)
            {
                rot.GetObject(monikers[0], out object obj);
                if (obj is not IFileDialog fileDialog)
                {
                    continue;
                }

                try
                {
                    if (obj is not IOleWindow oleWindow)
                    {
                        continue;
                    }

                    oleWindow.GetWindow(out nint dlgHwndRaw);
                    HWND dlgHwnd = new(dlgHwndRaw);
                    HWND dlgRoot = PInvoke.GetAncestor(dlgHwnd, GET_ANCESTOR_FLAGS.GA_ROOT);
                    HWND targetRoot = PInvoke.GetAncestor(topLevel, GET_ANCESTOR_FLAGS.GA_ROOT);

                    if (dlgHwnd != topLevel && dlgRoot != topLevel && targetRoot != dlgHwnd)
                    {
                        continue;
                    }

                    hr = fileDialog.GetFolder(out nint shellItemPtr);
                    if (hr < 0 || shellItemPtr == 0)
                    {
                        continue;
                    }

                    try
                    {
                        var shellItem = (IShellItemManual)Marshal.GetObjectForIUnknown(shellItemPtr);
                        hr = shellItem.GetDisplayName(unchecked((int)0x80058000) /* SIGDN_FILESYSPATH */, out nint displayNamePtr);
                        if (hr >= 0 && displayNamePtr != 0)
                        {
                            try
                            {
                                string? path = Marshal.PtrToStringUni(displayNamePtr);
                                return string.IsNullOrEmpty(path) ? null : path;
                            }
                            finally
                            {
                                Marshal.FreeCoTaskMem(displayNamePtr);
                            }
                        }
                    }
                    finally
                    {
                        Marshal.Release(shellItemPtr);
                    }
                }
                finally
                {
                    if (obj is not null)
                    {
                        Marshal.ReleaseComObject(obj);
                    }
                }
            }
        }
        catch (COMException ex)
        {
            logger.LogDebug(ex, "IFileDialog ROT query failed.");
        }

        return null;
    }

    private static void ReleaseComObject(object? comObject)
    {
        if (comObject is not null && Marshal.IsComObject(comObject))
        {
            Marshal.FinalReleaseComObject(comObject);
        }
    }

    [DllImport("ole32.dll", EntryPoint = "GetRunningObjectTable")]
    private static extern int Ole32GetRunningObjectTable(uint reserved, out IRunningObjectTable pprot);

    #region COM Interface Definitions (for types not generated by CsWin32)

    /// <summary>
    /// COM <c>IServiceProvider</c> (not <see cref="System.IServiceProvider"/>).
    /// Used to obtain <see cref="IShellBrowserManual"/> from <see cref="IWebBrowser2"/>.
    /// </summary>
    [ComImport]
    [Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IComServiceProvider
    {
        [PreserveSig]
        int QueryService(ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IShellBrowserManual ppvObject);
    }

    /// <summary>
    /// Minimal COM <c>IShellBrowser</c> definition — only <c>GetWindow</c> is needed.
    /// </summary>
    [ComImport]
    [Guid("000214E2-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellBrowserManual
    {
        [PreserveSig]
        int GetWindow(out nint handle);
    }

    [ComImport]
    [Guid("42F85136-DB7E-439C-85F1-E4075D135FC8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileDialog
    {
        [PreserveSig]
        int Show(nint parent);
        [PreserveSig]
        int SetFileTypes(uint cFileTypes, nint rgFilterSpec);
        [PreserveSig]
        int SetFileTypeIndex(uint iFileType);
        [PreserveSig]
        int GetFileTypeIndex(out uint piFileType);
        [PreserveSig]
        int Advise(nint pfde, out uint pdwCookie);
        [PreserveSig]
        int Unadvise(uint dwCookie);
        [PreserveSig]
        int SetOptions(uint fos);
        [PreserveSig]
        int GetOptions(out uint pfos);
        [PreserveSig]
        int SetDefaultFolder(nint psi);
        [PreserveSig]
        int SetFolder(nint psi);
        [PreserveSig]
        int GetFolder(out nint ppsi);
    }

    [ComImport]
    [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemManual
    {
        [PreserveSig]
        int BindToHandler(nint pbc, ref Guid bhid, ref Guid riid, out nint ppv);
        [PreserveSig]
        int GetParent(out nint ppsi);
        [PreserveSig]
        int GetDisplayName(int sigdnName, out nint ppszName);
        [PreserveSig]
        int GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        [PreserveSig]
        int Compare(nint psi, uint hint, out int piOrder);
    }

    [ComImport]
    [Guid("00000114-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IOleWindow
    {
        [PreserveSig]
        int GetWindow(out nint phwnd);
        [PreserveSig]
        int ContextSensitiveHelp([MarshalAs(UnmanagedType.Bool)] bool fEnterMode);
    }

    #endregion
}
