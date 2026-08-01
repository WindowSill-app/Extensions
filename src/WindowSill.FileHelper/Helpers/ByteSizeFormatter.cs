using System.Globalization;

namespace WindowSill.FileHelper.Helpers;

/// <summary>
/// Formats a raw byte count into a compact, human-readable size string (e.g. <c>"14.2 MB"</c>) using binary
/// (1024-based) units, matching how Windows File Explorer reports archive sizes. Kept separate from the view so
/// the (culture-aware) formatting logic can be unit-tested without any UI dependency.
/// </summary>
internal static class ByteSizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB", "PB"];

    /// <summary>
    /// Formats <paramref name="bytes"/> as a friendly size string. Whole numbers are used for raw bytes; a single
    /// decimal place (dropped when it would be <c>.0</c>) is used for KB and larger, so values stay compact enough
    /// for the limited width of a sill item. Negative inputs are clamped to zero.
    /// </summary>
    /// <param name="bytes">The size, in bytes.</param>
    /// <returns>A localized, compact size string such as <c>"0 B"</c>, <c>"1.5 KB"</c> or <c>"14.2 MB"</c>.</returns>
    internal static string Format(long bytes)
    {
        double size = bytes < 0 ? 0d : bytes;

        int unitIndex = 0;
        while (size >= 1024d && unitIndex < Units.Length - 1)
        {
            size /= 1024d;
            unitIndex++;
        }

        string number = unitIndex == 0
            ? size.ToString("0", CultureInfo.CurrentCulture)
            : size.ToString("0.#", CultureInfo.CurrentCulture);

        return $"{number} {Units[unitIndex]}";
    }
}
