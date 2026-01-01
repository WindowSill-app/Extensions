using WindowSill.API;

namespace WindowSill.PerfCounter;

internal static class TaskManagerLauncher
{
    internal static void OpenTaskManager(ISettingsProvider settingsProvider)
    {
        // Check if Task Manager launch is enabled
        if (!settingsProvider.GetSetting(Settings.Settings.EnableTaskManagerLaunch))
        {
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "taskmgr.exe",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open Task Manager: {ex.Message}");
        }
    }
}
