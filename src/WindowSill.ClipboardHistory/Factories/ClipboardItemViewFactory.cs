using WindowSill.API;
using WindowSill.ClipboardHistory.Core;
using WindowSill.ClipboardHistory.Services;
using WindowSill.ClipboardHistory.ViewModels;
using WindowSill.ClipboardHistory.Views;

namespace WindowSill.ClipboardHistory.Factories;

/// <summary>
/// Factory responsible for creating clipboard item ViewModels and their corresponding XAML views.
/// Bridges the gap between pure MVVM ViewModels and the WindowSill API view types.
/// </summary>
internal sealed class ClipboardItemViewFactory
{
    private readonly IPluginInfo _pluginInfo;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IProcessInteractionService _processInteractionService;
    private readonly PinnedClipboardService _pinnedService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClipboardItemViewFactory"/> class.
    /// </summary>
    /// <param name="pluginInfo">The plugin information.</param>
    /// <param name="settingsProvider">The settings provider for extension settings.</param>
    /// <param name="processInteractionService">The service for interacting with external processes.</param>
    /// <param name="pinnedService">The service that owns pinned clipboard items.</param>
    internal ClipboardItemViewFactory(
        IPluginInfo pluginInfo,
        ISettingsProvider settingsProvider,
        IProcessInteractionService processInteractionService,
        PinnedClipboardService pinnedService)
    {
        _pluginInfo = pluginInfo;
        _settingsProvider = settingsProvider;
        _processInteractionService = processInteractionService;
        _pinnedService = pinnedService;
    }

    /// <summary>
    /// Creates a ViewModel for the given clipboard item data without creating a view.
    /// Used by compact mode to populate the popup list.
    /// </summary>
    /// <param name="itemData">The clipboard item data including the detected data type.</param>
    /// <returns>The ViewModel for the clipboard item.</returns>
    internal ClipboardHistoryItemViewModelBase CreateViewModel(ClipboardItemData itemData)
    {
        ClipboardHistoryItemViewModelBase viewModel = itemData.DataType switch
        {
            DetectedClipboardDataType.Text => new TextItemViewModel(_settingsProvider, _processInteractionService, itemData.Source),
            DetectedClipboardDataType.Html => new HtmlItemViewModel(_processInteractionService, itemData.Source),
            DetectedClipboardDataType.Rtf => new RtfItemViewModel(_processInteractionService, itemData.Source),
            DetectedClipboardDataType.Image => new ImageItemViewModel(_processInteractionService, itemData.Source),
            DetectedClipboardDataType.Uri => new UriItemViewModel(_processInteractionService, itemData.Source),
            DetectedClipboardDataType.ApplicationLink => new ApplicationLinkItemViewModel(_processInteractionService, itemData.Source),
            DetectedClipboardDataType.Color => new ColorItemViewModel(_processInteractionService, itemData.Source),
            DetectedClipboardDataType.UserActivity => new UserActivityItemViewModel(_processInteractionService, itemData.Source),
            DetectedClipboardDataType.File => new FileItemViewModel(_processInteractionService, itemData.Source),
            _ => new UnknownItemViewModel(_processInteractionService, itemData.Source),
        };

        viewModel.ConfigurePinning(itemData, _pinnedService);
        return viewModel;
    }

    /// <summary>
    /// Creates a ViewModel and its corresponding view for the given clipboard item data.
    /// </summary>
    /// <param name="itemData">The clipboard item data including the detected data type.</param>
    /// <returns>A tuple of the ViewModel and its wrapped <see cref="SillListViewItem"/>.</returns>
    internal (ClipboardHistoryItemViewModelBase ViewModel, SillListViewItem View) Create(ClipboardItemData itemData)
    {
        (ClipboardHistoryItemViewModelBase ViewModel, SillListViewItem View) result = itemData.DataType switch
        {
            DetectedClipboardDataType.Text => CreateTextView(itemData.Source),
            DetectedClipboardDataType.Html => CreateHtmlView(itemData.Source),
            DetectedClipboardDataType.Rtf => CreateRtfView(itemData.Source),
            DetectedClipboardDataType.Image => CreateImageView(itemData.Source),
            DetectedClipboardDataType.Uri => CreateUriView(itemData.Source),
            DetectedClipboardDataType.ApplicationLink => CreateApplicationLinkView(itemData.Source),
            DetectedClipboardDataType.Color => CreateColorView(itemData.Source),
            DetectedClipboardDataType.UserActivity => CreateUserActivityView(itemData.Source),
            DetectedClipboardDataType.File => CreateFileView(itemData.Source),
            _ => CreateUnknownView(itemData.Source),
        };

        result.ViewModel.ConfigurePinning(itemData, _pinnedService);
        return result;
    }

