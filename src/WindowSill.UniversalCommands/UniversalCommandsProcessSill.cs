using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.UI.Xaml.Media.Imaging;
using WindowSill.API;
using WindowSill.UniversalCommands.Activators;
using WindowSill.UniversalCommands.Core;

namespace WindowSill.UniversalCommands;

[Export(typeof(ISill))]
[Name("Universal Commands Process")]
[Priority(Priority.Lowest)]
[Order(After = "Universal Commands Global")]
internal sealed class UniversalCommandsProcessSill : ISillActivatedByProcess, ISillListView, IDisposable
{
    private readonly IPluginInfo _pluginInfo;
    private readonly IProcessInteractionService _processInteractionService;
    private readonly UniversalCommandsService _universalCommandsService;
    private readonly DisposableSemaphore _disposableSemaphore = new();

    private string? _activeProcessName;
    private WindowInfo? _activeWindow;

    [ImportingConstructor]
    internal UniversalCommandsProcessSill(
        IPluginInfo pluginInfo,
        IProcessInteractionService processInteractionService,
        UniversalCommandsService universalCommandsService)
    {
        _pluginInfo = pluginInfo;
        _processInteractionService = processInteractionService;
        _universalCommandsService = universalCommandsService;
        _universalCommandsService.Commands.CollectionChanged += Commands_CollectionChanged;
    }

    public string DisplayName => "/WindowSill.UniversalCommands/Misc/ProcessDisplayName".GetLocalizedString();

    public ObservableCollection<SillListViewItem> ViewList { get; } = new();

    public SillView? PlaceholderView => null;

    public string[] ProcessActivatorTypeNames => [UniversalCommandsProcessActivator.ActivatorType];

    public SillSettingsView[]? SettingsViews => null;

    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "ctrl.svg")))
        };

    public void Dispose()
    {
        _universalCommandsService.Commands.CollectionChanged -= Commands_CollectionChanged;
        _disposableSemaphore.Dispose();
    }

    public async ValueTask OnActivatedAsync(string processActivatorTypeName, WindowInfo currentProcessWindow)
    {
        string appId = currentProcessWindow.ApplicationIdentifier;
        _activeProcessName = System.IO.Path.GetFileNameWithoutExtension(appId);
        _activeWindow = currentProcessWindow;
        await UpdateSillsAsync();
    }

    public ValueTask OnDeactivatedAsync()
    {
        _activeProcessName = null;
        _activeWindow = null;
        return ValueTask.CompletedTask;
    }

    private void Commands_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateSillsAsync().ForgetSafely();
    }

    private async Task UpdateSillsAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            using IDisposable _ = await _disposableSemaphore.WaitAsync(CancellationToken.None);

            ViewList.Clear();

            foreach (UniversalCommand command in _universalCommandsService.Commands)
            {
                if (command.TargetAppProcessName is not null
                    && string.Equals(command.TargetAppProcessName, _activeProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    ViewList.Add(UniversalCommandSillHelper.CreateButtonItem(command, _processInteractionService, _activeWindow!));
                }
            }
        });
    }
}
