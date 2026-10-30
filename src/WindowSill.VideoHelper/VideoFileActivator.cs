using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

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
        if (!dataPackageView.Contains(StandardDataFormats.StorageItems))
        {
            return false;
        }

        IReadOnlyList<IStorageItem> storageItems;
        try
        {
            storageItems = await dataPackageView.GetStorageItemsAsync().AsTask(cancellationToken);
        }
        catch (Exception ex) when (ex is COMException or OperationCanceledException or InvalidOperationException or UnauthorizedAccessException)
        {
            // The clipboard/drag data may become unavailable or use an invalid format between the
            // activation check and this call. The host abandons this task once its detection timeout
            // elapses, so an exception escaping here would never be observed and would later surface
            // as an unhandled task exception that takes the whole application down.
            return false;
        }

        return ContainsVideoFiles(storageItems);
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
