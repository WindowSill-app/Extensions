namespace WindowSill.FileHelper.Core;

/// <summary>
/// Renders a source document to a single output file in some target format. Implementations perform only the raw
/// document conversion; the surrounding <see cref="DocumentConverter"/> owns the temp-directory isolation and the
/// atomic, collision-safe relocation of the produced output beside the source.
/// </summary>
/// <remarks>
/// A renderer writes its primary output to the requested <c>outputFilePath</c>. Some formats (notably Markdown for a
/// document that contains images) additionally emit sibling resource files/folders next to that path; the converter
/// detects and relocates those as a set, so renderers do not need to worry about where the output ultimately lands.
/// </remarks>
internal interface IDocumentRenderer
{
    /// <summary>
    /// Renders <paramref name="sourcePath"/> to <paramref name="outputFilePath"/>.
    /// </summary>
    /// <param name="sourcePath">Path to the source document. Its format is fixed by the renderer instance.</param>
    /// <param name="outputFilePath">The path the primary output file should be written to.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="System.Exception">Thrown when the document is missing, corrupt, or cannot be rendered.</exception>
    void RenderToFile(string sourcePath, string outputFilePath, CancellationToken cancellationToken = default);
}
