using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FuzzySharp;
using WindowSill.API;
using WindowSill.AppLauncher.Core.AppInfo;

namespace WindowSill.AppLauncher.Settings;

internal sealed partial class AppSelectionFlyoutViewModel : ObservableObject
{
    private CancellationTokenSource? _searchCancellationTokenSource = new();

    public AppSelectionFlyoutViewModel()
    {
        SearchQuery = string.Empty;
    }

    [ObservableProperty]
    internal partial bool IsLoading { get; set; } = true;

    internal ObservableCollection<AppInfo> FilteredAppList { get; } = new();

    [ObservableProperty]
    internal partial string SearchQuery { get; set; }

    partial void OnSearchQueryChanged(string value)
    {
        _searchCancellationTokenSource?.Cancel();
        _searchCancellationTokenSource?.Dispose();
        _searchCancellationTokenSource = new CancellationTokenSource();
        SearchAsync(value, _searchCancellationTokenSource.Token).ForgetSafely();
    }

    private async Task SearchAsync(string searchQuery, CancellationToken cancellationToken)
    {
        IEnumerable<AppInfo> allApps = await GetAllAppsAsync(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        List<AppInfo> filteredAppList;
        if (string.IsNullOrEmpty(searchQuery))
        {
            filteredAppList = [.. allApps];
        }
        else
        {
            filteredAppList = PerformSimpleSearch(searchQuery, allApps);

            cancellationToken.ThrowIfCancellationRequested();

            // Fuzzy search, if we didn't find a result initially.
            if (filteredAppList.Count == 0)
            {
                filteredAppList = PerformFuzzySearch(searchQuery, allApps);
            }
        }

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                IsLoading = false;
                FilteredAppList.SynchronizeWith(filteredAppList);
            }
        });
    }

    private static List<AppInfo> PerformSimpleSearch(string searchQuery, IEnumerable<AppInfo>? apps)
    {
        List<AppInfo> filteredApps = [];
        if (apps is not null)
        {
            foreach (AppInfo app in apps)
            {
                bool match = false;
                if (string.IsNullOrEmpty(searchQuery))
                {
                    match = true;
                }
                else if (app.DefaultDisplayName.Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase))
                {
                    match = true;
                }
                else if (searchQuery.Contains(app.DefaultDisplayName, StringComparison.CurrentCultureIgnoreCase))
                {
                    match = true;
                }

                if (match)
                {
                    filteredApps.Add(app);
                }
            }
        }

        return filteredApps;
    }

    private static List<AppInfo> PerformFuzzySearch(string searchQuery, IEnumerable<AppInfo>? apps)
    {
        List<AppInfo> filteredApps = [];
        if (apps is not null)
        {
            foreach (AppInfo app in apps)
            {
                bool match = false;
                if (Fuzz.TokenInitialismRatio(searchQuery, app.DefaultDisplayName) >= 75)
                {
                    match = true;
                }
                else if (Fuzz.WeightedRatio(searchQuery, app.DefaultDisplayName) >= 50)
                {
                    match = true;
                }

                if (match)
                {
                    filteredApps.Add(app);
                }
            }
        }

        return filteredApps;
    }

    private static async Task<IEnumerable<AppInfo>> GetAllAppsAsync(CancellationToken cancellationToken)
    {
        Task<List<UwpAppInfo>> uwpAppsTask = UwpAppHelper.GetUwpAppsAsync();
        Task<List<ShortcutAppInfo>> startMenuAppsTask = ShortcutHelper.GetStartMenuAppsAsync();

        await Task.WhenAny(Task.WhenAll(uwpAppsTask, startMenuAppsTask), cancellationToken.AsTask());

        var allApps = new List<AppInfo>();
        allApps.AddRange(await uwpAppsTask);
        allApps.AddRange(await startMenuAppsTask);

        return allApps
            .DistinctBy(app => app.DefaultDisplayName)
            .OrderBy(app => app.DefaultDisplayName);
    }
}
