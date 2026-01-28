using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using CommunityToolkit.Diagnostics;
using Microsoft.UI.Xaml.Media.Imaging;
using WindowSill.API;
using WindowSill.AppLauncher.Core;
using WindowSill.AppLauncher.Settings;

namespace WindowSill.AppLauncher;

[Export(typeof(ISill))]
[Name(SillInternalName)]
[Priority(Priority.Highest)]
[HideIconInSillListView]
[SupportMultipleMonitors(showOnEveryMonitorsByDefault: true)]
public sealed class AppLauncherSill : ISillActivatedByDefault, ISillListView
{
    internal const string SillInternalName = "WindowSill.AppLauncherSill";

    private readonly Uri _iconPath;
    private readonly IPluginInfo _pluginInfo;
    private readonly AppGroupService _appGroupService;
    private readonly ISettingsProvider _settingsProvider;
    private readonly DisposableSemaphore _disposableSemaphore = new();

    [ImportingConstructor]
    internal AppLauncherSill(AppGroupService appGroupService, IPluginInfo pluginInfo, ISettingsProvider settingsProvider)
    {
        _appGroupService = appGroupService;
        _pluginInfo = pluginInfo;
        _settingsProvider = settingsProvider;

        _iconPath = new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "appLauncher.svg"));

        UpdateSillsAsync().ForgetSafely();
        _appGroupService.AppGroups.CollectionChanged += AppGroups_CollectionChanged;
    }

    public string DisplayName => "/WindowSill.AppLauncher/Misc/DisplayName".GetLocalizedString();

    public ObservableCollection<SillListViewItem> ViewList { get; } = new();

    public SillView? PlaceholderView => throw new NotImplementedException();

    public SillSettingsView[]? SettingsViews =>
        [
        new SillSettingsView(
            "/WindowSill.AppLauncher/Misc/DisplayName".GetLocalizedString(),
            new(() => new SettingsView(_iconPath, _appGroupService)))
        ];

    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(_iconPath)
        };

    public ValueTask OnActivatedAsync()
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnDeactivatedAsync()
    {
        return ValueTask.CompletedTask;
    }

    private void AppGroups_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateSillsAsync().ForgetSafely();
    }

    private async Task UpdateSillsAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            using IDisposable _ = await _disposableSemaphore.WaitAsync(CancellationToken.None);

            ViewList.Clear();
            AppGroup[] groups = _appGroupService.AppGroups.ToArray();
            for (int i = 0; i < groups.Length; i++)
            {
                AppGroup group = groups[i];
                group.GroupIcon.Reset();
                Guard.IsNotNull(group.GroupIcon.Task);

                if (group.Items.Count == 1)
                {
                    ViewList.Add(
                        new SillListViewButtonItem(
                            new ImageIcon()
                                .Source((await group.GroupIcon.Task)!),
                            group.GroupName,
                            async () => await group.Items[0].LaunchAsync(asAdmin: false)));
                }
                else
                {
                    ViewList.Add(
                        new SillListViewPopupItem(
                            new ImageIcon()
                                .Source((await group.GroupIcon.Task)!),
                            group.GroupName,
                            new AppGroupSillPopupContent(group, _settingsProvider)));
                }
            }
        });
    }
}
