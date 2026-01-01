using CommunityToolkit.Diagnostics;
using Path = System.IO.Path;

namespace WindowSill.AppLauncher.Core.AppInfo;

internal static class ExeAppHelper
{
    internal static ExeAppInfo? GetExeAppInfo(string exeFilePath)
    {
        Guard.IsNotNullOrEmpty(exeFilePath);
        if (!File.Exists(exeFilePath))
        {
            return null;
        }

        var exeAppInfo = new ExeAppInfo
        {
            DefaultDisplayName = Path.GetFileNameWithoutExtension(exeFilePath),
            DisplayName = Path.GetFileNameWithoutExtension(exeFilePath),
            ExeFilePath = exeFilePath,
        };
        exeAppInfo.OnDeserialized();
        return exeAppInfo;
    }
}
