using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Storage;
using WindowSill.API;
using WindowSill.FileHelper.Core;

namespace WindowSill.FileHelper.ViewModels;

/// <summary>
/// One page in the extract picker: its thumbnail, its number, and whether the user has ticked it.
/// </summary>
internal sealed partial class PdfPageItemViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PdfPageItemViewModel"/> class.
    /// </summary>
    /// <param name="pageIndex">Zero-based page index.</param>
    /// <param name="aspectRatio">Page height divided by width, used to size the placeholder before it renders.</param>
    internal PdfPageItemViewModel(int pageIndex, double aspectRatio)
    {
        PageIndex = pageIndex;
        AspectRatio = aspectRatio;
    }

    /// <summary>
    /// Gets the zero-based page index.
    /// </summary>
    internal int PageIndex { get; }

    /// <summary>
    /// Gets the 1-based page number shown under the thumbnail.
    /// </summary>
    public int PageNumber => PageIndex + 1;

    /// <summary>
    /// Gets the page's height-to-width ratio.
    /// </summary>
    public double AspectRatio { get; }

    /// <summary>
    /// Gets the thumbnail height that keeps the page's aspect ratio at the picker's fixed thumbnail width.
    /// </summary>
    public double ThumbnailHeight => ExtractPagesViewModel.ThumbnailDisplayWidth * AspectRatio;

    /// <summary>
    /// Gets or sets the rendered thumbnail, once available.
    /// </summary>
    [ObservableProperty]
    public partial BitmapImage? Thumbnail { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this page will be extracted.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// Gets a stable automation id so UI tests can tick a specific page.
    /// </summary>
    public string AutomationId => $"PdfPage{PageNumber}";
}

/// <summary>
/// ViewModel for the extract page picker: shows every page of the selected PDF as a thumbnail and extracts the
/// ticked ones into a new document.
/// </summary>
/// <remarks>
/// Thumbnails are rendered on demand as the list realizes each item rather than up front, so opening a large
/// document stays instant and only the pages the user actually scrolls to are rasterized.
/// </remarks>
internal sealed partial class ExtractPagesViewModel : ObservableObject, IDisposable
{
    /// <summary>
    /// Logical width of each thumbnail in the grid.
    /// </summary>
    internal const double ThumbnailDisplayWidth = 110;

    private readonly ConvertDocumentPopupViewModel _owner;
    private readonly IStorageFile _file;
    private readonly CancellationTokenSource _cts = new();

    private PdfPagePreview? _preview;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractPagesViewModel"/> class.
    /// </summary>
    /// <param name="owner">The popup view model that starts the extraction once confirmed.</param>
    /// <param name="file">The PDF to pick pages from.</param>
    internal ExtractPagesViewModel(ConvertDocumentPopupViewModel owner, IStorageFile file)
    {
        _owner = owner;
        _file = file;
    }

    /// <summary>
    /// Gets the pages of the document, in order.
    /// </summary>
    public ObservableCollection<PdfPageItemViewModel> Pages { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the document is still being opened.
    /// </summary>
    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the document could not be opened for preview.
    /// </summary>
    [ObservableProperty]
    public partial bool HasFailed { get; set; }

    /// <summary>
    /// Gets or sets how many pages are currently ticked.
    /// </summary>
    [ObservableProperty]
    public partial int SelectedCount { get; set; }

    /// <summary>
    /// Gets the confirm button caption, e.g. "Extract 3 pages".
    /// </summary>
    public string ConfirmText
        => string.Format(
            SelectedCount == 1
                ? "/WindowSill.FileHelper/PdfActions/ExtractConfirmOne".GetLocalizedString()
                : "/WindowSill.FileHelper/PdfActions/ExtractConfirmMany".GetLocalizedString(),
            SelectedCount);

    /// <summary>
    /// Gets a value indicating whether at least one page is ticked.
    /// </summary>
    public bool CanConfirm => SelectedCount > 0;

    /// <summary>
    /// Opens the document and builds a placeholder entry for every page. Thumbnails are filled in later, as the
    /// list realizes each item.
    /// </summary>
    internal async Task LoadAsync()
    {
        _preview = await PdfPagePreview.TryLoadAsync(_file, _cts.Token);

        if (_disposed)
        {
            return;
        }

        if (_preview is null || _preview.PageCount == 0)
        {
            HasFailed = true;
            IsLoading = false;
            return;
        }

        for (int i = 0; i < _preview.PageCount; i++)
        {
            var item = new PdfPageItemViewModel(i, _preview.GetPageAspectRatio(i));
            item.PropertyChanged += PageItem_PropertyChanged;
            Pages.Add(item);
        }

        IsLoading = false;
    }

    /// <summary>
    /// Renders a page's thumbnail if it has not been rendered yet. Called by the view when the item is realized.
    /// </summary>
    /// <param name="item">The page whose thumbnail is needed.</param>
    internal async Task EnsureThumbnailAsync(PdfPageItemViewModel item)
    {
        if (_disposed || _preview is null || item.Thumbnail is not null)
        {
            return;
        }

        try
        {
            int pixelWidth = (int)Math.Ceiling(ThumbnailDisplayWidth * 2);
            BitmapImage? bitmap = await _preview.RenderPageAsync(item.PageIndex, pixelWidth, _cts.Token);

            if (!_disposed && bitmap is not null)
            {
                item.Thumbnail = bitmap;
            }
        }
        catch (OperationCanceledException)
        {
            // The picker closed while this page was rendering.
        }
    }

    /// <summary>
    /// Ticks every page.
    /// </summary>
    [RelayCommand]
    private void SelectAll()
    {
        foreach (PdfPageItemViewModel page in Pages)
        {
            page.IsSelected = true;
        }
    }

    /// <summary>
    /// Unticks every page.
    /// </summary>
    [RelayCommand]
    private void ClearSelection()
    {
        foreach (PdfPageItemViewModel page in Pages)
        {
            page.IsSelected = false;
        }
    }

    /// <summary>
    /// Extracts the ticked pages, in document order.
    /// </summary>
    [RelayCommand]
    private void Confirm()
    {
        int[] selected = [.. Pages.Where(p => p.IsSelected).Select(p => p.PageIndex)];
        if (selected.Length == 0)
        {
            return;
        }

        _owner.StartExtract(_file.Path, selected);
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

        foreach (PdfPageItemViewModel page in Pages)
        {
            page.PropertyChanged -= PageItem_PropertyChanged;
        }

        _preview?.Dispose();
        _preview = null;
        _cts.Dispose();
    }

    private void PageItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PdfPageItemViewModel.IsSelected))
        {
            SelectedCount = Pages.Count(p => p.IsSelected);
        }
    }

    partial void OnSelectedCountChanged(int value)
    {
        OnPropertyChanged(nameof(ConfirmText));
        OnPropertyChanged(nameof(CanConfirm));
        ConfirmCommand.NotifyCanExecuteChanged();
    }
}
