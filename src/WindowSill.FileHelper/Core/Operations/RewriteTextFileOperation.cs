using Path = System.IO.Path;

namespace WindowSill.FileHelper.Core.Operations;

/// <summary>
/// Rewrites a text file with a different encoding and/or line-ending style, leaving its content untouched.
/// </summary>
/// <remarks>
/// The file is read with its detected encoding rather than an assumed one, so accented characters survive the
/// round trip instead of being mangled by a wrong guess.
/// </remarks>
internal sealed class RewriteTextFileOperation : IFileOperation
{
    private readonly string _sourcePath;
    private readonly TextEncodingKind? _targetEncoding;
    private readonly LineEndingKind? _targetLineEnding;
    private readonly string _outputNameSuffix;

    /// <summary>
    /// Initializes a new instance of the <see cref="RewriteTextFileOperation"/> class.
    /// </summary>
    /// <param name="sourcePath">The text file to rewrite.</param>
    /// <param name="targetEncoding">The encoding to write, or <see langword="null"/> to keep the detected one.</param>
    /// <param name="targetLineEnding">The line ending to write, or <see langword="null"/> to leave line breaks alone.</param>
    /// <param name="outputNameSuffix">Suffix appended to the output's base name, e.g. " (UTF-8)".</param>
    internal RewriteTextFileOperation(
        string sourcePath,
        TextEncodingKind? targetEncoding,
        LineEndingKind? targetLineEnding,
        string outputNameSuffix)
    {
        _sourcePath = sourcePath;
        _targetEncoding = targetEncoding;
        _targetLineEnding = targetLineEnding;
        _outputNameSuffix = outputNameSuffix;
    }

    /// <inheritdoc />
    public string DisplayName => Path.GetFileName(_sourcePath);

    /// <inheritdoc />
    public async Task<string?> ExecuteAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(0.05);

        string directory = Path.GetDirectoryName(_sourcePath) ?? string.Empty;
        string baseName = Path.GetFileNameWithoutExtension(_sourcePath) + _outputNameSuffix;
        string extension = Path.GetExtension(_sourcePath);

        string finalPath = await Task.Run(
            () => SafeOutputWriter.WriteAndRelocate(
                directory,
                baseName,
                extension,
                outputFilePath => Rewrite(outputFilePath),
                cancellationToken),
            cancellationToken)
            .ConfigureAwait(false);

        progress?.Report(1.0);
        return finalPath;
    }

    private void Rewrite(string outputFilePath)
    {
        (string content, TextEncodingKind detected) = TextFileReader.Read(_sourcePath);

        if (_targetLineEnding is LineEndingKind lineEnding)
        {
            content = TextFileReader.NormalizeLineEndings(content, lineEnding);
        }

        System.Text.Encoding encoding = TextFileReader.ToEncoding(_targetEncoding ?? detected);
        File.WriteAllText(outputFilePath, content, encoding);
    }
}
