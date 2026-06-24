using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using WindowSill.API;
using WindowSill.ClipboardHistory.Core;
using WindowSill.ClipboardHistory.Factories;
using WindowSill.ClipboardHistory.FirstTimeSetup;
using WindowSill.ClipboardHistory.Services;
using WindowSill.ClipboardHistory.Settings;
using WindowSill.ClipboardHistory.ViewModels;
using WindowSill.ClipboardHistory.Views;

namespace WindowSill.ClipboardHistory;

[Export(typeof(ISill))]
[Name("Clipboard History")]
[Priority(Priority.Low)]
[HideIconInSillListView]
[SupportMultipleMonitors(showOnEveryMonitorsByDefault: true)]
public sealed partial class ClipboardHistorySill : ObservableObject, ISillActivatedByDefault, ISillFirstTimeSetup, ISillListView
{
    private readonly ILogger _logger;
    private readonly IPluginInfo _pluginInfo;
    private readonly ISettingsProvider _settingsProvider;
    private readonly ClipboardHistoryDataService _clipboardDataService;
    private readonly PinnedClipboardService _pinnedService;
    private readonly ClipboardItemViewFactory _viewFactory;

    private ClipboardHistoryPopupViewModel? _popupViewModel;
    private SillListViewMenuFlyoutItem? _iconItem;

    [ImportingConstructor]
    internal ClipboardHistorySill(
        IProcessInteractionService processInteractionService,
        ISettingsProvider settingsProvider,
        IPluginInfo pluginInfo,
        ClipboardHistoryDataService clipboardDataService,
        PinnedClipboardService pinnedService)
    {
        _logger = this.Log();
        _pluginInfo = pluginInfo;
        _settingsProvider = settingsProvider;
        _clipboardDataService = clipboardDataService;
        _pinnedService = pinnedService;
        _viewFactory = new ClipboardItemViewFactory(_pluginInfo, settingsProvider, processInteractionService, pinnedService);
        PlaceholderView = _viewFactory.CreatePlaceholderView();
    }

    public string DisplayName => "/WindowSill.ClipboardHistory/Misc/DisplayName".GetLocalizedString();

