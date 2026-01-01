using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Text.Json;
using WindowSill.API;
using Path = System.IO.Path;

namespace WindowSill.AppLauncher.Core;

[Export(typeof(AppGroupService))]
internal sealed class AppGroupService
{
    private readonly string _pluginDataFolder;

    [ImportingConstructor]
    public AppGroupService(IPluginInfo pluginInfo)
    {
        _pluginDataFolder = pluginInfo.GetPluginDataFolder();
        LoadAsync().ForgetSafely();
    }

    internal ObservableCollection<AppGroup> AppGroups { get; } = new();

    internal async Task SaveAsync()
    {
        try
        {
            string appGroupsFilePath = Path.Combine(_pluginDataFolder, "app_groups.json");
            using FileStream fileStream = File.Create(appGroupsFilePath);
            await JsonSerializer.SerializeAsync(fileStream, AppGroups.ToArray());
        }
        catch
        {
        }
    }

    private async Task LoadAsync()
    {
        string appGroupsFilePath = Path.Combine(_pluginDataFolder, "app_groups.json");
        if (File.Exists(appGroupsFilePath))
        {
            using FileStream fileStream = File.OpenRead(appGroupsFilePath);
            AppGroup[]? appGroups = await JsonSerializer.DeserializeAsync<AppGroup[]>(fileStream);
            if (appGroups is not null)
            {
                await ThreadHelper.RunOnUIThreadAsync(() =>
                {
                    foreach (AppGroup appGroup in appGroups)
                    {
                        AppGroups.Add(appGroup);
                    }
                });

                // Saving because UwpAppInfo.PackageFullName may have changed.
                await SaveAsync();
            }
        }
    }
}
