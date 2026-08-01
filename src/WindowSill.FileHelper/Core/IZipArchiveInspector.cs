namespace WindowSill.FileHelper.Core;

/// <summary>
/// Reads ZIP archive metadata from its central directory, without extracting any entry's content.
/// </summary>
internal interface IZipArchiveInspector
{
    /// <summary>
    /// Reads the central directory of the ZIP archive located at <paramref name="zipFilePath"/> and computes a
    /// <see cref="ZipArchiveInspectionResult"/> from it, containing both the aggregate summary and the per-entry list.
    /// </summary>
    /// <param name="zipFilePath">The full path to the ZIP archive to inspect.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the computed <see cref="ZipArchiveInspectionResult"/>.</returns>
    /// <exception cref="System.IO.InvalidDataException">Thrown when the file is not a valid ZIP archive.</exception>
    Task<ZipArchiveInspectionResult> InspectAsync(string zipFilePath, CancellationToken cancellationToken = default);
}
