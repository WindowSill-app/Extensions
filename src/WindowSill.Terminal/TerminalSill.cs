using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using WindowSill.API;
using WindowSill.Terminal.Activators;
using WindowSill.Terminal.Core.Commands;
using WindowSill.Terminal.Sill;
using Path = System.IO.Path;

namespace WindowSill.Terminal;

[Export(typeof(ISill))]
[Name("Terminal")]
[SupportMultipleMonitors(true)]
internal sealed class TerminalSill
    : ISillActivatedByTextSelection,
    ISillActivatedByDragAndDrop,
    ISillActivatedByDefault,
    ISillListView
{
    private readonly Lock _lock = new();
    private readonly IPluginInfo _pluginInfo;
    private readonly SillFactory _sillFactory;
    private readonly CommandExecutionService _commandExecutionService;

    private bool _isDynamicallyActivated;
    private bool _ignoreRebuildViewList;
    private WindowTextSelection? _currentWindowTextSelection;
    private string[]? _currentDroppedFiles;

    [ImportingConstructor]
    public TerminalSill(IPluginInfo pluginInfo, SillFactory sillFactory, CommandExecutionService commandExecutionService)
    {
        _pluginInfo = pluginInfo;
        _sillFactory = sillFactory;
        _commandExecutionService = commandExecutionService;
        _commandExecutionService.BackgroundRunnersRemoved += CommandExecutionService_BackgroundRunnersChanged;
    }

    /// <inheritdoc />
    public string DisplayName => "Terminal";

    /// <inheritdoc />
    public string[] TextSelectionActivatorTypeNames => [CommandSelectionActivator.ActivatorName];

    /// <inheritdoc />
    public string[] DragAndDropActivatorTypeNames => [ScriptFileDropActivator.ActivatorName];

    /// <inheritdoc />
    public SillSettingsView[]? SettingsViews => null;

    /// <inheritdoc />
    public ObservableCollection<SillListViewItem> ViewList { get; } = [];

    /// <inheritdoc />
    public SillView? PlaceholderView => null;

    /// <inheritdoc />
    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "terminal.svg")))
        };

    /// <inheritdoc />
    public async ValueTask OnActivatedAsync(string textSelectionActivatorTypeName, WindowTextSelection currentSelection)
    {
        await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            _isDynamicallyActivated = true;
            _currentWindowTextSelection = currentSelection;
            _currentDroppedFiles = null;
            await RebuildViewListAsync();
        });
    }

    /// <inheritdoc />
    public async ValueTask OnActivatedAsync(string dragAndDropActivatorTypeName, DataPackageView dataPackageView)
    {
        try
        {
            var droppedCompatibleFiles = new List<string>();

            if (dataPackageView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> storageItems = await dataPackageView.GetStorageItemsAsync();

                for (int i = 0; i < storageItems.Count; i++)
                {
                    if (storageItems[i] is IStorageFile storageFile)
                    {
                        try
                        {
                            if (ScriptFileDropActivator.SupportedExtensions.Contains(storageFile.FileType)
                                && File.Exists(storageFile.Path))
                            {
                                droppedCompatibleFiles.Add(storageFile.Path);
                            }
                        }
                        catch (Exception)
                        {
                            // Path too long or other error; skip this file.
                            continue;
                        }
                    }
                }
            }

            await ThreadHelper.RunOnUIThreadAsync(async () =>
            {
                _isDynamicallyActivated = true;
                _currentWindowTextSelection = null;
                _currentDroppedFiles = droppedCompatibleFiles.ToArray();
                await RebuildViewListAsync();
            });
        }
        catch
        {
            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                _isDynamicallyActivated = true;
                _currentWindowTextSelection = null;
                _currentDroppedFiles = null;
                ClearViewListAndAddOngoingCommands();
            });
        }
    }

    /// <inheritdoc />
    public async ValueTask OnActivatedAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            if (!_isDynamicallyActivated)
            {
                _currentWindowTextSelection = null;
                _currentDroppedFiles = null;
                await RebuildViewListAsync();
            }
        });
    }

    /// <inheritdoc />
    public async ValueTask OnDeactivatedAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            _isDynamicallyActivated = false;
            _currentWindowTextSelection = null;
            _currentDroppedFiles = null;
            ClearViewListAndAddOngoingCommands();
        });
    }

    private async Task RebuildViewListAsync()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (_ignoreRebuildViewList)
        {
            return;
        }

        lock (_lock)
        {
            if (_ignoreRebuildViewList)
            {
                return;
            }

            _ignoreRebuildViewList = true;
        }

        ClearViewListAndAddOngoingCommands();

        WindowTextSelection? currentWindowTextSelection = _currentWindowTextSelection;
        string[]? currentDroppedFiles = _currentDroppedFiles;
        if (currentWindowTextSelection is not null)
        {
            SillListViewMenuFlyoutItem? commandSelectionSill = await _sillFactory.CreateSillFromSelectedTextAsync(currentWindowTextSelection);
            if (commandSelectionSill is not null)
            {
                ViewList.Add(commandSelectionSill);
            }
        }
        else if (currentDroppedFiles is not null)
        {
            for (int i = 0; i < currentDroppedFiles.Length; i++)
            {
                string filePath = currentDroppedFiles[i];
                SillListViewMenuFlyoutItem? scriptFileDropSill = await _sillFactory.CreateSillFromScriptFilePathAsync(filePath);
                if (scriptFileDropSill is not null)
                {
                    ViewList.Add(scriptFileDropSill);
                }
            }
        }

        _ignoreRebuildViewList = false;
    }

    private void ClearViewListAndAddOngoingCommands()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        for (int i = 0; i < ViewList.Count; i++)
        {
            if (ViewList[i].Content is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        ViewList.Clear();

        IReadOnlyList<CommandRunner> backgroundRunners = _commandExecutionService.GetBackgroundRunners();
        for (int i = 0; i < backgroundRunners.Count; i++)
        {
            CommandRunner runner = backgroundRunners[i];
            SillListViewMenuFlyoutItem commandExecutionSill = _sillFactory.CreateSillFromCommandRunner(runner);
            ViewList.Add(commandExecutionSill);
        }
    }

    private void CommandExecutionService_BackgroundRunnersChanged(object? sender, EventArgs e)
    {
        ThreadHelper.RunOnUIThreadAsync(RebuildViewListAsync);
    }
}
