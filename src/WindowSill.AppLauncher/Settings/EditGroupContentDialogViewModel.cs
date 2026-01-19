using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Storage;
using WindowSill.AppLauncher.Core;
using WindowSill.AppLauncher.Core.AppInfo;
using Path = System.IO.Path;

namespace WindowSill.AppLauncher.Settings;

internal sealed partial class EditGroupContentDialogViewModel : ObservableObject
{
    private readonly ContentDialog _contentDialog;

    internal EditGroupContentDialogViewModel(ContentDialog contentDialog, AppGroup appGroup)
    {
        _contentDialog = contentDialog;

        GroupName = appGroup.GroupName;
        OverrideGroupIconPath = appGroup.OverrideGroupIconPath;
        Items = new ObservableCollection<AppInfo>(appGroup.Items);

        UpdateIcon();
        Items.CollectionChanged += Items_CollectionChanged;
        _contentDialog.IsPrimaryButtonEnabled = Items.Count > 0;
    }

    internal ObservableCollection<AppInfo> Items { get; }

    [ObservableProperty]
    internal partial string GroupName { get; set; } = string.Empty;

    [ObservableProperty]
    internal partial TaskCompletionNotifier<ImageSource?>? GroupIcon { get; set; }

    [ObservableProperty]
    internal partial string? OverrideGroupIconPath { get; set; }

    internal void AddAppInfo(AppInfo appInfo)
    {
        if (!Items.Contains(appInfo))
        {
            Items.Insert(0, appInfo);
        }
    }

    internal void AddFile(StorageFile file)
    {
        if (Path.GetExtension(file.Path).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            ShortcutAppInfo? shortcutAppInfo = ShortcutHelper.GetShortcutAppInfo(file.Path);
            if (shortcutAppInfo != null)
            {
                AddAppInfo(shortcutAppInfo);
            }
        }
        else if (Path.GetExtension(file.Path).Equals(".exe", StringComparison.OrdinalIgnoreCase))
        {
            ExeAppInfo? exeAppInfo = ExeAppHelper.GetExeAppInfo(file.Path);
            if (exeAppInfo != null)
            {
                AddAppInfo(exeAppInfo);
            }
        }
        else
        {
            var fileAppInfo = new FileAppInfo
            {
                DefaultDisplayName = Path.GetFileNameWithoutExtension(file.Path),
                DisplayName = Path.GetFileNameWithoutExtension(file.Path),
                FilePath = file.Path,
            };
            fileAppInfo.OnDeserialized();
            AddAppInfo(fileAppInfo);
        }
    }

    internal void AddFolder(StorageFolder folder)
    {
        var folderAppInfo = new FolderAppInfo
        {
            DefaultDisplayName = Path.GetFileNameWithoutExtension(folder.Path),
            DisplayName = Path.GetFileNameWithoutExtension(folder.Path),
            FolderPath = folder.Path,
        };
        folderAppInfo.OnDeserialized();
        AddAppInfo(folderAppInfo);
    }

    internal void AddUrl(string url, string displayName)
    {
        var urlAppInfo = new UrlAppInfo
        {
            DefaultDisplayName = displayName,
            DisplayName = displayName,
            Url = url,
        };
        urlAppInfo.OnDeserialized();
        AddAppInfo(urlAppInfo);
    }

    internal void UpdateIcon()
    {
        if (string.IsNullOrWhiteSpace(OverrideGroupIconPath) || !File.Exists(OverrideGroupIconPath))
        {
            GroupIcon = new TaskCompletionNotifier<ImageSource?>(() => IconHelper.CreateGridIconAsync(Items.ToArray(), selectedSize: 2), runTaskImmediately: false);
        }
        else
        {
            GroupIcon = new TaskCompletionNotifier<ImageSource?>(() => IconHelper.GetIconFromFileOrFolderAsync(OverrideGroupIconPath), runTaskImmediately: false);
        }
    }

    internal AppGroup ToAppGroup()
    {
        string groupName = GroupName;
        if (string.IsNullOrEmpty(groupName))
        {
            groupName = string.Join(", ", Items.Select(app => app.DisplayName));
            if (groupName.Length > 75)
            {
                groupName = groupName[..75] + "...";
            }
        }

        var appGroup = new AppGroup
        {
            GroupName = groupName,
            OverrideGroupIconPath = OverrideGroupIconPath,
            Items = Items.ToList(),
        };
        appGroup.OnDeserialized();
        return appGroup;
    }

    private void Items_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateIcon();
        _contentDialog.IsPrimaryButtonEnabled = Items.Count > 0;
    }
}
