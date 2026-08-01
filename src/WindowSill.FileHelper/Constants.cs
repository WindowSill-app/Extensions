namespace WindowSill.FileHelper;

internal static class Constants
{
    /// <summary>
    /// File extension for ZIP archives that trigger the instant ZIP metadata summary.
    /// </summary>
    /// <remarks>
    /// Document extensions deliberately do not live here: they are owned by
    /// <see cref="Core.ConversionCatalog"/>, which pairs each one with the format it represents and the conversions
    /// that format supports.
    /// </remarks>
    internal const string ZipExtension = ".zip";
}
