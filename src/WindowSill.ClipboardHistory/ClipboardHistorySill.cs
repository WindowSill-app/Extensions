using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using WindowSill.API;
using WindowSill.ClipboardHistory.FirstTimeSetup;
using WindowSill.ClipboardHistory.Settings;
using WindowSill.ClipboardHistory.UI;
using WindowSill.ClipboardHistory.Utils;

namespace WindowSill.ClipboardHistory;

[Export(typeof(ISill))]
[Name("Clipboard History")]
[Priority(Priority.Low)]
public sealed class ClipboardHistorySill : ISillActivatedByDefault, ISillFirstTimeSetup, ISillListView
{
    private readonly DisposableSemaphore _semaphore = new();
    private readonly ILogger _logger;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IProcessInteractionService _processInteractionService;
    private readonly IPluginInfo _pluginInfo;

    [ImportingConstructor]
    internal ClipboardHistorySill(
        IProcessInteractionService processInteractionService,
        ISettingsProvider settingsProvider,
        IPluginInfo pluginInfo)
    {
        _logger = this.Log();
        _processInteractionService = processInteractionService;
        _pluginInfo = pluginInfo;
        _settingsProvider = settingsProvider;
        _settingsProvider.SettingChanged += SettingsProvider_SettingChanged;
        Clipboard.ContentChanged += Clipboard_ContentChanged;
        Clipboard.HistoryChanged += Clipboard_HistoryChanged;
        Clipboard.HistoryEnabledChanged += Clipboard_HistoryEnabledChanged;

        UpdateClipboardHistoryAsync().Forget();
    }

    public string DisplayName => "/WindowSill.ClipboardHistory/Misc/DisplayName".GetLocalizedString();

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

    public SillView? PlaceholderView { get; } = EmptyOrDisabledItemViewModel.CreateView();

    public IFirstTimeSetupContributor[] GetFirstTimeSetupContributors()
    {
        if (Clipboard.IsHistoryEnabled())
        {
            return [];
        }

        return [new ClipboardHistoryFirstTimeSetupContributor()];
    }

    public async ValueTask OnActivatedAsync()
    {
        _settingsProvider.SettingChanged += SettingsProvider_SettingChanged;
        Clipboard.ContentChanged += Clipboard_ContentChanged;
        Clipboard.HistoryChanged += Clipboard_HistoryChanged;
        Clipboard.HistoryEnabledChanged += Clipboard_HistoryEnabledChanged;

        await UpdateClipboardHistoryAsync();
    }

    public ValueTask OnDeactivatedAsync()
    {
        _settingsProvider.SettingChanged -= SettingsProvider_SettingChanged;
        Clipboard.ContentChanged -= Clipboard_ContentChanged;
        Clipboard.HistoryChanged -= Clipboard_HistoryChanged;
        Clipboard.HistoryEnabledChanged -= Clipboard_HistoryEnabledChanged;
        return ValueTask.CompletedTask;
    }

    private void SettingsProvider_SettingChanged(ISettingsProvider sender, SettingChangedEventArgs args)
    {
        if (args.SettingName == Settings.Settings.MaximumHistoryCount.Name)
        {
            UpdateClipboardHistoryAsync().Forget();
        }
        else if (args.SettingName == Settings.Settings.HidePasswords.Name)
        {
            ViewList.Clear();
            UpdateClipboardHistoryAsync().Forget();
        }
    }

    private void Clipboard_HistoryEnabledChanged(object? sender, object e)
    {
        UpdateClipboardHistoryAsync().Forget();
    }

    private void Clipboard_HistoryChanged(object? sender, ClipboardHistoryChangedEventArgs e)
    {
        UpdateClipboardHistoryAsync().Forget();
    }

    private void Clipboard_ContentChanged(object? sender, object e)
    {
        // TODO
    }

    private async Task UpdateClipboardHistoryAsync()
    {
        await Task.Run(async () =>
        {
            ThreadHelper.ThrowIfOnUIThread();

            using (await _semaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false))
            {
                IReadOnlyList<ClipboardHistoryItem> clipboardItems = await GetClipboardHistoryItemsAsync();

                await ThreadHelper.RunOnUIThreadAsync(async () =>
                {
                    await ViewList.SynchronizeWithAsync(
                        clipboardItems,
                        (oldItem, newItem) =>
                        {
                            if (oldItem.DataContext is ClipboardHistoryItemViewModelBase oldItemViewModel)
                            {
                                return oldItemViewModel.Equals(newItem);
                            }
                            throw new Exception($"Unexpected item type in ViewList: {oldItem.DataContext?.GetType().FullName ?? "null"}");
                        },
                        async (clipboardItem) =>
                        {
                            ClipboardHistoryItemViewModelBase viewModel;
                            SillListViewItem view;

                            try
                            {
                                DetectedClipboardDataType dataType = await DataHelper.GetDetectedClipboardDataTypeAsync(clipboardItem);

                                (viewModel, view) = dataType switch
                                {
                                    DetectedClipboardDataType.Image => ImageItemViewModel.CreateView(_processInteractionService, clipboardItem),
                                    DetectedClipboardDataType.Text => TextItemViewModel.CreateView(_settingsProvider, _processInteractionService, clipboardItem),
                                    DetectedClipboardDataType.Html => HtmlItemViewModel.CreateView(_processInteractionService, clipboardItem),
                                    DetectedClipboardDataType.Rtf => RtfItemViewModel.CreateView(_processInteractionService, clipboardItem),
                                    DetectedClipboardDataType.Uri => UriItemViewModel.CreateView(_processInteractionService, clipboardItem),
                                    DetectedClipboardDataType.ApplicationLink => ApplicationLinkItemViewModel.CreateView(_processInteractionService, clipboardItem),
                                    DetectedClipboardDataType.Color => ColorItemViewModel.CreateView(_processInteractionService, clipboardItem),
                                    DetectedClipboardDataType.UserActivity => UserActivityItemViewModel.CreateView(_processInteractionService, clipboardItem),
                                    DetectedClipboardDataType.File => FileItemViewModel.CreateView(_processInteractionService, clipboardItem),
                                    _ => UnknownItemViewModel.CreateView(_processInteractionService, clipboardItem),
                                };
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to create a view and viewmodel for a clipboard item.");
                                (viewModel, view) = UnknownItemViewModel.CreateView(_processInteractionService, clipboardItem);
                            }

                            CreateContextMenu(viewModel, view);

                            return view;
                        });
                });
            }
        });
    }

    private async Task<IReadOnlyList<ClipboardHistoryItem>> GetClipboardHistoryItemsAsync()
    {
        try
        {
            if (Clipboard.IsHistoryEnabled())
            {
                ClipboardHistoryItemsResult clipboardHistory = await Clipboard.GetHistoryItemsAsync();
                if (clipboardHistory.Status == ClipboardHistoryItemsResultStatus.Success)
                {
                    return clipboardHistory.Items
                        .Take(_settingsProvider.GetSetting(Settings.Settings.MaximumHistoryCount))
                        .ToList();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get clipboard history items.");
        }

        return Array.Empty<ClipboardHistoryItem>();
    }

    private static void CreateContextMenu(ClipboardHistoryItemViewModelBase viewModel, SillListViewItem view)
    {
        var menuFlyout = new MenuFlyout();
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

        view.ContextFlyout = menuFlyout;
    }
}
