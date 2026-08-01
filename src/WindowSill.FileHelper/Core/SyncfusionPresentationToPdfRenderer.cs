using Syncfusion.Pdf;
using Syncfusion.Presentation;
using Syncfusion.PresentationRenderer;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// Renders a PowerPoint deck to PDF, preserving slide layout, so it can be shared with someone who has no
/// presentation application.
/// </summary>
internal sealed class SyncfusionPresentationToPdfRenderer : IDocumentRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncfusionPresentationToPdfRenderer"/> class, registering the
    /// Syncfusion license (if one is available) before any Syncfusion API is used.
    /// </summary>
    internal SyncfusionPresentationToPdfRenderer()
    {
        SyncfusionLicense.EnsureRegistered();
    }

    /// <inheritdoc />
    public void RenderToFile(string sourcePath, string outputFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using IPresentation presentation = Presentation.Open(sourcePath);

        // The renderer has to be attached before conversion; without it the slides come out blank.
        presentation.PresentationRenderer = new PresentationRenderer();

        cancellationToken.ThrowIfCancellationRequested();

        using PdfDocument pdf = PresentationToPdfConverter.Convert(presentation);

        cancellationToken.ThrowIfCancellationRequested();

        using FileStream output = File.Create(outputFilePath);
        pdf.Save(output);
    }
}
