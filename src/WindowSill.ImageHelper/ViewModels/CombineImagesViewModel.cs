using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

using Windows.Storage;

using WindowSill.API;
using WindowSill.ImageHelper.Core;

namespace WindowSill.ImageHelper.ViewModels;

/// <summary>
/// ViewModel for the "combine images into a PDF" popup.
/// </summary>
/// <remarks>
/// There is nothing to choose before starting, so the popup runs the work as soon as it opens, in the same way the
/// compression popup does.
/// </remarks>
internal sealed partial class CombineImagesViewModel : ObservableObject
{
    private readonly IReadOnlyList<IStorageFile> _files;
    private readonly IImagePdfCombiner _combiner;
    private readonly Action _closePopup;

    private CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CombineImagesViewModel"/> class.
    /// </summary>
    /// <param name="files">The selected images.</param>
    /// <param name="combiner">The combiner that writes the PDF.</param>
    /// <param name="closePopup">Closes the popup.</param>
    internal CombineImagesViewModel(
        IReadOnlyList<IStorageFile> files,
        IImagePdfCombiner combiner,
        Action closePopup)
    {
        _files = files;
        _combiner = combiner;
        _closePopup = closePopup;
    }

    /// <summary>
    /// Gets the combine task, as a single-item collection so the view can reuse the task-list presentation.
    /// </summary>
    public ObservableCollection<CombineTaskItem> CombineTasks { get; } = new();

    /// <summary>
    /// Gets or sets the cancel/done button text.
    /// </summary>
    [ObservableProperty]
    public partial string ActionButtonText { get; set; } = string.Empty;

    /// <summary>
    /// Closes the popup.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _closePopup();
    }

    /// <summary>
    /// Starts the combine when the popup opens.
    /// </summary>
    internal void OnOpening()
    {
        ActionButtonText = "/WindowSill.ImageHelper/CombineImages/Cancel".GetLocalizedString();

        CombineTasks.Clear();

        // Explorer hands the selection over in click order; combining in the order the files are listed is what
        // someone assembling "page 1, page 2, page 3" scans expects.
        IReadOnlyList<string> orderedPaths =
        [
            .. _files
                .Select(file => file.Path)
                .Where(path => !string.IsNullOrEmpty(path))
                .OrderBy(System.IO.Path.GetFileName, NaturalStringComparer.Instance)
        ];

        if (orderedPaths.Count == 0)
        {
            ActionButtonText = "/WindowSill.ImageHelper/CombineImages/Done".GetLocalizedString();
            return;
        }

        CombineTasks.Add(new CombineTaskItem(orderedPaths, _combiner));
        RunCombineAsync(_cancellationTokenSource.Token).Forget();
    }

    /// <summary>
    /// Cancels pending work when the popup closes.
    /// </summary>
    internal void OnClosing()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    private async Task RunCombineAsync(CancellationToken cancellationToken)
    {
        try
        {
            await CombineTasks[0].CombineAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
        {
            // Cancellation is expected.
        }
        catch (Exception ex)
        {
            this.Log().LogError(ex, "Error while combining images into a PDF.");
        }

        ActionButtonText = "/WindowSill.ImageHelper/CombineImages/Done".GetLocalizedString();
    }
}
