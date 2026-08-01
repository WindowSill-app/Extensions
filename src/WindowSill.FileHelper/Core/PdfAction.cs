using WindowSill.API;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// An action FileHelper can perform on a PDF selection.
/// </summary>
internal enum PdfAction
{
    /// <summary>Combine several PDFs, in a user-chosen order, into one document.</summary>
    Merge,

    /// <summary>Write each page of a PDF to its own file inside a folder beside the source.</summary>
    Split,

    /// <summary>Rewrite a PDF with images recompressed, fonts subset and metadata stripped.</summary>
    Compress,

    /// <summary>Copy a user-chosen set of pages out of a PDF into a new document.</summary>
    Extract,

    /// <summary>Save every page as a PNG image inside a folder beside the source.</summary>
    SaveAsImages,

    /// <summary>Require a password to open the document.</summary>
    Protect,

    /// <summary>Remove the document's open password, given the current one.</summary>
    Unlock,
}

/// <summary>
/// Describes one <see cref="PdfAction"/> for display.
/// </summary>
/// <param name="Action">The action being described.</param>
/// <param name="ResourceKey">Key of the button label in <c>PdfActions.resw</c>.</param>
/// <param name="RequiresConfiguration">
/// Whether choosing this action opens a configuration page (choosing pages, arranging files) instead of starting
/// the work straight away.
/// </param>
internal sealed record PdfActionInfo(PdfAction Action, string ResourceKey, bool RequiresConfiguration = false)
{
    /// <summary>
    /// Gets the localized label for this action. Resolved on each access so it follows a display language change
    /// and never runs before the host's localization provider is ready.
    /// </summary>
    internal string DisplayName
        => $"/WindowSill.FileHelper/PdfActions/{ResourceKey}".GetLocalizedString();
}
