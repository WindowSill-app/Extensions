using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Text.Json;
using WindowSill.API;
using Path = System.IO.Path;

namespace WindowSill.AppLauncher.Core;

[Export(typeof(AppGroupService))]
internal sealed class AppGroupService
{
    private readonly DisposableSemaphore _disposableSemaphore = new();
    private readonly string _pluginDataFolder;
    private bool _ignoreAppGroupChanges;

    [ImportingConstructor]
    public AppGroupService(IPluginInfo pluginInfo)
    {
        _pluginDataFolder = pluginInfo.GetPluginDataFolder();
        AppGroups.CollectionChanged += AppGroups_CollectionChanged;
        LoadAsync().ForgetSafely();
    }

    private void AppGroups_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        SaveAsync().ForgetSafely();
    }

    internal ObservableCollection<AppGroup> AppGroups { get; } = new();

    private async Task SaveAsync()
    {
        try
        {
            if (_ignoreAppGroupChanges)
            {
                return;
            }

            using IDisposable _ = await _disposableSemaphore.WaitAsync(CancellationToken.None);
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
                _ignoreAppGroupChanges = true;
                await ThreadHelper.RunOnUIThreadAsync(() =>
                {
                    foreach (AppGroup appGroup in appGroups)
                    {
                        AppGroups.Add(appGroup);
                    }
                });
                _ignoreAppGroupChanges = false;

                // Saving because UwpAppInfo.PackageFullName may have changed.
                await SaveAsync();
            }
        }
    }
}
