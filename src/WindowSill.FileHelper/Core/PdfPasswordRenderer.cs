using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Pdf.Security;

using WindowSill.API;

using Path = System.IO.Path;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// Adds or removes a PDF's open password.
/// </summary>
/// <remarks>
/// Removing a password is not a bypass: the current password must be supplied to open the document in the first
/// place. Both directions rewrite the file rather than editing in place, so the original is never at risk.
/// </remarks>
internal sealed class PdfPasswordRenderer : IDocumentRenderer
{
    private readonly string _password;
    private readonly bool _protect;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfPasswordRenderer"/> class.
    /// </summary>
    /// <param name="password">The password to apply, or the current password when unlocking.</param>
    /// <param name="protect"><see langword="true"/> to add a password; <see langword="false"/> to remove it.</param>
    internal PdfPasswordRenderer(string password, bool protect)
    {
        _password = password;
        _protect = protect;
        SyncfusionLicense.EnsureRegistered();
    }

    /// <inheritdoc />
    public void RenderToFile(string sourcePath, string outputFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using PdfLoadedDocument document = OpenDocument(sourcePath);

        if (_protect)
        {
            PdfSecurity security = document.Security;
            security.KeySize = PdfEncryptionKeySize.Key256Bit;
            security.Algorithm = PdfEncryptionAlgorithm.AES;
            security.UserPassword = _password;
        }
        else
        {
            // Clearing both passwords leaves the document readable by anyone.
            document.Security.UserPassword = string.Empty;
            document.Security.OwnerPassword = string.Empty;
        }

        cancellationToken.ThrowIfCancellationRequested();

        using FileStream output = File.Create(outputFilePath);
        document.Save(output);
    }

    private PdfLoadedDocument OpenDocument(string sourcePath)
    {
        try
        {
            // Protecting an already-unprotected file needs no password; unlocking needs the current one.
            return _protect
                ? PdfDocumentLoader.Load(sourcePath)
                : new PdfLoadedDocument(sourcePath, _password);
        }
        catch (PdfInvalidPasswordException)
        {
            throw new InvalidOperationException(
                "/WindowSill.FileHelper/PdfActions/ErrorWrongPassword".GetLocalizedString());
        }
    }
}
