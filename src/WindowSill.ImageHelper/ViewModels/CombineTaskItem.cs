using CommunityToolkit.Mvvm.ComponentModel;

using WindowSill.API;
using WindowSill.ImageHelper.Core;
using WindowSill.ImageHelper.Helpers;

using Path = System.IO.Path;

namespace WindowSill.ImageHelper.ViewModels;

/// <summary>
/// Represents the single "combine into PDF" task, with observable progress state.
/// </summary>
/// <remarks>
/// Unlike conversion and compression, combining is one task for the whole selection rather than one per file, so this
/// item is named after the PDF it produces.
/// </remarks>
internal sealed partial class CombineTaskItem : ObservableObject
{
    private readonly IReadOnlyList<string> _sourcePaths;
    private readonly IImagePdfCombiner _combiner;
    private readonly string _outputPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="CombineTaskItem"/> class.
    /// </summary>
    /// <param name="sourcePaths">The images to combine, in page order.</param>
    /// <param name="combiner">The combiner that writes the PDF.</param>
    internal CombineTaskItem(IReadOnlyList<string> sourcePaths, IImagePdfCombiner combiner)
    {
        _sourcePaths = sourcePaths;
        _combiner = combiner;

        // The PDF is named after the first image, so it lands beside the selection with a predictable name.
        _outputPath = FilePathHelper.GetUniqueOutputPath(sourcePaths[0], string.Empty, "pdf");

        IsRunning = true;
    }

    /// <summary>
    /// Gets the name of the PDF being produced.
    /// </summary>
    public string FileName => Path.GetFileName(_outputPath);

    /// <summary>
    /// Gets a short description of what the PDF will contain, e.g. "3 images".
    /// </summary>
    public string PageSummary
        => string.Format("/WindowSill.ImageHelper/CombineImages/PageSummary".GetLocalizedString(), _sourcePaths.Count);

    /// <summary>
    /// Gets or sets whether the work is currently running.
    /// </summary>
    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    /// <summary>
    /// Gets or sets whether the PDF was produced.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSucceeded { get; set; }

    /// <summary>
    /// Gets or sets whether the work failed.
    /// </summary>
    [ObservableProperty]
    public partial bool IsFailed { get; set; }

    /// <summary>
    /// Runs the combine.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    internal async Task CombineAsync(CancellationToken cancellationToken)
    {
        bool isSucceeded = false;
        try
        {
            await _combiner.CombineAsync(_sourcePaths, _outputPath, cancellationToken);
            isSucceeded = true;
        }
        catch (Exception)
        {
            // Reported through IsFailed below.
        }

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            IsRunning = false;
            IsSucceeded = isSucceeded;
            IsFailed = !isSucceeded;
        });
    }
}
