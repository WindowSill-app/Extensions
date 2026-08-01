using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Storage;
using WindowSill.API;
using WindowSill.FileHelper.Core;
using WindowSill.FileHelper.Helpers;

namespace WindowSill.FileHelper.ViewModels;

/// <summary>
/// ViewModel backing the instant, read-only ZIP archive metadata summary shown inline (non-clickable) in the sill.
/// Inspection runs once, asynchronously, as soon as the view model is constructed, so the view can show a brief
/// loading state and then the summary without the caller needing to orchestrate the async call itself.
/// </summary>
internal sealed partial class ZipInfoViewModel : ObservableObject
{
    private readonly IZipArchiveInspector _inspector;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZipInfoViewModel"/> class and immediately starts
    /// inspecting <paramref name="file"/> in the background.
    /// </summary>
    /// <param name="file">The single selected ZIP archive.</param>
    /// <param name="inspector">The archive inspector to use.</param>
    internal ZipInfoViewModel(IStorageFile file, IZipArchiveInspector inspector)
    {
        _inspector = inspector;
        FileName = file.Name;
        LoadAsync(file.Path).ForgetSafely();
    }

    /// <summary>
    /// Gets the display name of the selected archive.
    /// </summary>
    internal string FileName { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the archive is still being inspected.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    public partial bool IsLoading { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the archive could not be inspected (e.g. corrupt or unreadable ZIP).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    public partial bool HasError { get; set; }

    /// <summary>
    /// Gets or sets the number of files contained in the archive.
    /// </summary>
    [ObservableProperty]
    public partial int FileCount { get; set; }

    /// <summary>
    /// Gets or sets the number of distinct folder paths contained in the archive.
    /// </summary>
    [ObservableProperty]
    public partial int FolderCount { get; set; }

    /// <summary>
    /// Gets or sets the total compressed size, in bytes, of all file entries.
    /// </summary>
    [ObservableProperty]
    public partial long CompressedSizeInBytes { get; set; }

    /// <summary>
    /// Gets or sets the total uncompressed size, in bytes, of all file entries.
    /// </summary>
    [ObservableProperty]
    public partial long UncompressedSizeInBytes { get; set; }

    /// <summary>
    /// Gets or sets the already-localized, formatted compression outcome text (e.g. "42% smaller", "12% larger",
    /// "Same as original size"), or a "not available" message when the archive has no uncompressed content to
    /// compute a ratio from. This is the prominent "headline" line of the inline summary.
    /// </summary>
    [ObservableProperty]
    public partial string CompressionRatioText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the already-localized, composed file/folder count line (e.g. "28 files · 23 folders").
    /// </summary>
    [ObservableProperty]
    public partial string CountsText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the already-composed, friendly size line comparing uncompressed to compressed size
    /// (e.g. "36.3 MB → 14.2 MB").
    /// </summary>
    [ObservableProperty]
    public partial string SizesText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether compression made the archive smaller than its uncompressed content,
    /// so the UI can highlight this as a positive outcome.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSmaller { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether compression made the archive larger than its uncompressed content,
    /// so the UI can call out this counter-intuitive outcome.
    /// </summary>
    [ObservableProperty]
    public partial bool IsLarger { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there is no meaningfully positive or negative compression outcome
    /// to highlight (equal size, or no data available), so the UI can fall back to neutral styling.
    /// </summary>
    [ObservableProperty]
    public partial bool IsNeutral { get; set; }

    /// <summary>
    /// Gets or sets the per-file entries shown in the details popup, sorted largest-first. Replaced as a whole once
    /// inspection completes, so a bound list updates in one step.
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<ZipEntryListItemViewModel> Entries { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the archive has at least one file entry to list in the popup, so the
    /// popup can show an "empty archive" message instead of a blank list.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    public partial bool HasEntries { get; set; }

    /// <summary>
    /// Gets a value indicating whether the popup should show its "empty archive" message: inspection has finished,
    /// it succeeded, and the archive contained no file entries.
    /// </summary>
    public bool ShowEmptyState => !IsLoading && !HasError && !HasEntries;

    private async Task LoadAsync(string path)
    {
        try
        {
            ZipArchiveInspectionResult result = await _inspector.InspectAsync(path, CancellationToken.None);
            ZipArchiveSummary summary = result.Summary;

            // Build the popup's entry list off the UI thread: sort largest-first (by uncompressed size, then name
            // for a stable order among equal-sized entries) and pre-format every display string.
            IReadOnlyList<ZipEntryListItemViewModel> entries = result.Entries
                .OrderByDescending(entry => entry.UncompressedSizeInBytes)
                .ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(CreateEntryListItem)
                .ToList();

            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                FileCount = summary.FileCount;
                FolderCount = summary.FolderCount;
                CompressedSizeInBytes = summary.CompressedSizeInBytes;
                UncompressedSizeInBytes = summary.UncompressedSizeInBytes;
                Entries = entries;
                HasEntries = entries.Count > 0;

                ZipCompressionEffectiveness effectiveness = summary.CompressionEffectiveness;
                IsSmaller = effectiveness == ZipCompressionEffectiveness.Smaller;
                IsLarger = effectiveness == ZipCompressionEffectiveness.Larger;
                IsNeutral = !IsSmaller && !IsLarger;

                CompressionRatioText = effectiveness switch
                {
                    ZipCompressionEffectiveness.Smaller => string.Format(
                        CultureInfo.CurrentCulture,
                        "/WindowSill.FileHelper/ZipInfo/CompressionRatioSmallerFormat".GetLocalizedString(),
                        summary.CompressionPercentage),
                    ZipCompressionEffectiveness.Larger => string.Format(
                        CultureInfo.CurrentCulture,
                        "/WindowSill.FileHelper/ZipInfo/CompressionRatioLargerFormat".GetLocalizedString(),
                        summary.CompressionPercentage),
                    ZipCompressionEffectiveness.Equal => "/WindowSill.FileHelper/ZipInfo/CompressionRatioEqual".GetLocalizedString(),
                    _ => "/WindowSill.FileHelper/ZipInfo/CompressionRatioNotAvailable".GetLocalizedString(),
                };

                string filesText = string.Format(
                    CultureInfo.CurrentCulture,
                    (FileCount == 1
                        ? "/WindowSill.FileHelper/ZipInfo/FilesSingularFormat"
                        : "/WindowSill.FileHelper/ZipInfo/FilesPluralFormat").GetLocalizedString(),
                    FileCount);

                string foldersText = string.Format(
                    CultureInfo.CurrentCulture,
                    (FolderCount == 1
                        ? "/WindowSill.FileHelper/ZipInfo/FoldersSingularFormat"
                        : "/WindowSill.FileHelper/ZipInfo/FoldersPluralFormat").GetLocalizedString(),
                    FolderCount);

                CountsText = string.Format(
                    CultureInfo.CurrentCulture,
                    "/WindowSill.FileHelper/ZipInfo/CountsJoinFormat".GetLocalizedString(),
                    filesText,
                    foldersText);

                SizesText = string.Format(
                    CultureInfo.CurrentCulture,
                    "/WindowSill.FileHelper/ZipInfo/SizesFormat".GetLocalizedString(),
                    ByteSizeFormatter.Format(UncompressedSizeInBytes),
                    ByteSizeFormatter.Format(CompressedSizeInBytes));

                IsLoading = false;
            });
        }
        catch (Exception)
        {
            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                HasError = true;
                IsNeutral = true;
                IsLoading = false;
            });
        }
    }