    /// <summary>
    /// Creates the placeholder view shown when the clipboard history is empty or disabled.
    /// </summary>
    /// <returns>A <see cref="SillView"/> containing the empty/disabled placeholder.</returns>
    internal SillView CreatePlaceholderView()
    {
        return new SillView { Content = new EmptyOrDisabledItemView(_pluginInfo) };
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateTextView(IClipboardItemSource source)
    {
        var viewModel = new TextItemViewModel(_settingsProvider, _processInteractionService, source);
        SillListViewButtonItem view = CreateButtonItem(viewModel);
        view.Content = new TextItemContentView(viewModel);
        view.PreviewFlyoutContent = new TextItemPreviewView(viewModel);
        return (viewModel, view);
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateHtmlView(IClipboardItemSource source)
    {
        var viewModel = new HtmlItemViewModel(_processInteractionService, source);
        SillListViewButtonItem view = CreateButtonItem(viewModel);
        view.Content = new HtmlItemContentView(viewModel);
        view.PreviewFlyoutContent = new HtmlItemPreviewView(viewModel);
        return (viewModel, view);
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateRtfView(IClipboardItemSource source)
    {
        var viewModel = new RtfItemViewModel(_processInteractionService, source);
        SillListViewButtonItem view = CreateButtonItem(viewModel);
        view.Content = new RtfItemContentView(viewModel);
        view.PreviewFlyoutContent = new RtfItemPreviewView(viewModel);
        return (viewModel, view);
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateImageView(IClipboardItemSource source)
    {
        var viewModel = new ImageItemViewModel(_processInteractionService, source);
        SillListViewButtonItem view = CreateButtonItem(viewModel);
        view.Content = new ImageItemContentView(viewModel);
        view.PreviewFlyoutContent = new ImageItemPreviewView(viewModel);
        return (viewModel, view);
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateUriView(IClipboardItemSource source)
    {
        var viewModel = new UriItemViewModel(_processInteractionService, source);
        SillListViewButtonItem view = CreateButtonItem(viewModel);
        view.Content = new UriItemContentView(viewModel);
        view.PreviewFlyoutContent = new UriItemPreviewView(viewModel);
        return (viewModel, view);
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateApplicationLinkView(IClipboardItemSource source)
    {
        var viewModel = new ApplicationLinkItemViewModel(_processInteractionService, source);
        SillListViewButtonItem view = CreateButtonItem(viewModel);
        view.Content = new ApplicationLinkItemContentView(viewModel);
        view.PreviewFlyoutContent = new ApplicationLinkItemPreviewView(viewModel);
        return (viewModel, view);
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateColorView(IClipboardItemSource source)
    {
        var viewModel = new ColorItemViewModel(_processInteractionService, source);
        SillListViewButtonItem view = CreateButtonItem(viewModel);
        view.Content = new ColorItemContentView(viewModel);
        view.PreviewFlyoutContent = new ColorItemPreviewView(viewModel);
        return (viewModel, view);
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateUserActivityView(IClipboardItemSource source)
    {
        var viewModel = new UserActivityItemViewModel(_processInteractionService, source);
        SillListViewButtonItem view = CreateButtonItem(viewModel);
        view.Content = new UserActivityItemContentView(viewModel);
        view.PreviewFlyoutContent = new UserActivityItemPreviewView(viewModel);
        return (viewModel, view);
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateFileView(IClipboardItemSource source)
    {
        var viewModel = new FileItemViewModel(_processInteractionService, source);
        SillListViewButtonItem view = CreateButtonItem(viewModel);
        view.Content = new FileItemContentView(viewModel);
        view.PreviewFlyoutContent = new FileItemPreviewView(viewModel);
        return (viewModel, view);
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateUnknownView(IClipboardItemSource source)
    {
        var viewModel = new UnknownItemViewModel(_processInteractionService, source);
        SillListViewButtonItem view = CreateButtonItem(viewModel);
        view.Content = new UnknownItemContentView(viewModel);
        view.PreviewFlyoutContent = new UnknownItemPreviewView(viewModel);
        return (viewModel, view);
    }

    private static SillListViewButtonItem CreateButtonItem(ClipboardHistoryItemViewModelBase viewModel)
    {
        return new SillListViewButtonItem(viewModel.PasteCommand) { DataContext = viewModel };
    }
}
