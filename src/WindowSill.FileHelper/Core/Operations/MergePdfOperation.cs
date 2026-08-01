using Syncfusion.Pdf;

using WindowSill.API;

using Path = System.IO.Path;

namespace WindowSill.FileHelper.Core.Operations;

/// <summary>
/// Merges several PDFs, in the order they were selected, into a single document written beside the first file.
/// </summary>
/// <remarks>
/// This is the one operation that consumes the whole selection rather than a single file, which is why the queue
/// works in terms of <see cref="IFileOperation"/> rather than per-file conversions. Output safety (temp directory,
/// atomic non-overwriting move, "(n)" collision naming) is shared with the converters via
/// <see cref="SafeOutputWriter"/> rather than reimplemented here.
/// </remarks>
internal sealed class MergePdfOperation : IFileOperation
{
    private readonly IReadOnlyList<string> _sourcePaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="MergePdfOperation"/> class.
    /// </summary>
    /// <param name="sourcePaths">The PDFs to merge, in the order they should appear in the output.</param>
    internal MergePdfOperation(IReadOnlyList<string> sourcePaths)
    {
        _sourcePaths = sourcePaths;
        SyncfusionLicense.EnsureRegistered();
    }

    /// <inheritdoc />
    public string DisplayName
        => string.Format("/WindowSill.FileHelper/PdfActions/MergeTaskName".GetLocalizedString(), _sourcePaths.Count);

    /// <inheritdoc />
    public async Task<string?> ExecuteAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(0.05);

        string firstSource = _sourcePaths[0];
        string directory = Path.GetDirectoryName(firstSource) ?? string.Empty;
        string baseName = string.Format(
            "/WindowSill.FileHelper/PdfActions/MergedFileName".GetLocalizedString(),
            Path.GetFileNameWithoutExtension(firstSource));

        string finalPath = await Task.Run(
            () => SafeOutputWriter.WriteAndRelocate(
                directory,
                baseName,
                ConversionCatalog.GetInfo(DocumentFileFormat.Pdf).Extension,
                outputFilePath => MergeInto(outputFilePath, cancellationToken),
                cancellationToken),
            cancellationToken)
            .ConfigureAwait(false);

        progress?.Report(1.0);
        return finalPath;
    }

    private void MergeInto(string outputFilePath, CancellationToken cancellationToken)
    {
        // Streams are opened up front and kept alive for the whole merge: Syncfusion reads from each source lazily
        // while building the destination, so closing one early would corrupt the output.
        var sourceStreams = new List<FileStream>(_sourcePaths.Count);
        try
        {
            foreach (string sourcePath in _sourcePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                sourceStreams.Add(File.OpenRead(sourcePath));
            }

            using var destination = new PdfDocument();
            PdfDocumentBase.Merge(destination, [.. sourceStreams]);

            cancellationToken.ThrowIfCancellationRequested();

            using FileStream outputStream = File.Create(outputFilePath);
            destination.Save(outputStream);
        }
        finally
        {
            foreach (FileStream sourceStream in sourceStreams)
            {
                sourceStream.Dispose();
            }
        }
    }
}
