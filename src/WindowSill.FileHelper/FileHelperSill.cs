using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using WindowSill.API;
using WindowSill.FileHelper.Core;
using WindowSill.FileHelper.Services;
using WindowSill.FileHelper.ViewModels;
using WindowSill.FileHelper.Views;

namespace WindowSill.FileHelper;

/// <summary>
/// Entry point for the File Helper extension. Selecting exactly one ZIP archive shows an instant, read-only
/// metadata summary; selecting one-or-more documents that share a convertible format (DOCX, DOC, RTF, HTML,
/// Markdown or TXT) activates a "Convert" queue-backed workflow whose persisted progress survives popup and
/// file-selection changes.
/// </summary>
[Export(typeof(ISill))]
[Name("File Helper")]
internal sealed class FileHelperSill : ISillActivatedByDragAndDrop, ISillActivatedByDefault, ISillListView, IDisposable
{
    private readonly IPluginInfo _pluginInfo;
    private readonly IZipArchiveInspector _zipArchiveInspector;
    private readonly IFileOperationService _fileOperationService;

    private bool _isDynamicallyActivated;
    private FileSelectionResult _currentSelection = FileSelectionResult.None;

    [ImportingConstructor]
    internal FileHelperSill(
        IPluginInfo pluginInfo,
        IZipArchiveInspector zipArchiveInspector,
        IFileOperationService fileOperationService)
    {
        _pluginInfo = pluginInfo;
        _zipArchiveInspector = zipArchiveInspector;
        _fileOperationService = fileOperationService;
        _fileOperationService.Queues.CollectionChanged += Queues_CollectionChanged;
    }

    public string DisplayName => "/WindowSill.FileHelper/Misc/DisplayName".GetLocalizedString();

    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "file.svg")))
        };

    public ObservableCollection<SillListViewItem> ViewList { get; } = new();

    public SillView? PlaceholderView => null;

    public SillSettingsView[]? SettingsViews => null;

    public string[] DragAndDropActivatorTypeNames => ["FileHelperFileDrop"];

    public async ValueTask OnActivatedAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            if (!_isDynamicallyActivated)
            {
                _currentSelection = FileSelectionResult.None;
                UpdateViewList();
            }
        });
    }

    public async ValueTask OnActivatedAsync(string dragAndDropActivatorTypeName, DataPackageView data)
    {
        FileSelectionResult selection = FileSelectionResult.None;

        try
        {
            if (data.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> storageItems = await data.GetStorageItemsAsync();
                selection = FileSelectionClassifier.Classify(storageItems);
            }
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // The clipboard/drag data may become unavailable or use an invalid format
            // between the activation check and this call. Fall back to "no selection".
        }

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            _isDynamicallyActivated = true;
            _currentSelection = selection;

            UpdateViewList();
        });
    }

    public async ValueTask OnDeactivatedAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            _isDynamicallyActivated = false;
            _currentSelection = FileSelectionResult.None;

            UpdateViewList();
        });
    }

    public void Dispose()
    {
        _fileOperationService.Queues.CollectionChanged -= Queues_CollectionChanged;
    }

    private void Queues_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ThreadHelper.RunOnUIThreadAsync(() =>
        {
            UpdateViewList();
        }).ForgetSafely();
    }

    private void UpdateViewList()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        ViewList.Clear();

        if (_currentSelection.Kind == FileSelectionKind.Zip && _currentSelection.Files is { Count: 1 })
        {
            var viewModel = new ZipInfoViewModel(_currentSelection.Files[0], _zipArchiveInspector);

            // The inline summary stays as the button's content (adapting to the sill size), but the item is now a
            // popup item: clicking it opens a read-only details popup listing every file in the archive. Both the
            // inline content and the popup share this one view model, so the archive is inspected only once.
            var viewItem = new SillListViewPopupItem();
            viewItem.Content = new ZipInfoSillContent(viewModel, viewItem);
            viewItem.PopupContent = new ZipDetailsPopup(viewModel);
            ViewList.Add(viewItem);
        }
        else if (_currentSelection is { Kind: FileSelectionKind.Document, DocumentFileFormat: DocumentFileFormat sourceFormat }
            && _currentSelection.Files is { Count: > 0 })
        {
            ConvertDocumentPopupViewModel viewModel =
                ConvertDocumentPopupViewModel.ForConversion(_currentSelection.Files, sourceFormat, _fileOperationService);
            var popup = new ConvertDocumentPopup(_fileOperationService, viewModel);
            ViewList.Add(
                new SillListViewPopupItem(
                    "/WindowSill.FileHelper/ConvertDocument/Title".GetLocalizedString(),
                    null,
                    popup));
        }
        else if (_currentSelection.Kind == FileSelectionKind.Pdf && _currentSelection.Files is { Count: > 0 })
        {
            ConvertDocumentPopupViewModel viewModel =
                ConvertDocumentPopupViewModel.ForPdfActions(_currentSelection.Files, _fileOperationService);
            var popup = new ConvertDocumentPopup(_fileOperationService, viewModel);
            ViewList.Add(
                new SillListViewPopupItem(
                    "/WindowSill.FileHelper/PdfActions/Title".GetLocalizedString(),
                    null,
                    popup));
        }

        // Text handling sits alongside conversion rather than replacing it: a .txt can be both converted to another
        // document format and re-encoded, so both items appear.
        if (_currentSelection.IsTextSelection && _currentSelection.Files is { Count: > 0 })
        {
            ConvertDocumentPopupViewModel textViewModel =
                ConvertDocumentPopupViewModel.ForTextActions(_currentSelection.Files, _fileOperationService);
            var textPopup = new ConvertDocumentPopup(_fileOperationService, textViewModel);
            ViewList.Add(
                new SillListViewPopupItem(
                    "/WindowSill.FileHelper/TextActions/Title".GetLocalizedString(),
                    null,
                    textPopup));
        }

        for (int i = 0; i < _fileOperationService.Queues.Count; i++)
        {
            FileOperationQueue queue = _fileOperationService.Queues[i];

            var viewItem = new SillListViewPopupItem();
            viewItem.Content = new ConvertDocumentProgressListItemContent(viewItem, queue);
            viewItem.PopupContent = new ConvertDocumentPopup(
                _fileOperationService,
                ConvertDocumentPopupViewModel.ForQueue(queue, _fileOperationService));

            ViewList.Add(viewItem);
        }
    }
}
