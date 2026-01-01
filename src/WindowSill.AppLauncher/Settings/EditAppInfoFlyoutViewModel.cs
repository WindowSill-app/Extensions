using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowSill.AppLauncher.Core.AppInfo;

namespace WindowSill.AppLauncher.Settings;

internal sealed partial class EditAppInfoFlyoutViewModel : ObservableObject
{
    internal EditAppInfoFlyoutViewModel(AppInfo appInfo)
    {
        AppInfo = appInfo;
    }

    internal AppInfo AppInfo { get; }

    internal bool IsExeApp => AppInfo is ExeAppInfo;

    internal bool IsExeOrShortcutApp => IsExeApp || AppInfo is ShortcutAppInfo;

    internal string Arguments
    {
        get => (AppInfo as ExeAppInfo)?.Arguments ?? string.Empty;
        set
        {
            if (AppInfo is ExeAppInfo exeAppInfo)
            {
                exeAppInfo.Arguments = value;
                OnPropertyChanged();
            }
        }
    }

    internal bool RunAsAdmin
    {
        get => (AppInfo as ShortcutAppInfo)?.AlwaysRunAsAdmin ?? (AppInfo as ExeAppInfo)?.AlwaysRunAsAdmin ?? false;
        set
        {
            if (AppInfo is ShortcutAppInfo shortcutAppInfo)
            {
                shortcutAppInfo.AlwaysRunAsAdmin = value;
                OnPropertyChanged();
            }
            else if (AppInfo is ExeAppInfo exeAppInfo)
            {
                exeAppInfo.AlwaysRunAsAdmin = value;
                OnPropertyChanged();
            }
        }
    }

    [RelayCommand]
    private void Reset()
    {
        AppInfo.DisplayName = AppInfo.DefaultDisplayName;
        AppInfo.OverrideAppIconPath = null;
        Arguments = string.Empty;
        RunAsAdmin = false;
        AppInfo.OnDeserialized();
    }
}
