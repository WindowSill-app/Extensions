using WindowSill.API;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// Describes one <see cref="DocumentFileFormat"/>: the extension FileHelper writes for it, the extensions it accepts
/// when recognizing a selected file, and the label shown on its conversion button.
/// </summary>
/// <param name="Format">The format being described.</param>
/// <param name="Family">The kind of document this format represents.</param>
/// <param name="Extension">The extension used when writing this format, including the leading dot.</param>
/// <param name="InputExtensions">Every extension recognized as this format, including <paramref name="Extension"/>.</param>
/// <param name="ResourceKey">Key of the button label in <c>ConvertDocument.resw</c>.</param>
internal sealed record DocumentFileFormatInfo(
    DocumentFileFormat Format,
    DocumentFamily Family,
    string Extension,
    IReadOnlyList<string> InputExtensions,
    string ResourceKey)
{
    /// <summary>
    /// Gets the localized label for this format.
    /// </summary>
    /// <remarks>
    /// Resolved on each access rather than captured at construction: descriptors are built in a static initializer,
    /// which can run before the host's localization provider is ready, and the label must also follow a display
    /// language change at runtime.
    /// </remarks>
    internal string DisplayName
        => $"/WindowSill.FileHelper/ConvertDocument/{ResourceKey}".GetLocalizedString();
}
