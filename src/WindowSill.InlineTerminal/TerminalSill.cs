using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using WindowSill.API;
using WindowSill.API.Core.Threading;
using WindowSill.InlineTerminal.Activators;
using WindowSill.InlineTerminal.Core;
using WindowSill.InlineTerminal.Core.Commands;
using WindowSill.InlineTerminal.Core.UI;
using WindowSill.InlineTerminal.Settings;
using WindowSill.InlineTerminal.Views;
using Path = System.IO.Path;

namespace WindowSill.InlineTerminal;

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
    private readonly ISettingsProvider _settingsProvider;
    private readonly SillFactory _sillFactory;
    private readonly CommandExecutionService _commandExecutionService;
    private readonly AsyncLazy<SillListViewPopupItem?> _createOnGoingCommandsPopup;

    private bool _isDynamicallyActivated;
    private bool _ignoreRebuildViewList;
    private WindowTextSelection? _currentWindowTextSelection;
    private string[]? _currentDroppedFiles;

    [ImportingConstructor]
    public TerminalSill(IPluginInfo pluginInfo, ISettingsProvider settingsProvider, SillFactory sillFactory, CommandExecutionService commandExecutionService)
    {
        _pluginInfo = pluginInfo;
        _settingsProvider = settingsProvider;
        _sillFactory = sillFactory;
        _commandExecutionService = commandExecutionService;
        _commandExecutionService.RunnersChanged += CommandExecutionService_RunnersChanged;
        _commandExecutionService.RunnerDestroyed += CommandExecutionService_RunnerDestroyed;

        PluginAssetHelper.BaseDirectory = pluginInfo.GetPluginContentDirectory();

        _createOnGoingCommandsPopup = new AsyncLazy<SillListViewPopupItem?>(async () => await _sillFactory.CreateOnGoingCommandsPopupAsync());
    }

    /// <inheritdoc />
    public string DisplayName => "Terminal";

    /// <inheritdoc />
    public string[] TextSelectionActivatorTypeNames => [CommandSelectionActivator.ActivatorName];

    /// <inheritdoc />
    public string[] DragAndDropActivatorTypeNames => [ScriptFileDropActivator.ActivatorName];

    /// <inheritdoc />
    public SillSettingsView[]? SettingsViews =>
        [
        new SillSettingsView(
            DisplayName,
            new(() => new SettingsView(_settingsProvider)))
        ];

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
            await ThreadHelper.RunOnUIThreadAsync(async () =>
            {
                _isDynamicallyActivated = true;
                _currentWindowTextSelection = null;
                _currentDroppedFiles = null;
                ClearViewList();
                await InsertOrRemoveOnGoingCommandsAsync();
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
        await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            _isDynamicallyActivated = false;
            _currentWindowTextSelection = null;
            _currentDroppedFiles = null;
            ClearViewList();
            await InsertOrRemoveOnGoingCommandsAsync();
        });
    }

    private async Task RebuildViewListAsync()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        lock (_lock)
        {
            if (_ignoreRebuildViewList)
            {
                return;
            }

            _ignoreRebuildViewList = true;
        }

        ClearViewList();
        await InsertOrRemoveOnGoingCommandsAsync();

        WindowTextSelection? currentWindowTextSelection = _currentWindowTextSelection;
        string[]? currentDroppedFiles = _currentDroppedFiles;
        if (currentWindowTextSelection is not null)
        {
            List<SillListViewPopupItem> commandSelectionSills = await _sillFactory.CreateSillsFromSelectedTextAsync(currentWindowTextSelection);
            foreach (SillListViewPopupItem sill in commandSelectionSills)
            {
                ViewList.Add(sill);
            }
        }
        else if (currentDroppedFiles is not null)
        {
            for (int i = 0; i < currentDroppedFiles.Length; i++)
            {
                string filePath = currentDroppedFiles[i];
                SillListViewPopupItem? scriptFileDropSill = await _sillFactory.CreateSillFromScriptFilePathAsync(filePath);
                if (scriptFileDropSill is not null)
                {
                    ViewList.Add(scriptFileDropSill);
                }
            }
        }

        lock (_lock)
        {
            _ignoreRebuildViewList = false;
        }
    }

    private async Task InsertOrRemoveOnGoingCommandsAsync()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        SillListViewPopupItem? onGoingCommandsSill = await _createOnGoingCommandsPopup.GetValueAsync();
        if (onGoingCommandsSill is not null)
        {
            if (_commandExecutionService.GetStartedRunners().Count > 0)
            {
                if (!ViewList.Contains(onGoingCommandsSill))
                {
                    ViewList.Insert(0, onGoingCommandsSill);
                }
            }
            else
            {
                if (ViewList.Contains(onGoingCommandsSill))
                {
                    ViewList.Remove(onGoingCommandsSill);
                }
            }
        }
    }

    private void ClearViewList()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        for (int i = 0; i < ViewList.Count; i++)
        {
            if (ViewList[i].Content is IDisposable disposable)
            {
                disposable.Dispose();
            }

            if (ViewList[i] is SillListViewPopupItem popup && popup.PopupContent is IDisposable disposablePopup)
            {
                disposablePopup.Dispose();
            }
        }

        ViewList.Clear();
    }

    private void CommandExecutionService_RunnersChanged(object? sender, EventArgs e)
    {
        ThreadHelper.RunOnUIThreadAsync(InsertOrRemoveOnGoingCommandsAsync).ForgetSafely();
    }

    private void CommandExecutionService_RunnerDestroyed(object? sender, Guid e)
    {
        ThreadHelper.RunOnUIThreadAsync(() =>
        {
            for (int i = 0; i < ViewList.Count; i++)
            {
                if (ViewList[i] is SillListViewPopupItem popup
                    && popup.PopupContent is CommandPopup commandPopup
                    && commandPopup.ViewModel.Id == e)
                {
                    ViewList.RemoveAt(i);
                    break;
                }
            }
        }).ForgetSafely();
    }
}
