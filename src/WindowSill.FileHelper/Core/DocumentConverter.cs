using Path = System.IO.Path;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// Converts a document to a target format by delegating the document rendering to an
/// <see cref="IDocumentRenderer"/> and wrapping it with <see cref="SafeOutputWriter"/>'s safe file handling, so a
/// cancelled or failed conversion never leaves a partial/corrupt file, nor a leftover temp directory, behind.
/// </summary>
internal sealed class DocumentConverter : IDocumentConverter
{
    private readonly IDocumentRenderer _renderer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentConverter"/> class.
    /// </summary>
    /// <param name="renderer">The engine that renders a source document to <paramref name="outputExtension"/>.</param>
    /// <param name="outputExtension">The output file extension, including the leading dot (e.g. <c>.pdf</c>).</param>
    internal DocumentConverter(IDocumentRenderer renderer, string outputExtension)
    {
        _renderer = renderer;
        OutputExtension = outputExtension;
    }

    /// <inheritdoc />
    public string OutputExtension { get; }

    /// <inheritdoc />
    public async Task<string?> ConvertAsync(
        string sourcePath,
        string outputPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(0.05);

        string directory = Path.GetDirectoryName(outputPath) ?? string.Empty;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(outputPath);
        string extension = Path.GetExtension(outputPath);

        // Rendering is synchronous, CPU-bound work; run it off the caller's thread. The whole render + relocate
        // happens inside the Task so a cancelled/failed conversion never leaves anything behind.
        string finalPath = await Task.Run(
            () => SafeOutputWriter.WriteAndRelocate(
                directory,
                fileNameWithoutExtension,
                extension,
                tempOutputFile => _renderer.RenderToFile(sourcePath, tempOutputFile, cancellationToken),
                cancellationToken),
            cancellationToken)
            .ConfigureAwait(false);

        progress?.Report(1.0);
        return finalPath;
    }
}
