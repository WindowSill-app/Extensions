using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;

using WindowSill.API;

using Path = System.IO.Path;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// Splits a PDF into one file per page. The pages are written as siblings of the requested output path rather than
/// to the path itself, so <see cref="SafeOutputWriter"/> relocates the whole set into a dedicated subfolder beside
/// the source instead of scattering loose files next to it.
/// </summary>
internal sealed class PdfSplitRenderer : IDocumentRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PdfSplitRenderer"/> class, registering the Syncfusion license
    /// (if one is available) before any Syncfusion API is used.
    /// </summary>
    internal PdfSplitRenderer()
    {
        SyncfusionLicense.EnsureRegistered();
    }

    /// <inheritdoc />
    public void RenderToFile(string sourcePath, string outputFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string directory = Path.GetDirectoryName(outputFilePath) ?? string.Empty;

        // Page files are named after the SOURCE document, not the output path, so they read naturally
        // ("report - page 1.pdf") inside the folder the writer creates for them.
        string baseName = Path.GetFileNameWithoutExtension(sourcePath);
        string pageNameFormat = "/WindowSill.FileHelper/PdfActions/PageFileName".GetLocalizedString();

        using PdfLoadedDocument loaded = PdfDocumentLoader.Load(sourcePath);

        int pageCount = loaded.Pages.Count;
        for (int i = 0; i < pageCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var singlePage = new PdfDocument();
            singlePage.ImportPage(loaded, i);

            string pageFileName = string.Format(pageNameFormat, baseName, i + 1) + ".pdf";
            using FileStream outputStream = File.Create(Path.Combine(directory, pageFileName));
            singlePage.Save(outputStream);
        }
    }
}