    private bool IsCompactMode => _settingsProvider.GetSetting(Settings.Settings.CompactMode);

    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "clipboard.svg")))
        };

    public SillSettingsView[]? SettingsViews =>
        [
        new SillSettingsView(
            DisplayName,
            new(() => new SettingsView(_settingsProvider)))
        ];

    public ObservableCollection<SillListViewItem> ViewList { get; } = new();

    public SillView? PlaceholderView { get; }

    public IFirstTimeSetupContributor[] GetFirstTimeSetupContributors()
    {
        if (_clipboardDataService.IsHistoryEnabled)
        {
            return [];
        }

        return [new ClipboardHistoryFirstTimeSetupContributor()];
    }

    public async ValueTask OnActivatedAsync()
    {
        _settingsProvider.SettingChanged += SettingsProvider_SettingChanged;
        _clipboardDataService.DataUpdated += ClipboardDataService_DataUpdated;
        _pinnedService.PinsChanged += PinnedService_PinsChanged;
        _clipboardDataService.Subscribe();
        await ExplorerDetector.StartTrackingAsync();

        await _pinnedService.LoadAsync();
        await RefreshClipboardDataAsync();
    }

    public ValueTask OnDeactivatedAsync()
    {
        ExplorerDetector.StopTracking();
        _settingsProvider.SettingChanged -= SettingsProvider_SettingChanged;
        _clipboardDataService.DataUpdated -= ClipboardDataService_DataUpdated;
        _pinnedService.PinsChanged -= PinnedService_PinsChanged;
        _clipboardDataService.Unsubscribe();
        _popupViewModel = null;
        return ValueTask.CompletedTask;
    }

    private void PinnedService_PinsChanged(object? sender, EventArgs e)
    {
        RefreshClipboardDataAsync().Forget();
    }

    private void SettingsProvider_SettingChanged(ISettingsProvider sender, SettingChangedEventArgs args)
    {
        if (args.SettingName == Settings.Settings.MaximumHistoryCount.Name)
        {
            RefreshClipboardDataAsync().Forget();
        }
        else if (args.SettingName == Settings.Settings.HidePasswords.Name)
        {
            ViewList.Clear();
            _popupViewModel = null;
            _clipboardDataService.ClearCache();
            RefreshClipboardDataAsync().Forget();
        }
        else if (args.SettingName == Settings.Settings.CompactMode.Name)
        {
            ViewList.Clear();
            _popupViewModel = null;
            RefreshClipboardDataAsync().Forget();
        }
    }

    private void ClipboardDataService_DataUpdated(object? sender, EventArgs e)
    {
        RefreshClipboardDataAsync().Forget();
    }

    private async Task RefreshClipboardDataAsync()
    {
        int maxItems = _settingsProvider.GetSetting(Settings.Settings.MaximumHistoryCount);
        await _clipboardDataService.RefreshAsync(maxItems);

        if (IsCompactMode)
        {
            await UpdatePopupViewModelAsync();
        }
        else
        {
            await UpdateViewListAsync();
        }
    }

    /// <summary>
    /// Combines pinned items (shown first) with the live clipboard history, excluding any
    /// live items whose content is already pinned.
    /// </summary>
    private IReadOnlyList<ClipboardItemData> GetCombinedItems()
    {
        IReadOnlyList<ClipboardItemData> pinned = _pinnedService.GetPinnedItems();
        var pinnedSignatures = new HashSet<string>(pinned.Select(p => p.ContentSignature), StringComparer.Ordinal);

        IEnumerable<ClipboardItemData> live = _clipboardDataService.GetCachedItems()
            .Where(i => !pinnedSignatures.Contains(i.ContentSignature));

        return [.. pinned, .. live];
    }

    /// <summary>
    /// Updates the popup ViewModel's items collection for compact mode.
    /// Creates the popup SillListViewPopupItem on first call.
    /// </summary>
    private async Task UpdatePopupViewModelAsync()
    {
        IReadOnlyList<ClipboardItemData> cachedItems = GetCombinedItems();

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            if (_popupViewModel is null)
            {
                _popupViewModel = new ClipboardHistoryPopupViewModel();
                var popupContent = new ClipboardHistoryPopupContent(_popupViewModel);
                var popupItem = new SillListViewPopupItem(
                    new ImageIcon
                    {
                        Source = new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "clipboard.svg")))
                    },
                    DisplayName,
                    popupContent);

                ViewList.Clear();
                ViewList.Add(popupItem);
            }

            _popupViewModel.Items.SynchronizeWith(
                cachedItems,
                (existingVm, newItemData) => existingVm.Equals(newItemData.Source),
                (itemData) =>
                {
                    try
                    {
                        return _viewFactory.CreateViewModel(itemData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create a viewmodel for a clipboard item in compact mode.");
                        return _viewFactory.CreateViewModel(new ClipboardItemData(itemData.Source, DetectedClipboardDataType.Unknown, itemData.ContentSignature));
                    }
                });
        });
    }

    /// <summary>
    /// Updates the ViewList with individual SillListViewButtonItems for normal (expanded) mode.
    /// </summary>
    private async Task UpdateViewListAsync()
    {
        IReadOnlyList<ClipboardItemData> cachedItems = GetCombinedItems();

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            if (_iconItem is null)
            {
                _iconItem = new SillListViewMenuFlyoutItem(
                    new ImageIcon
                    {
                        Source = new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "clipboard.svg")))
                    },
                    null,
                    new MenuFlyout
                    {
                        Items =
                        {
                            new MenuFlyoutItem
                            {
                                Text = "/WindowSill.ClipboardHistory/Misc/ClearHistory".GetLocalizedString(),
                                Icon = new SymbolIcon(Symbol.Clear),
                                Command = ClearCommand
                            }
                        }
                    });
            }

            ViewList.Remove(_iconItem);

            ViewList.SynchronizeWith(
                cachedItems,
                (oldItem, newItem) =>
                {
                    if (oldItem.DataContext is ClipboardHistoryItemViewModelBase oldItemViewModel)
                    {
                        return oldItemViewModel.Equals(newItem.Source);
                    }
                    throw new Exception($"Unexpected item type in ViewList: {oldItem.DataContext?.GetType().FullName ?? "null"}");
                },
                (itemData) =>
                {
                    ClipboardHistoryItemViewModelBase viewModel;
                    SillListViewItem view;

                    try
                    {
                        (viewModel, view) = _viewFactory.Create(itemData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create a view and viewmodel for a clipboard item.");
                        (viewModel, view) = _viewFactory.Create(new ClipboardItemData(itemData.Source, DetectedClipboardDataType.Unknown, itemData.ContentSignature));
                    }

                    CreateContextMenu(viewModel, view);

                    if (itemData.Source.IsPinned)
                    {
                        DecorateAsPinned(view);
                    }

                    return view;
                });

            if (cachedItems.Count > 0)
            {
                ViewList.Insert(0, _iconItem);
            }
        });
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            Clipboard.ClearHistory();
        });
    }

    private static void CreateContextMenu(ClipboardHistoryItemViewModelBase viewModel, SillListViewItem view)
    {
        var menuFlyout = new MenuFlyout();

        if (viewModel.IsPinned)
        {
            menuFlyout.Items.Add(new MenuFlyoutItem
            {
                Text = "/WindowSill.ClipboardHistory/Misc/Unpin".GetLocalizedString(),
                Icon = new SymbolIcon(Symbol.UnPin),
                Command = viewModel.UnpinCommand
            });
        }
        else
        {
            menuFlyout.Items.Add(new MenuFlyoutItem
            {
                Text = "/WindowSill.ClipboardHistory/Misc/Pin".GetLocalizedString(),
                Icon = new SymbolIcon(Symbol.Pin),
                Command = viewModel.PinCommand,
                IsEnabled = viewModel.CanPin
            });

            menuFlyout.Items.Add(new MenuFlyoutItem
            {
                Text = "/WindowSill.ClipboardHistory/Misc/ClearHistory".GetLocalizedString(),
                Icon = new SymbolIcon(Symbol.Clear),
                Command = viewModel.ClearCommand
            });
            menuFlyout.Items.Add(new MenuFlyoutItem
            {
                Text = "/WindowSill.ClipboardHistory/Misc/Delete".GetLocalizedString(),
                Icon = new SymbolIcon(Symbol.Delete),
                Command = viewModel.DeleteCommand
            });
        }

        view.ContextFlyout = menuFlyout;
    }

    /// <summary>
    /// Adds a small pin indicator overlay to a pinned item's content.
    /// </summary>
    private static void DecorateAsPinned(SillListViewItem view)
    {
        if (view is SillListViewButtonItem buttonItem && buttonItem.Content is UIElement existing)
        {
            var grid = new Grid();
            grid.Children.Add(existing);
            grid.Children.Add(new FontIcon
            {
                Glyph = "\uE840",
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(2, 2, 0, 0),
                Opacity = 0.7
            });

            buttonItem.Content = grid;
        }
    }
}
