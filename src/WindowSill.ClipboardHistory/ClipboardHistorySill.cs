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
    private ClipboardHistoryMenuViewModel? _menuViewModel;

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
                    ViewList.Clear();

                    if (!Clipboard.IsHistoryEnabled() || clipboardItems.Count == 0)
                    {
                        // Don't add anything - PlaceholderView will be shown
                        return;
                    }

                    // Create or reuse the menu view model
                    if (_menuViewModel == null)
                    {
                        _menuViewModel = new ClipboardHistoryMenuViewModel(
                            _logger,
                            _settingsProvider,
                            _processInteractionService);
                    }

                    // Update menu items
                    await _menuViewModel.UpdateMenuItemsAsync(clipboardItems);

                    // Create a single menu flyout item that shows all clipboard history
                    var menuFlyoutItem = new SillListViewMenuFlyoutItem(
                        $"📋 {clipboardItems.Count} {(clipboardItems.Count == 1 ? "item" : "items")}",
                        "/WindowSill.ClipboardHistory/Misc/DisplayName".GetLocalizedString(),
                        _menuViewModel.GetMenuFlyout());

                    ViewList.Add(menuFlyoutItem);
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
}
