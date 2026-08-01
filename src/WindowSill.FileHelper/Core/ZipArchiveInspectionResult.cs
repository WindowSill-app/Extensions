namespace WindowSill.FileHelper.Core;

/// <summary>
/// The full result of inspecting a ZIP archive's central directory: the aggregate <see cref="ZipArchiveSummary"/>
/// used for the compact inline sill summary, plus the per-entry <see cref="ZipEntryInfo"/> list used by the details
/// popup. Both are produced from a single central-directory read, so no entry content is ever extracted and the
/// archive is opened only once.
/// </summary>
/// <param name="Summary">The aggregate metadata (counts and total sizes) for the whole archive.</param>
/// <param name="Entries">
/// The individual file entries in the archive (folders excluded), in the archive's own central-directory order.
/// Presentation concerns such as sorting are the responsibility of the caller.
/// </param>
internal sealed record ZipArchiveInspectionResult(
    ZipArchiveSummary Summary,
    IReadOnlyList<ZipEntryInfo> Entries);
