using System.ComponentModel.Composition;
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
        if (dataPackageView.Contains(StandardDataFormats.StorageItems))
        {
            IReadOnlyList<IStorageItem> storageItems = await dataPackageView.GetStorageItemsAsync();
            return ContainsScriptFile(storageItems);
        }

        return false;
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
