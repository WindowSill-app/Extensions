using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using WindowSill.API;
using WindowSill.VideoHelper.Core;
using WindowSill.VideoHelper.Services;
using WindowSill.VideoHelper.ViewModels;
using WindowSill.VideoHelper.Views;

namespace WindowSill.VideoHelper;

[Export(typeof(ISill))]
[Name("Video Helper")]
internal sealed class VideoHelperSill : ISillActivatedByDragAndDrop, ISillActivatedByDefault, ISillListView, IDisposable
{
    private readonly IPluginInfo _pluginInfo;
    private readonly IVideoCompressionService _compressionService;
    private readonly IVideoConversionService _conversionService;
    private readonly string _pluginDataFolder;

    private bool _isDynamicallyActivated;
    private IReadOnlyList<IStorageFile>? _currentSelectedFiles;

    [ImportingConstructor]
    internal VideoHelperSill(IPluginInfo pluginInfo, IVideoCompressionService compressionService, IVideoConversionService conversionService)
    {
        _pluginInfo = pluginInfo;
        _compressionService = compressionService;
        _conversionService = conversionService;
        _compressionService.Queues.CollectionChanged += Queues_CollectionChanged;
        _conversionService.Queues.CollectionChanged += Queues_CollectionChanged;
        _pluginDataFolder = _pluginInfo.GetPluginDataFolder();
    }

    public string DisplayName => "/WindowSill.VideoHelper/Misc/DisplayName".GetLocalizedString();

    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "video.svg")))
        };

    public ObservableCollection<SillListViewItem> ViewList { get; } = new();

    public SillView? PlaceholderView => null;

    public SillSettingsView[]? SettingsViews => null;

    public string[] DragAndDropActivatorTypeNames => ["VideoFileDrop"];

    public async ValueTask OnActivatedAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            if (!_isDynamicallyActivated)
            {
                _currentSelectedFiles = null;
                UpdateViewList();
            }
        });
    }

    public async ValueTask OnActivatedAsync(string dragAndDropActivatorTypeName, DataPackageView data)
    {
        var compatibleFiles = new List<IStorageFile>();
        if (data.Contains(StandardDataFormats.StorageItems))
        {
            IReadOnlyList<IStorageItem> storageItems = await data.GetStorageItemsAsync();
            for (int i = 0; i < storageItems.Count; i++)
            {
                IStorageItem storageItem = storageItems[i];
                if (storageItem is IStorageFile storageFile)
                {
                    string fileType = storageFile.FileType.ToLowerInvariant();
                    if (Constants.SupportedExtensions.Contains(fileType))
                    {
                        compatibleFiles.Add(storageFile);
                    }
                }
            }
        }

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            _isDynamicallyActivated = true;
            _currentSelectedFiles = compatibleFiles;

            UpdateViewList();
        });
    }

    public async ValueTask OnDeactivatedAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            _isDynamicallyActivated = false;
            _currentSelectedFiles = null;

            UpdateViewList();
        });
    }

    public void Dispose()
    {
        _compressionService.Queues.CollectionChanged -= Queues_CollectionChanged;
        _conversionService.Queues.CollectionChanged -= Queues_CollectionChanged;
    }

    private CompressVideoPopup? CreateCompressPopup(string ffmpegDirectory)
    {
        if (_currentSelectedFiles is null || _currentSelectedFiles.Count == 0)
        {
            return null;
        }

        var viewModel = new CompressVideoPopupViewModel(_currentSelectedFiles, ffmpegDirectory, _compressionService, _pluginInfo);
        return new CompressVideoPopup(_compressionService, viewModel);
    }

    private ConvertVideoPopup? CreateConvertPopup(string ffmpegDirectory)
    {
        if (_currentSelectedFiles is null || _currentSelectedFiles.Count == 0)
        {
            return null;
        }

        var viewModel = new ConvertVideoPopupViewModel(_currentSelectedFiles, ffmpegDirectory, _conversionService, _pluginInfo);
        return new ConvertVideoPopup(_conversionService, viewModel);
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

        string ffmpegDirectory = FFmpegManager.GetFFmpegDirectory(_pluginDataFolder);
        CompressVideoPopup? compressPopup = CreateCompressPopup(ffmpegDirectory);
        if (compressPopup != null)
        {
            ViewList.Insert(
                0,
                new SillListViewPopupItem(
                    "/WindowSill.VideoHelper/CompressVideo/Title".GetLocalizedString(),
                    null,
                compressPopup));
        }

        ConvertVideoPopup? convertPopup = CreateConvertPopup(ffmpegDirectory);
        if (convertPopup != null)
        {
            ViewList.Add(
                new SillListViewPopupItem(
                    "/WindowSill.VideoHelper/ConvertVideo/Title".GetLocalizedString(),
                    null,
                    convertPopup));
        }

        for (int i = 0; i < _compressionService.Queues.Count; i++)
        {
            VideoCompressionQueue queue = _compressionService.Queues[i];

            var viewItem = new SillListViewPopupItem();
            viewItem.Content = new CompressVideoProgressListItemContent(viewItem, queue);
            viewItem.PopupContent = new CompressVideoPopup(_compressionService, new CompressVideoPopupViewModel(queue, _compressionService));

            ViewList.Add(viewItem);
        }

        for (int i = 0; i < _conversionService.Queues.Count; i++)
        {
            VideoConversionQueue queue = _conversionService.Queues[i];

            var viewItem = new SillListViewPopupItem();
            viewItem.Content = new ConvertVideoProgressListItemContent(viewItem, queue);
            viewItem.PopupContent = new ConvertVideoPopup(_conversionService, new ConvertVideoPopupViewModel(queue, _conversionService));

            ViewList.Add(viewItem);
        }
    }
}

