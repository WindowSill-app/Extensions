using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;

using WindowSill.API;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// Loads PDFs for the FileHelper PDF actions, translating Syncfusion's low-level load failures into messages the
/// user can act on.
/// </summary>
/// <remarks>
/// A password-protected PDF is by far the most common reason a load fails, and Syncfusion's own exception text is
/// not something to surface verbatim in the result list, so it is replaced with an explanation of what to do.
/// </remarks>
internal static class PdfDocumentLoader
{
    /// <summary>
    /// Opens a PDF for reading.
    /// </summary>
    /// <param name="sourcePath">Path to the PDF to open.</param>
    /// <returns>The loaded document. The caller owns it and must dispose it.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the PDF is password-protected, carrying a user-facing explanation.
    /// </exception>
    internal static PdfLoadedDocument Load(string sourcePath)
    {
        try
        {
            return new PdfLoadedDocument(sourcePath);
        }
        catch (PdfInvalidPasswordException)
        {
            throw new InvalidOperationException(
                "/WindowSill.FileHelper/PdfActions/ErrorPasswordProtected".GetLocalizedString());
        }
    }
}
