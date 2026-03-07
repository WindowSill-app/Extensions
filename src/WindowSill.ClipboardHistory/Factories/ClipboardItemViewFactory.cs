using Windows.ApplicationModel.DataTransfer;
using WindowSill.API;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ClipboardItemViewFactory"/> class.
    /// </summary>
    /// <param name="pluginInfo">The plugin information.</param>
    /// <param name="settingsProvider">The settings provider for extension settings.</param>
    /// <param name="processInteractionService">The service for interacting with external processes.</param>
    internal ClipboardItemViewFactory(
        IPluginInfo pluginInfo,
        ISettingsProvider settingsProvider,
        IProcessInteractionService processInteractionService)
    {
        _pluginInfo = pluginInfo;
        _settingsProvider = settingsProvider;
        _processInteractionService = processInteractionService;
    }

    /// <summary>
    /// Creates a ViewModel for the given clipboard item data without creating a view.
    /// Used by compact mode to populate the popup list.
    /// </summary>
    /// <param name="itemData">The clipboard item data including the detected data type.</param>
    /// <returns>The ViewModel for the clipboard item.</returns>
    internal ClipboardHistoryItemViewModelBase CreateViewModel(ClipboardItemData itemData)
    {
        return itemData.DataType switch
        {
            DetectedClipboardDataType.Text => new TextItemViewModel(_settingsProvider, _processInteractionService, itemData.Item),
            DetectedClipboardDataType.Html => new HtmlItemViewModel(_processInteractionService, itemData.Item),
            DetectedClipboardDataType.Rtf => new RtfItemViewModel(_processInteractionService, itemData.Item),
            DetectedClipboardDataType.Image => new ImageItemViewModel(_processInteractionService, itemData.Item),
            DetectedClipboardDataType.Uri => new UriItemViewModel(_processInteractionService, itemData.Item),
            DetectedClipboardDataType.ApplicationLink => new ApplicationLinkItemViewModel(_processInteractionService, itemData.Item),
            DetectedClipboardDataType.Color => new ColorItemViewModel(_processInteractionService, itemData.Item),
            DetectedClipboardDataType.UserActivity => new UserActivityItemViewModel(_processInteractionService, itemData.Item),
            DetectedClipboardDataType.File => new FileItemViewModel(_processInteractionService, itemData.Item),
            _ => new UnknownItemViewModel(_processInteractionService, itemData.Item),
        };
    }

    /// <summary>
    /// Creates a ViewModel and its corresponding view for the given clipboard item data.
    /// </summary>
    /// <param name="itemData">The clipboard item data including the detected data type.</param>
    /// <returns>A tuple of the ViewModel and its wrapped <see cref="SillListViewItem"/>.</returns>
    internal (ClipboardHistoryItemViewModelBase ViewModel, SillListViewItem View) Create(ClipboardItemData itemData)
    {
        return itemData.DataType switch
        {
            DetectedClipboardDataType.Text => CreateTextView(itemData.Item),
            DetectedClipboardDataType.Html => CreateHtmlView(itemData.Item),
            DetectedClipboardDataType.Rtf => CreateRtfView(itemData.Item),
            DetectedClipboardDataType.Image => CreateImageView(itemData.Item),
            DetectedClipboardDataType.Uri => CreateUriView(itemData.Item),
            DetectedClipboardDataType.ApplicationLink => CreateApplicationLinkView(itemData.Item),
            DetectedClipboardDataType.Color => CreateColorView(itemData.Item),
            DetectedClipboardDataType.UserActivity => CreateUserActivityView(itemData.Item),
            DetectedClipboardDataType.File => CreateFileView(itemData.Item),
            _ => CreateUnknownView(itemData.Item),
        };
    }

    /// <summary>
    /// Creates the placeholder view shown when the clipboard history is empty or disabled.
    /// </summary>
    /// <returns>A <see cref="SillView"/> containing the empty/disabled placeholder.</returns>
    internal SillView CreatePlaceholderView()
    {
        return new SillView { Content = new EmptyOrDisabledItemView(_pluginInfo) };
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateTextView(ClipboardHistoryItem item)
    {
        var viewModel = new TextItemViewModel(_settingsProvider, _processInteractionService, item);
        SillListViewButtonItem view = CreateButtonItem(viewModel);
        view.Content = new TextItemContentView(viewModel);
        view.PreviewFlyoutContent = new TextItemPreviewView(viewModel);
        return (viewModel, view);
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateHtmlView(ClipboardHistoryItem item)
    {
        var viewModel = new HtmlItemViewModel(_processInteractionService, item);
        SillListViewButtonItem view = CreateButtonItem(viewModel);
        view.Content = new HtmlItemContentView(viewModel);
        view.PreviewFlyoutContent = new HtmlItemPreviewView(viewModel);
        return (viewModel, view);
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateRtfView(ClipboardHistoryItem item)
    {
        var viewModel = new RtfItemViewModel(_processInteractionService, item);
        SillListViewButtonItem view = CreateButtonItem(viewModel);
        view.Content = new RtfItemContentView(viewModel);
        view.PreviewFlyoutContent = new RtfItemPreviewView(viewModel);
        return (viewModel, view);
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateImageView(ClipboardHistoryItem item)
    {
        var viewModel = new ImageItemViewModel(_processInteractionService, item);
        SillListViewButtonItem view = CreateButtonItem(viewModel);
        view.Content = new ImageItemContentView(viewModel);
        view.PreviewFlyoutContent = new ImageItemPreviewView(viewModel);
        return (viewModel, view);
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateUriView(ClipboardHistoryItem item)
    {
        var viewModel = new UriItemViewModel(_processInteractionService, item);
        SillListViewButtonItem view = CreateButtonItem(viewModel);
        view.Content = new UriItemContentView(viewModel);
        view.PreviewFlyoutContent = new UriItemPreviewView(viewModel);
        return (viewModel, view);
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateApplicationLinkView(ClipboardHistoryItem item)
    {
        var viewModel = new ApplicationLinkItemViewModel(_processInteractionService, item);
        SillListViewButtonItem view = CreateButtonItem(viewModel);
        view.Content = new ApplicationLinkItemContentView(viewModel);
        view.PreviewFlyoutContent = new ApplicationLinkItemPreviewView(viewModel);
        return (viewModel, view);
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateColorView(ClipboardHistoryItem item)
    {
        var viewModel = new ColorItemViewModel(_processInteractionService, item);
        SillListViewButtonItem view = CreateButtonItem(viewModel);
        view.Content = new ColorItemContentView(viewModel);
        view.PreviewFlyoutContent = new ColorItemPreviewView(viewModel);
        return (viewModel, view);
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateUserActivityView(ClipboardHistoryItem item)
    {
        var viewModel = new UserActivityItemViewModel(_processInteractionService, item);
        SillListViewButtonItem view = CreateButtonItem(viewModel);
        view.Content = new UserActivityItemContentView(viewModel);
        view.PreviewFlyoutContent = new UserActivityItemPreviewView(viewModel);
        return (viewModel, view);
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateFileView(ClipboardHistoryItem item)
    {
        var viewModel = new FileItemViewModel(_processInteractionService, item);
        SillListViewButtonItem view = CreateButtonItem(viewModel);
        view.Content = new FileItemContentView(viewModel);
        view.PreviewFlyoutContent = new FileItemPreviewView(viewModel);
        return (viewModel, view);
    }

    private (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateUnknownView(ClipboardHistoryItem item)
    {
        var viewModel = new UnknownItemViewModel(_processInteractionService, item);
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
