using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.UI.Xaml.Media.Imaging;
using WindowSill.API;
using WindowSill.UniversalCommands.Core;
using WindowSill.UniversalCommands.Settings;

namespace WindowSill.UniversalCommands;

[Export(typeof(ISill))]
[Name("Universal Commands Global")]
[Priority(Priority.Lowest)]
[SupportMultipleMonitors(showOnEveryMonitorsByDefault: true)]
internal sealed class UniversalCommandsGlobalSill : ISillActivatedByDefault, ISillListView, IDisposable
{
    private readonly IPluginInfo _pluginInfo;
    private readonly IProcessInteractionService _processInteractionService;
    private readonly UniversalCommandsService _universalCommandsService;
    private readonly DisposableSemaphore _disposableSemaphore = new();

    private CancellationTokenSource? _debounceCts;

    [ImportingConstructor]
    internal UniversalCommandsGlobalSill(
        IPluginInfo pluginInfo,
        IProcessInteractionService processInteractionService,
        UniversalCommandsService universalCommandsService)
    {
        _pluginInfo = pluginInfo;
        _processInteractionService = processInteractionService;
        _universalCommandsService = universalCommandsService;
        _universalCommandsService.Commands.CollectionChanged += Commands_CollectionChanged;
    }

    public string DisplayName => "/WindowSill.UniversalCommands/Misc/GlobalDisplayName".GetLocalizedString();

    public SillSettingsView[]? SettingsViews =>
        [
            new SillSettingsView(
                DisplayName,
                new(() => new SettingsView(_pluginInfo, _universalCommandsService)))
        ];

    public ObservableCollection<SillListViewItem> ViewList { get; } = new();

    public SillView? PlaceholderView => null;

    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "ctrl.svg")))
        };

    public void Dispose()
    {
        _universalCommandsService.Commands.CollectionChanged -= Commands_CollectionChanged;
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _disposableSemaphore.Dispose();
    }

    public ValueTask OnActivatedAsync()
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnDeactivatedAsync()
    {
        return ValueTask.CompletedTask;
    }

    private void Commands_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        CancellationTokenSource cts = _debounceCts = new();
        DebounceUpdateSillsAsync(cts.Token).ForgetSafely();
    }

    private async Task DebounceUpdateSillsAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        await UpdateSillsAsync();
    }

    private async Task UpdateSillsAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            using IDisposable _ = await _disposableSemaphore.WaitAsync(CancellationToken.None);

            ViewList.Clear();

            foreach (UniversalCommand command in _universalCommandsService.Commands)
            {
                if (command.TargetAppProcessName is null)
                {
                    ViewList.Add(UniversalCommandSillHelper.CreateButtonItem(command, _processInteractionService));
                }
            }
        });
    }
}
