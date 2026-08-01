using Windows.Storage;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// Classifies a raw file/storage-item selection (e.g. from File Explorer or a drag-and-drop operation) into one
/// of the two FileHelper experiences: a single ZIP archive, or one-or-more convertible documents that all share
/// the same format. Mixed or unsupported selections are intentionally ignored rather than producing an ambiguous
/// combined UI.
/// </summary>
internal static class FileSelectionClassifier
{
    /// <summary>
    /// Classifies the given storage items.
    /// </summary>
    /// <param name="storageItems">The selected storage items (files and/or folders).</param>
    /// <returns>The experience that applies, the files it applies to, and the shared document format if any.</returns>
    internal static FileSelectionResult Classify(IReadOnlyList<IStorageItem> storageItems)
    {
        if (storageItems.Count == 0)
        {
            return FileSelectionResult.None;
        }

        // If the selection contains anything that isn't a file (e.g. a folder), it's not a supported
        // homogeneous selection for either experience.
        var files = new List<IStorageFile>(storageItems.Count);
        foreach (IStorageItem storageItem in storageItems)
        {
            if (storageItem is not IStorageFile storageFile)
            {
                return FileSelectionResult.None;
            }

            files.Add(storageFile);
        }

        if (files.Count == 1 && string.Equals(GetExtension(files[0]), Constants.ZipExtension, StringComparison.OrdinalIgnoreCase))
        {
            return new FileSelectionResult(FileSelectionKind.Zip, files, null);
        }

        return ClassifyAsDocuments(files);
    }

    /// <summary>
    /// Accepts the selection only when every file resolves to the same format, so the popup can offer one
    /// unambiguous set of actions. Extensions that are aliases of one another (e.g. <c>.htm</c> and <c>.html</c>)
    /// resolve to the same format and therefore mix freely.
    /// </summary>
    private static FileSelectionResult ClassifyAsDocuments(List<IStorageFile> files)
    {
        // Text handling is independent of conversion: a .txt is both convertible and re-encodable, while a .json
        // is only re-encodable.
        bool isTextSelection = files.TrueForAll(f => TextFileTypes.IsTextExtension(GetExtension(f)));

        if (!ConversionCatalog.TryGetFormat(GetExtension(files[0]), out DocumentFileFormat format))
        {
            return isTextSelection
                ? new FileSelectionResult(FileSelectionKind.None, files, null, IsTextSelection: true)
                : FileSelectionResult.None;
        }

        for (int i = 1; i < files.Count; i++)
        {
            if (!ConversionCatalog.TryGetFormat(GetExtension(files[i]), out DocumentFileFormat other) || other != format)
            {
                return isTextSelection
                    ? new FileSelectionResult(FileSelectionKind.None, files, null, IsTextSelection: true)
                    : FileSelectionResult.None;
            }
        }

        // PDFs cannot be read back into a document model, so instead of conversion targets they get the PDF
        // actions (merge, split, compress, extract).
        if (format == DocumentFileFormat.Pdf)
        {
            return new FileSelectionResult(FileSelectionKind.Pdf, files, format);
        }

        // A recognized format with nothing to convert to has no conversion experience to offer, but may still be
        // text.
        if (ConversionCatalog.GetTargets(format).Count == 0)
        {
            return isTextSelection
                ? new FileSelectionResult(FileSelectionKind.None, files, null, IsTextSelection: true)
                : FileSelectionResult.None;
        }

        return new FileSelectionResult(FileSelectionKind.Document, files, format, isTextSelection);
    }

    private static string GetExtension(IStorageFile file)
    {
        try
        {
            return file.FileType;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
