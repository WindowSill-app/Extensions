using System.Collections.ObjectModel;
using System.ComponentModel.Composition;

using Microsoft.UI.Xaml.Media.Imaging;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

using WindowSill.API;
using WindowSill.ImageHelper.Core;
using WindowSill.ImageHelper.ViewModels;
using WindowSill.ImageHelper.Views;

namespace WindowSill.ImageHelper;

/// <summary>
/// Entry point for the Image Helper extension.
/// </summary>
[Export(typeof(ISill))]
[Name("Image Helper")]
public sealed class ImageHelperSill : ISillActivatedByDragAndDrop, ISillListView
{
    private readonly IPluginInfo _pluginInfo;
    private readonly IImageCompressor _compressor = new MagickImageCompressor();
    private readonly IImageConverter _converter = new MagickImageConverter();
    private readonly IImageResizer _resizer = new MagickImageResizer();

    [ImportingConstructor]
    internal ImageHelperSill(IPluginInfo pluginInfo)
    {
        _pluginInfo = pluginInfo;
    }

    public string DisplayName => "/WindowSill.ImageHelper/Misc/DisplayName".GetLocalizedString();

    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "image.svg")))
        };

    public ObservableCollection<SillListViewItem> ViewList { get; } = new();

    public SillView? PlaceholderView => throw new NotImplementedException();

    public SillSettingsView[]? SettingsViews => throw new NotImplementedException();

    public string[] DragAndDropActivatorTypeNames => ["ImageFileDrop"];

    public async ValueTask OnActivatedAsync(string dragAndDropActivatorTypeName, DataPackageView data)
    {
        var compatibleFiles = new List<IStorageFile>();
        try
        {
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
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // The clipboard data may become unavailable or use an invalid format
            // between the activation check and this call. Nothing to do here.
            return;
        }

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            ViewList.Clear();
            if (compatibleFiles.Count == 1)
            {
                ResizeImagePopup resizePopup = CreateResizePopup(compatibleFiles[0]);
                ViewList.Add(
                    new SillListViewPopupItem(
                        "/WindowSill.ImageHelper/ResizeImage/Title".GetLocalizedString(),
                        null,
                        resizePopup));
            }

            ConvertImagePopup convertPopup = CreateConvertPopup(compatibleFiles);
            ViewList.Add(
                new SillListViewPopupItem(
                    "/WindowSill.ImageHelper/ConvertImage/Title".GetLocalizedString(),
                    null,
                    convertPopup));

            CompressImagePopup compressPopup = CreateCompressPopup(compatibleFiles);
            ViewList.Add(
                new SillListViewPopupItem(
                    "/WindowSill.ImageHelper/CompressImage/Title".GetLocalizedString(),
                    null,
                    compressPopup));
        });
    }

    public ValueTask OnDeactivatedAsync()
    {
        throw new NotImplementedException();
    }

    private CompressImagePopup CreateCompressPopup(IReadOnlyList<IStorageFile> files)
    {
        CompressImagePopup? popup = null;
        var viewModel = new CompressImageViewModel(files, _compressor, () => popup?.Close());
        popup = new CompressImagePopup(viewModel);
        return popup;
    }

    private ConvertImagePopup CreateConvertPopup(IReadOnlyList<IStorageFile> files)
    {
        ConvertImagePopup? popup = null;
        var viewModel = new ConvertImageViewModel(files, _converter, () => popup?.Close());
        popup = new ConvertImagePopup(viewModel);
        return popup;
    }

    private ResizeImagePopup CreateResizePopup(IStorageFile file)
    {
        ResizeImagePopup? popup = null;
        var viewModel = new ResizeImageViewModel(file, _resizer, () => popup?.Close());
        popup = new ResizeImagePopup(viewModel);
        return popup;
    }
}
