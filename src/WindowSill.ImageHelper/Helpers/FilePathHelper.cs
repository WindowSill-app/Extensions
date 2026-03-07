using Path = System.IO.Path;

namespace WindowSill.ImageHelper.Helpers;

/// <summary>
/// Provides helper methods for generating output file paths that avoid overwriting original files.
/// </summary>
internal static class FilePathHelper
{
    /// <summary>
    /// Generates a unique output file path by adding a suffix to the file name.
    /// If the file already exists, appends a number (e.g., "_compressed_1").
    /// </summary>
    /// <param name="originalPath">The original file path.</param>
    /// <param name="suffix">The suffix to add (e.g., "_compressed", "_resized").</param>
    /// <param name="newExtension">Optional new extension (without dot). If null, keeps the original extension.</param>
    /// <returns>A unique file path that doesn't exist.</returns>
    internal static string GetUniqueOutputPath(string originalPath, string suffix, string? newExtension = null)
    {
        string directory = Path.GetDirectoryName(originalPath) ?? string.Empty;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
        string extension = newExtension ?? Path.GetExtension(originalPath).TrimStart('.');

        string baseName = $"{fileNameWithoutExtension}{suffix}";
        string newPath = Path.Combine(directory, $"{baseName}.{extension}");

        // If the file exists, append a number until we find a unique name
        int counter = 1;
        while (File.Exists(newPath))
        {
            newPath = Path.Combine(directory, $"{baseName}_{counter}.{extension}");
            counter++;
        }

        return newPath;
    }
}
