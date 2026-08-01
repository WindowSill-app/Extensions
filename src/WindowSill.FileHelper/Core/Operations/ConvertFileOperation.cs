using WindowSill.FileHelper.Helpers;

using Path = System.IO.Path;

namespace WindowSill.FileHelper.Core.Operations;

/// <summary>
/// Runs an <see cref="IDocumentConverter"/> over a single selected file, placing the result beside it. This is the
/// operation behind both format conversion and the per-file PDF actions (compress, split).
/// </summary>
internal sealed class ConvertFileOperation : IFileOperation
{
    private readonly string _sourcePath;
    private readonly IDocumentConverter _converter;
    private readonly string? _outputNameSuffix;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConvertFileOperation"/> class.
    /// </summary>
    /// <param name="sourcePath">The file to process.</param>
    /// <param name="converter">The converter to run, already bound to a source/target pair.</param>
    /// <param name="outputNameSuffix">
    /// Optional suffix for the output's base name. Needed when the output shares the source's extension (e.g.
    /// compressing a PDF), so the result reads as "report - compressed.pdf" rather than "report (1).pdf".
    /// </param>
    internal ConvertFileOperation(string sourcePath, IDocumentConverter converter, string? outputNameSuffix = null)
    {
        _sourcePath = sourcePath;
        _converter = converter;
        _outputNameSuffix = outputNameSuffix;
    }

    /// <inheritdoc />
    public string DisplayName => Path.GetFileName(_sourcePath);

    /// <inheritdoc />
    public Task<string?> ExecuteAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        string directory = Path.GetDirectoryName(_sourcePath) ?? string.Empty;
        string baseName = Path.GetFileNameWithoutExtension(_sourcePath) + _outputNameSuffix;
        string outputPath = OutputPathHelper.GetCandidatePath(directory, baseName, _converter.OutputExtension, counter: 0);

        return _converter.ConvertAsync(_sourcePath, outputPath, progress, cancellationToken);
    }
}
