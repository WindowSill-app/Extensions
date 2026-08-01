using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Storage;
using WindowSill.API;
using WindowSill.FileHelper.Core;

using Path = System.IO.Path;

namespace WindowSill.FileHelper.ViewModels;

/// <summary>
/// One PDF in the merge order list: its first page as a thumbnail, its name, and how many pages it contributes.
/// </summary>
internal sealed partial class MergeFileViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MergeFileViewModel"/> class.
    /// </summary>
    /// <param name="file">The PDF this row represents.</param>
    /// <param name="moveUpCommand">Command that moves this row one position earlier.</param>
    /// <param name="moveDownCommand">Command that moves this row one position later.</param>
    internal MergeFileViewModel(IStorageFile file, ICommand moveUpCommand, ICommand moveDownCommand)
    {
        File = file;
        FileName = Path.GetFileName(file.Path);
        MoveUpCommand = moveUpCommand;
        MoveDownCommand = moveDownCommand;
    }

    /// <summary>
    /// Gets the underlying file.
    /// </summary>
    internal IStorageFile File { get; }

    /// <summary>
    /// Gets the file name shown in the list.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets the command that moves this row one position earlier. Carried on the item because a
    /// <c>DataTemplate</c> cannot reach the page's view model with <c>x:Bind</c>.
    /// </summary>
    public ICommand MoveUpCommand { get; }

    /// <summary>
    /// Gets the command that moves this row one position later.
    /// </summary>
    public ICommand MoveDownCommand { get; }

    /// <summary>
    /// Gets or sets the first-page thumbnail, once rendered.
    /// </summary>
    [ObservableProperty]
    public partial BitmapImage? Thumbnail { get; set; }

    /// <summary>
    /// Gets or sets the page-count caption (e.g. "3 pages"), once known.
    /// </summary>
    [ObservableProperty]
    public partial string PageCountText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the 1-based position of this file in the merge order.
    /// </summary>
    [ObservableProperty]
    public partial int Position { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this file can move earlier in the order.
    /// </summary>
    [ObservableProperty]
    public partial bool CanMoveUp { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this file can move later in the order.
    /// </summary>
    [ObservableProperty]
    public partial bool CanMoveDown { get; set; }
}

/// <summary>
/// ViewModel for the merge arrangement page: shows the selected PDFs in the order they will be combined and lets
/// the user reorder them before starting.
/// </summary>
/// <remarks>
/// Reordering is done with explicit move buttons rather than drag-and-drop: it stays usable by keyboard and screen
/// reader, and it is deterministic to automate, which drag-and-drop inside a virtualized list is not.
/// </remarks>
internal sealed partial class MergeOrderViewModel : ObservableObject, IDisposable
{
    private readonly ConvertDocumentPopupViewModel _owner;
    private readonly List<PdfPagePreview> _previews = [];
    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// Width, in pixels, of the first-page thumbnails. Small enough to render several instantly, large enough to
    /// recognize a document at a glance.
    /// </summary>
    private const int ThumbnailPixelWidth = 96;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MergeOrderViewModel"/> class.
    /// </summary>
    /// <param name="owner">The popup view model that starts the merge once confirmed.</param>
    internal MergeOrderViewModel(ConvertDocumentPopupViewModel owner)
    {
        _owner = owner;

        foreach (IStorageFile file in owner.Files)
        {
            Files.Add(new MergeFileViewModel(file, MoveUpCommand, MoveDownCommand));
        }

        RefreshPositions();
    }

    /// <summary>
    /// Gets the files in the order they will be merged.
    /// </summary>
    public ObservableCollection<MergeFileViewModel> Files { get; } = [];

    /// <summary>
    /// Gets the confirm button caption, e.g. "Merge 3 files".
    /// </summary>
    public string ConfirmText
        => string.Format("/WindowSill.FileHelper/PdfActions/MergeConfirm".GetLocalizedString(), Files.Count);

    /// <summary>
    /// Loads each file's first-page thumbnail and page count.
    /// </summary>
    internal async Task LoadPreviewsAsync()
    {
        foreach (MergeFileViewModel item in Files)
        {
            if (_disposed)
            {
                return;
            }

            PdfPagePreview? preview = await PdfPagePreview.TryLoadAsync(item.File, _cts.Token);
            if (preview is null)
            {
                continue;
            }

            _previews.Add(preview);

            item.PageCountText = string.Format(
                preview.PageCount == 1
                    ? "/WindowSill.FileHelper/PdfActions/PageCountOne".GetLocalizedString()
                    : "/WindowSill.FileHelper/PdfActions/PageCountMany".GetLocalizedString(),
                preview.PageCount);

            item.Thumbnail = await preview.RenderPageAsync(0, ThumbnailPixelWidth, _cts.Token);
        }
    }

    /// <summary>
    /// Moves a file one position earlier in the merge order.
    /// </summary>
    [RelayCommand]
    private void MoveUp(MergeFileViewModel item)
    {
        int index = Files.IndexOf(item);
        if (index > 0)
        {
            Files.Move(index, index - 1);
            RefreshPositions();
        }
    }

    /// <summary>
    /// Moves a file one position later in the merge order.
    /// </summary>
    [RelayCommand]
    private void MoveDown(MergeFileViewModel item)
    {
        int index = Files.IndexOf(item);
        if (index >= 0 && index < Files.Count - 1)
        {
            Files.Move(index, index + 1);
            RefreshPositions();
        }
    }

    /// <summary>
    /// Starts the merge using the current order.
    /// </summary>
    [RelayCommand]
    private void Confirm()
    {
        _owner.StartMerge([.. Files.Select(f => f.File.Path)]);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts.Cancel();

        foreach (PdfPagePreview preview in _previews)
        {
            preview.Dispose();
        }

        _previews.Clear();
        _cts.Dispose();
    }

    /// <summary>
    /// Re-numbers the rows and re-evaluates which move buttons apply, so the first row cannot move up and the last
    /// cannot move down.
    /// </summary>
    private void RefreshPositions()
    {
        for (int i = 0; i < Files.Count; i++)
        {
            Files[i].Position = i + 1;
            Files[i].CanMoveUp = i > 0;
            Files[i].CanMoveDown = i < Files.Count - 1;
        }
    }
}
