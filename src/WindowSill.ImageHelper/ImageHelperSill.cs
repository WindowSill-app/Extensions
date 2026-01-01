using System.Collections.ObjectModel;
using System.ComponentModel.Composition;

using Microsoft.UI.Xaml.Media.Imaging;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

using WindowSill.API;
using WindowSill.ImageHelper.CompressImage;
using WindowSill.ImageHelper.ConvertImage;
using WindowSill.ImageHelper.ResizeImage;

namespace WindowSill.ImageHelper;

[Export(typeof(ISill))]
[Name("Image Helper")]
public sealed class ImageHelperSill : ISillActivatedByDragAndDrop, ISillListView
{
    private readonly IPluginInfo _pluginInfo;

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
            ViewList.Clear();
            if (compatibleFiles.Count == 1)
            {
                ViewList.Add(
                    new SillListViewPopupItem(
                        "/WindowSill.ImageHelper/ResizeImage/Title".GetLocalizedString(),
                        null,
                        ResizeImageViewModel.CreateView(compatibleFiles[0])));
            }

            ViewList.Add(
                new SillListViewPopupItem(
                    "/WindowSill.ImageHelper/ConvertImage/Title".GetLocalizedString(),
                    null,
                    ConvertImageViewModel.CreateView(compatibleFiles)));

            ViewList.Add(
                new SillListViewPopupItem(
                    "/WindowSill.ImageHelper/CompressImage/Title".GetLocalizedString(),
                    null,
                    CompressImageViewModel.CreateView(compatibleFiles)));
        });
    }

    public ValueTask OnDeactivatedAsync()
    {
        throw new NotImplementedException();
    }
}
