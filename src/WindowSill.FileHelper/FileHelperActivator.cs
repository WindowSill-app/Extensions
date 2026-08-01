using System.ComponentModel.Composition;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using WindowSill.API;
using WindowSill.FileHelper.Core;

namespace WindowSill.FileHelper;

/// <summary>
/// Decides whether the FileHelper sill should activate for a given file selection: exactly one ZIP archive, or
/// one-or-more documents that all share the same convertible format. Mixed/unsupported selections do not activate
/// this sill.
/// </summary>
[Export(typeof(ISillDragAndDropActivator))]
[ActivationType("FileHelperFileDrop")]
internal sealed class FileHelperActivator : ISillDragAndDropActivator
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
            storageItems = await dataPackageView.GetStorageItemsAsync();
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // The clipboard/drag data may become unavailable or use an invalid format
            // between the activation check and this call. Nothing to do here.
            return false;
        }

        FileSelectionResult selection = FileSelectionClassifier.Classify(storageItems);
        return selection.HasAnyExperience;
    }
}
