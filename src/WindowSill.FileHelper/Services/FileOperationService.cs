using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Windows.Storage;
using WindowSill.API;
using WindowSill.FileHelper.Core;
using WindowSill.FileHelper.Core.Operations;

using Path = System.IO.Path;

namespace WindowSill.FileHelper.Services;

/// <summary>
/// Manages file operation queues that run independently of any UI popup.
/// Exported as a MEF singleton so all components share the same service instance.
/// </summary>
[Export(typeof(IFileOperationService))]
internal sealed class FileOperationService : IFileOperationService
{
    /// <inheritdoc />
    public ObservableCollection<FileOperationQueue> Queues { get; } = [];

    /// <inheritdoc />
    public FileOperationQueue StartConversion(IReadOnlyList<IStorageFile> files, DocumentFileFormat source, DocumentFileFormat target)
    {
        List<string> filePaths = [.. files.Select(f => f.Path)];

        // Renderers are stateless and cheap to build, so each file gets a converter bound to this exact
        // source/target pair rather than sharing one per target format.
        List<IFileOperation> operations =
        [
            .. filePaths.Select(path => new ConvertFileOperation(path, ConversionCatalog.CreateConverter(source, target)))
        ];

        string progressText = filePaths.Count == 1
            ? Path.GetFileName(filePaths[0])
            : string.Format("/WindowSill.FileHelper/Misc/ConvertingFiles".GetLocalizedString(), filePaths.Count);

        return Start(operations, progressText);
    }

    /// <inheritdoc />
    public FileOperationQueue StartPdfAction(IReadOnlyList<IStorageFile> files, PdfAction action)
    {
        List<string> filePaths = [.. files.Select(f => f.Path)];

        return Start(
            PdfActionCatalog.CreateOperations(action, filePaths),
            PdfActionCatalog.GetProgressText(action, filePaths));
    }

    /// <inheritdoc />
    public FileOperationQueue StartPdfMerge(IReadOnlyList<string> orderedPaths)
        => Start(
            PdfActionCatalog.CreateMergeOperations(orderedPaths),
            PdfActionCatalog.GetProgressText(PdfAction.Merge, orderedPaths));

    /// <inheritdoc />
    public FileOperationQueue StartPdfExtract(string sourcePath, IReadOnlyList<int> pageIndices)
        => Start(
            PdfActionCatalog.CreateExtractOperations(sourcePath, pageIndices),
            PdfActionCatalog.GetExtractProgressText(pageIndices.Count));

    /// <inheritdoc />
    public FileOperationQueue StartTextAction(IReadOnlyList<IStorageFile> files, TextAction action)
    {
        List<string> filePaths = [.. files.Select(f => f.Path)];

        return Start(
            TextActionCatalog.CreateOperations(action, filePaths),
            TextActionCatalog.GetProgressText(filePaths));
    }

    /// <inheritdoc />
    public FileOperationQueue StartPdfPassword(string sourcePath, string password, bool protect)
        => Start(
            PdfActionCatalog.CreatePasswordOperations(sourcePath, password, protect),
            PdfActionCatalog.GetPasswordProgressText(sourcePath, protect));

    private FileOperationQueue Start(IReadOnlyList<IFileOperation> operations, string progressText)
    {
        var queue = new FileOperationQueue(operations, progressText);
        Queues.Add(queue);

        // Start the work in the background without blocking
        RunQueueAsync(queue).ForgetSafely();

        return queue;
    }

    private static async Task RunQueueAsync(FileOperationQueue queue)
    {
        try
        {
            await queue.RunAsync();
        }
        catch (Exception)
        {
            // Queue handles its own state transitions; this is a safety net
            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                if (queue.State != FileOperationQueueState.Completed)
                {
                    queue.State = FileOperationQueueState.Failed;
                }
            });
        }
    }
}
