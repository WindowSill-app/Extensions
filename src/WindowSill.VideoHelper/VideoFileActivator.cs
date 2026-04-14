using System.ComponentModel.Composition;

using WindowSill.API;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace WindowSill.VideoHelper;

[Export(typeof(ISillDragAndDropActivator))]
[ActivationType("VideoFileDrop")]
internal sealed class VideoFileActivator : ISillDragAndDropActivator
{
    /// <inheritdoc />
    public async ValueTask<bool> GetShouldBeActivatedAsync(DataPackageView dataPackageView, CancellationToken cancellationToken)
    {
        if (dataPackageView.Contains(StandardDataFormats.StorageItems))
        {
            IReadOnlyList<IStorageItem> storageItems = await dataPackageView.GetStorageItemsAsync();
            return ContainsVideoFiles(storageItems);
        }

        return false;
    }

    private static bool ContainsVideoFiles(IReadOnlyList<IStorageItem> storageItems)
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
                    continue;
                }
            }
        }

        return false;
    }
}
