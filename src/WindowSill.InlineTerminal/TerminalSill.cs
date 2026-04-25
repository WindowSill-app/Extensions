using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using WindowSill.API;
using WindowSill.API.Core.Threading;
using WindowSill.InlineTerminal.Activators;
using WindowSill.InlineTerminal.Core.UI;
using WindowSill.InlineTerminal.Models;
using WindowSill.InlineTerminal.Services;
using WindowSill.InlineTerminal.Settings;
using WindowSill.InlineTerminal.Views;
using Path = System.IO.Path;

namespace WindowSill.InlineTerminal;

/// <summary>
/// Entry point for the Inline Terminal extension.
/// Manages dynamic sills (from text selection/drop) and pinned sills (executed commands).
/// </summary>
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
    private readonly CommandService _commandService;
    private readonly AsyncLazy<SillListViewPopupItem?> _createOnGoingCommandsPopup;

    // Pinned sills: commands that have been executed at least once.
    // These survive text selection changes and are only removed when all runs are dismissed.
    private readonly Dictionary<Guid, SillListViewPopupItem> _pinnedSills = [];

    // Track which sills are "dynamic" (created from current selection) so we can clean them up.
    private readonly List<SillListViewPopupItem> _dynamicSills = [];

    private bool _isDynamicallyActivated;
    private bool _ignoreRebuildViewList;
    private WindowTextSelection? _currentWindowTextSelection;
    private string[]? _currentDroppedFiles;

    [ImportingConstructor]
    public TerminalSill(IPluginInfo pluginInfo, ISettingsProvider settingsProvider, SillFactory sillFactory, CommandService commandService)
    {
        _pluginInfo = pluginInfo;
        _settingsProvider = settingsProvider;
        _sillFactory = sillFactory;
        _commandService = commandService;
        _commandService.RunsChanged += CommandService_RunsChanged;
        _commandService.CommandRemoved += CommandService_CommandRemoved;

        PluginAssetHelper.BaseDirectory = pluginInfo.GetPluginContentDirectory();

        _createOnGoingCommandsPopup = new AsyncLazy<SillListViewPopupItem?>(async () => await _sillFactory.CreateOnGoingCommandsPopupAsync());
    }

    /// <inheritdoc />
    public string DisplayName => "/WindowSill.InlineTerminal/TerminalSill/DisplayName".GetLocalizedString();

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
                ClearDynamicSills();
                await RebuildViewListFromCurrentStateAsync();
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
            ClearDynamicSills();
            await RebuildViewListFromCurrentStateAsync();
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

        ClearDynamicSills();

        // Create new dynamic sills from current selection/drop.
        WindowTextSelection? currentWindowTextSelection = _currentWindowTextSelection;
        string[]? currentDroppedFiles = _currentDroppedFiles;

        if (currentWindowTextSelection is not null)
        {
            List<SillListViewPopupItem> commandSills = await _sillFactory.CreateSillsFromSelectedTextAsync(currentWindowTextSelection);
            foreach (SillListViewPopupItem sill in commandSills)
            {
                // Skip if a pinned sill already exists for the same command text.
                if (sill.PopupContent is CommandPopup commandPopup
                    && IsDuplicateOfPinnedCommand(commandPopup.ViewModel.Command))
                {
                    DisposeUnpinnedSill(sill);
                    continue;
                }

                _dynamicSills.Add(sill);
            }
        }
        else if (currentDroppedFiles is not null)
        {
            for (int i = 0; i < currentDroppedFiles.Length; i++)
            {
                SillListViewPopupItem? sill = await _sillFactory.CreateSillFromScriptFilePathAsync(currentDroppedFiles[i]);
                if (sill is not null)
                {
                    // Skip if a pinned sill already exists for the same script file.
                    if (sill.PopupContent is CommandPopup commandPopup
                        && IsDuplicateOfPinnedCommand(commandPopup.ViewModel.Command))
                    {
                        DisposeUnpinnedSill(sill);
                        continue;
                    }

                    _dynamicSills.Add(sill);
                }
            }
        }

        await RebuildViewListFromCurrentStateAsync();

        lock (_lock)
        {
            _ignoreRebuildViewList = false;
        }
    }

    /// <summary>
    /// Reconstructs ViewList from: OnGoingCommands + PinnedSills + DynamicSills.
    /// </summary>
    private async Task RebuildViewListFromCurrentStateAsync()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        ViewList.Clear();

        // 1. On-going commands (if any runs exist).
        if (_commandService.GetAllActiveRuns().Count > 0)
        {
            SillListViewPopupItem? onGoingCommandsSill = await _createOnGoingCommandsPopup.GetValueAsync();
            if (onGoingCommandsSill is not null)
            {
                ViewList.Add(onGoingCommandsSill);
            }
        }

        // 2. Pinned sills (executed commands).
        foreach (SillListViewPopupItem pinnedSill in _pinnedSills.Values)
        {
            ViewList.Add(pinnedSill);
        }

        // 3. Dynamic sills (from current selection/drop).
        // Skip dynamic sills that are already pinned (same command).
        foreach (SillListViewPopupItem dynamicSill in _dynamicSills)
        {
            if (!ViewList.Contains(dynamicSill))
            {
                ViewList.Add(dynamicSill);
            }
        }
    }

    private void ClearDynamicSills()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        foreach (SillListViewPopupItem sill in _dynamicSills)
        {
            // Don't dispose pinned sills.
            if (!_pinnedSills.ContainsValue(sill))
            {
                DisposeUnpinnedSill(sill);
            }
        }

        _dynamicSills.Clear();
    }

    /// <summary>
    /// Returns true if a pinned sill already exists with the same script text or script file path.
    /// </summary>
    private bool IsDuplicateOfPinnedCommand(CommandDefinition newCommand)
    {
        foreach (SillListViewPopupItem pinnedSill in _pinnedSills.Values)
        {
            if (pinnedSill.PopupContent is CommandPopup pinnedPopup)
            {
                CommandDefinition pinnedCommand = pinnedPopup.ViewModel.Command;

                // Match by script file path.
                if (!string.IsNullOrEmpty(newCommand.ScriptFilePath)
                    && string.Equals(newCommand.ScriptFilePath, pinnedCommand.ScriptFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Match by script text.
                if (!string.IsNullOrEmpty(newCommand.Script)
                    && string.Equals(newCommand.Script, pinnedCommand.Script, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void DisposeUnpinnedSill(SillListViewPopupItem sill)
    {
        if (sill.Content is IDisposable disposable)
        {
            disposable.Dispose();
        }

        if (sill.PopupContent is IDisposable disposablePopup)
        {
            disposablePopup.Dispose();
        }
    }

    private void CommandService_RunsChanged(object? sender, EventArgs e)
    {
        ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            // Pin any dynamic sill whose command has been executed.
            for (int i = _dynamicSills.Count - 1; i >= 0; i--)
            {
                SillListViewPopupItem sill = _dynamicSills[i];
                if (sill.PopupContent is CommandPopup commandPopup
                    && commandPopup.ViewModel.Command.HasBeenExecuted
                    && !_pinnedSills.ContainsKey(commandPopup.ViewModel.CommandId))
                {
                    _pinnedSills[commandPopup.ViewModel.CommandId] = sill;
                }
            }

            await RebuildViewListFromCurrentStateAsync();
        }).ForgetSafely();
    }

    private void CommandService_CommandRemoved(object? sender, Guid commandId)
    {
        ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            if (_pinnedSills.Remove(commandId, out SillListViewPopupItem? sill))
            {
                DisposeUnpinnedSill(sill);
            }

            await RebuildViewListFromCurrentStateAsync();
        }).ForgetSafely();
    }
}
