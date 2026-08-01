using System.Collections.ObjectModel;
using Windows.Storage;
using WindowSill.FileHelper.Core;

namespace WindowSill.FileHelper.Services;

/// <summary>
/// Defines the contract for managing document conversion queues that persist
/// independently of the UI lifecycle.
/// </summary>
internal interface IFileOperationService
{
    /// <summary>
    /// Gets the observable collection of all active and completed conversion queues.
    /// </summary>
    ObservableCollection<FileOperationQueue> Queues { get; }

    /// <summary>
    /// Starts converting the specified files to another format.
    /// </summary>
    /// <param name="files">The files to convert. All are expected to be of <paramref name="source"/> format.</param>
    /// <param name="source">The format the files are in.</param>
    /// <param name="target">The format to convert them to.</param>
    /// <returns>The created and running queue.</returns>
    FileOperationQueue StartConversion(IReadOnlyList<IStorageFile> files, DocumentFileFormat source, DocumentFileFormat target);

    /// <summary>
    /// Starts a PDF action that needs no configuration (split, compress) over the specified files.
    /// </summary>
    /// <param name="files">The selected PDFs, in selection order.</param>
    /// <param name="action">The action to perform.</param>
    /// <returns>The created and running queue.</returns>
    FileOperationQueue StartPdfAction(IReadOnlyList<IStorageFile> files, PdfAction action);

    /// <summary>
    /// Starts merging PDFs in a user-chosen order.
    /// </summary>
    /// <param name="orderedPaths">The PDFs to merge, in the order they should appear in the output.</param>
    /// <returns>The created and running queue.</returns>
    FileOperationQueue StartPdfMerge(IReadOnlyList<string> orderedPaths);

    /// <summary>
    /// Starts copying a user-chosen set of pages out of a PDF.
    /// </summary>
    /// <param name="sourcePath">The PDF to take pages from.</param>
    /// <param name="pageIndices">Zero-based indices of the pages to copy, in output order.</param>
    /// <returns>The created and running queue.</returns>
    FileOperationQueue StartPdfExtract(string sourcePath, IReadOnlyList<int> pageIndices);

    /// <summary>
    /// Starts rewriting the encoding or line endings of the specified text files.
    /// </summary>
    /// <param name="files">The selected text files.</param>
    /// <param name="action">The adjustment to make.</param>
    /// <returns>The created and running queue.</returns>
    FileOperationQueue StartTextAction(IReadOnlyList<IStorageFile> files, TextAction action);

    /// <summary>
    /// Starts adding or removing a PDF's open password.
    /// </summary>
    /// <param name="sourcePath">The PDF to protect or unlock.</param>
    /// <param name="password">The password to apply, or the current password when unlocking.</param>
    /// <param name="protect"><see langword="true"/> to add a password; <see langword="false"/> to remove it.</param>
    /// <returns>The created and running queue.</returns>
    FileOperationQueue StartPdfPassword(string sourcePath, string password, bool protect);
}
