using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using WindowSill.API;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace WindowSill.InlineTerminal.Activators;

/// <summary>
/// Activates when a dropped file is a recognized script or executable
/// (e.g., <c>.bat</c>, <c>.cmd</c>, <c>.ps1</c>, <c>.sh</c>, <c>.com</c>).
/// </summary>
[Export(typeof(ISillDragAndDropActivator))]
[ActivationType(ActivatorName)]
internal sealed class ScriptFileDropActivator : ISillDragAndDropActivator
{
    internal const string ActivatorName = "ScriptFileDrop";

    internal static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Windows
        ".bat",
        ".cmd",
        ".ps1",
        ".com",

        // Unix / WSL
        ".sh",
        ".bash",
        ".zsh",
    };

    /// <summary>
    /// Returns <see langword="true"/> when the dropped items contain at least one
    /// recognized script file.
    /// </summary>
    public async ValueTask<bool> GetShouldBeActivatedAsync(
        DataPackageView dataPackageView,
        CancellationToken cancellationToken)
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

        return ContainsScriptFile(storageItems);
    }

    private static bool ContainsScriptFile(IReadOnlyList<IStorageItem> storageItems)
    {
        for (int i = 0; i < storageItems.Count; i++)
        {
            if (storageItems[i] is IStorageFile storageFile)
            {
                try
                {
                    if (SupportedExtensions.Contains(storageFile.FileType))
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    // Path too long or other error; skip this file.
                    continue;
                }
            }
        }

        return false;
    }
}