    /// <summary>
    /// Builds a fully-formatted, localized display item for a single ZIP entry, including its size line and a
    /// per-entry compression outcome phrased with the same "smaller"/"larger"/"same" wording as the inline headline.
    /// </summary>
    private static ZipEntryListItemViewModel CreateEntryListItem(ZipEntryInfo entry)
    {
        string sizesText = string.Format(
            CultureInfo.CurrentCulture,
            "/WindowSill.FileHelper/ZipInfo/SizesFormat".GetLocalizedString(),
            ByteSizeFormatter.Format(entry.UncompressedSizeInBytes),
            ByteSizeFormatter.Format(entry.CompressedSizeInBytes));

        ZipCompressionEffectiveness effectiveness = entry.CompressionEffectiveness;

        string compressionText = effectiveness switch
        {
            ZipCompressionEffectiveness.Smaller => string.Format(
                CultureInfo.CurrentCulture,
                "/WindowSill.FileHelper/ZipInfo/CompressionRatioSmallerFormat".GetLocalizedString(),
                entry.CompressionPercentage),
            ZipCompressionEffectiveness.Larger => string.Format(
                CultureInfo.CurrentCulture,
                "/WindowSill.FileHelper/ZipInfo/CompressionRatioLargerFormat".GetLocalizedString(),
                entry.CompressionPercentage),
            ZipCompressionEffectiveness.Equal => "/WindowSill.FileHelper/ZipInfo/CompressionRatioEqual".GetLocalizedString(),
            _ => "/WindowSill.FileHelper/ZipInfo/EntryCompressionNotApplicable".GetLocalizedString(),
        };

        return new ZipEntryListItemViewModel(
            entry.Name,
            entry.RelativePath,
            sizesText,
            compressionText,
            IsSmaller: effectiveness == ZipCompressionEffectiveness.Smaller,
            IsLarger: effectiveness == ZipCompressionEffectiveness.Larger);
    }
}
