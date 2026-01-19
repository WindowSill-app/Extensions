using System.ComponentModel.Composition;
using WindowSill.API;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace WindowSill.ImageHelper;

[Export(typeof(ISillDragAndDropActivator))]
[ActivationType("ImageFileDrop")]
internal sealed class ImageFileActivator : ISillDragAndDropActivator
{
    public async ValueTask<bool> GetShouldBeActivatedAsync(DataPackageView dataPackageView, CancellationToken cancellationToken)
    {
        if (dataPackageView.Contains(StandardDataFormats.StorageItems))
        {
            IReadOnlyList<IStorageItem> storageItems = await dataPackageView.GetStorageItemsAsync();
            return ContainsImageFiles(storageItems);
        }

        return false;
    }

    private static bool ContainsImageFiles(IReadOnlyList<IStorageItem> storageItems)
    {
        for (int i = 0; i < storageItems.Count; i++)
        {
            IStorageItem storageItem = storageItems[i];
            if (storageItem is IStorageFile storageFile)
            {
                try
                {
                    string fileType = storageFile.FileType.ToLowerInvariant();
                    return Constants.SupportedExtensions.Contains(fileType);
                }
                catch (Exception)
                {
                    // Path is too long to process, or something else; skip this file
                    continue;
                }
            }
        }

        return false;
    }
}
