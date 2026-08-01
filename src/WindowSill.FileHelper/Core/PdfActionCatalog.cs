using WindowSill.API;

using WindowSill.FileHelper.Core.Operations;

using Path = System.IO.Path;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// Decides which PDF actions apply to a given selection, and turns a chosen action into the operations that carry
/// it out. Mirrors <see cref="ConversionCatalog"/>'s role for document conversion.
/// </summary>
internal static class PdfActionCatalog
{
    private static readonly PdfActionInfo Merge = new(PdfAction.Merge, "Merge", RequiresConfiguration: true);
    private static readonly PdfActionInfo Split = new(PdfAction.Split, "Split");
    private static readonly PdfActionInfo Compress = new(PdfAction.Compress, "Compress");
    private static readonly PdfActionInfo Extract = new(PdfAction.Extract, "Extract", RequiresConfiguration: true);
    private static readonly PdfActionInfo SaveAsImages = new(PdfAction.SaveAsImages, "SaveAsImages");
    private static readonly PdfActionInfo Protect = new(PdfAction.Protect, "Protect", RequiresConfiguration: true);
    private static readonly PdfActionInfo Unlock = new(PdfAction.Unlock, "Unlock", RequiresConfiguration: true);

    /// <summary>
    /// Gets the actions available for a selection of <paramref name="fileCount"/> PDFs, in presentation order.
    /// </summary>
    /// <param name="fileCount">How many PDFs are selected.</param>
    /// <returns>The applicable actions, or an empty list when nothing applies.</returns>
    /// <remarks>
    /// Merging needs at least two documents, while picking pages out of — or bursting apart, imaging, or
    /// password-changing — several documents at once would produce an ambiguous pile of output, so each selection
    /// size gets the actions that make sense for it.
    /// </remarks>
    internal static IReadOnlyList<PdfActionInfo> GetActions(int fileCount)
    {
        if (fileCount <= 0)
        {
            return [];
        }

        return fileCount == 1
            ? [Extract, Split, SaveAsImages, Compress, Protect, Unlock]
            : [Merge, Compress];
    }

    /// <summary>
    /// Builds the operations that perform <paramref name="action"/> over the selected files. Only valid for actions
    /// that need no configuration; <see cref="CreateMergeOperations"/> and <see cref="CreateExtractOperations"/>
    /// cover the rest.
    /// </summary>
    /// <param name="action">The action to perform.</param>
    /// <param name="sourcePaths">The selected PDF paths, in selection order.</param>
    /// <returns>One operation per file.</returns>
    internal static IReadOnlyList<IFileOperation> CreateOperations(PdfAction action, IReadOnlyList<string> sourcePaths)
    {
        string pdfExtension = ConversionCatalog.GetInfo(DocumentFileFormat.Pdf).Extension;

        switch (action)
        {
            case PdfAction.Merge:
                return CreateMergeOperations(sourcePaths);

            case PdfAction.Split:
                // Splitting reuses the converter pipeline: the renderer emits one file per page, which the safe
                // writer then relocates together into a folder named after the document.
                return [.. sourcePaths.Select(path =>
                    new ConvertFileOperation(path, new DocumentConverter(new PdfSplitRenderer(), pdfExtension)))];

            case PdfAction.Compress:
                string suffix = "/WindowSill.FileHelper/PdfActions/CompressedFileSuffix".GetLocalizedString();
                return [.. sourcePaths.Select(path =>
                    new ConvertFileOperation(path, new DocumentConverter(new PdfCompressRenderer(), pdfExtension), suffix))];

            case PdfAction.SaveAsImages:
                return [.. sourcePaths.Select(path => new SavePdfPagesAsImagesOperation(path))];

            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported PDF action.");
        }
    }

    /// <summary>
    /// Builds the operation that adds or removes a PDF's open password.
    /// </summary>
    /// <param name="sourcePath">The PDF to protect or unlock.</param>
    /// <param name="password">The password to apply, or the current password when unlocking.</param>
    /// <param name="protect"><see langword="true"/> to add a password; <see langword="false"/> to remove it.</param>
    internal static IReadOnlyList<IFileOperation> CreatePasswordOperations(string sourcePath, string password, bool protect)
    {
        string pdfExtension = ConversionCatalog.GetInfo(DocumentFileFormat.Pdf).Extension;
        string suffixKey = protect ? "ProtectedFileSuffix" : "UnlockedFileSuffix";
        string suffix = $"/WindowSill.FileHelper/PdfActions/{suffixKey}".GetLocalizedString();

        return
        [
            new ConvertFileOperation(
                sourcePath,
                new DocumentConverter(new PdfPasswordRenderer(password, protect), pdfExtension),
                suffix)
        ];
    }

    /// <summary>
    /// Builds the label shown next to the queue's progress bar in the sill for a password change.
    /// </summary>
    /// <param name="sourcePath">The PDF being changed.</param>
    /// <param name="protect"><see langword="true"/> when adding a password; <see langword="false"/> when removing it.</param>
    internal static string GetPasswordProgressText(string sourcePath, bool protect)
        => string.Format(
            (protect
                ? "/WindowSill.FileHelper/PdfActions/ProgressProtecting"
                : "/WindowSill.FileHelper/PdfActions/ProgressUnlocking").GetLocalizedString(),
            Path.GetFileName(sourcePath));

    /// <summary>
    /// Builds the operation that merges the given PDFs in the order supplied by the user.
    /// </summary>
    /// <param name="orderedPaths">The PDFs to merge, in the order they should appear in the output.</param>
    internal static IReadOnlyList<IFileOperation> CreateMergeOperations(IReadOnlyList<string> orderedPaths)
        => [new MergePdfOperation(orderedPaths)];

    /// <summary>
    /// Builds the operation that copies the chosen pages out of a PDF.
    /// </summary>
    /// <param name="sourcePath">The PDF to take pages from.</param>
    /// <param name="pageIndices">Zero-based indices of the pages to copy, in output order.</param>
    internal static IReadOnlyList<IFileOperation> CreateExtractOperations(string sourcePath, IReadOnlyList<int> pageIndices)
        => [new ExtractPdfPagesOperation(sourcePath, pageIndices)];

    /// <summary>
    /// Builds the label shown next to the queue's progress bar in the sill for extracting pages.
    /// </summary>
    /// <param name="pageCount">How many pages are being extracted.</param>
    internal static string GetExtractProgressText(int pageCount)
        => string.Format("/WindowSill.FileHelper/PdfActions/ProgressExtracting".GetLocalizedString(), pageCount);

    /// <summary>
    /// Builds the label shown next to the queue's progress bar in the sill (e.g. "Merging 3 files").
    /// </summary>
    /// <param name="action">The action being performed.</param>
    /// <param name="sourcePaths">The selected PDF paths.</param>
    internal static string GetProgressText(PdfAction action, IReadOnlyList<string> sourcePaths)
    {
        string key = action switch
        {
            PdfAction.Merge => "ProgressMerging",
            PdfAction.Split => "ProgressSplitting",
            PdfAction.Compress => "ProgressCompressing",
            PdfAction.SaveAsImages => "ProgressImaging",
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported PDF action."),
        };

        // A single-file action reads better naming the file itself; merging is always about the whole set.
        if (sourcePaths.Count == 1 && action != PdfAction.Merge)
        {
            return string.Format(
                $"/WindowSill.FileHelper/PdfActions/{key}One".GetLocalizedString(),
                Path.GetFileName(sourcePaths[0]));
        }

        return string.Format(
            $"/WindowSill.FileHelper/PdfActions/{key}Many".GetLocalizedString(),
            sourcePaths.Count);
    }
}
