using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// Renders page thumbnails for a PDF using the Windows OS rasterizer (<see cref="PdfDocument"/>), so the extension
/// ships no third-party PDF rendering engine.
/// </summary>
/// <remarks>
/// <para>
/// Rendering is deliberately bounded in three ways, because a picker can ask for every page of a large document at
/// once:
/// </para>
/// <list type="bullet">
/// <item>A semaphore caps how many pages rasterize concurrently, so scrolling a 500-page document cannot spawn 500
/// simultaneous renders.</item>
/// <item>Finished bitmaps are kept in a capped least-recently-used cache keyed by page and pixel width, so pages
/// scrolled back into view are instant without growing memory without bound.</item>
/// <item><see cref="Dispose"/> drops the document and the cache promptly, because the underlying pages hold native
/// memory that would otherwise linger until finalization.</item>
/// </list>
/// </remarks>
internal sealed class PdfPagePreview : IDisposable
{
    /// <summary>
    /// How many pages may rasterize at once. Two keeps the UI responsive while still overlapping the next render
    /// with the current one.
    /// </summary>
    private const int MaxConcurrentRenders = 2;

    /// <summary>
    /// How many rendered thumbnails to keep. Comfortably covers a scrolled viewport in both directions without
    /// letting a large document pin every page's bitmap in memory.
    /// </summary>
    private const int MaxCachedPages = 60;

    private readonly SemaphoreSlim _renderGate = new(MaxConcurrentRenders, MaxConcurrentRenders);
    private readonly Dictionary<PageKey, LinkedListNode<PageKey>> _cacheNodes = [];
    private readonly Dictionary<PageKey, BitmapImage> _cache = [];
    private readonly LinkedList<PageKey> _cacheOrder = [];

    private PdfDocument? _document;
    private bool _disposed;

    private PdfPagePreview(PdfDocument document)
    {
        _document = document;
    }

    /// <summary>
    /// Gets the number of pages in the document, or 0 once disposed.
    /// </summary>
    internal int PageCount => (int)(_document?.PageCount ?? 0);

    /// <summary>
    /// Opens a PDF for preview.
    /// </summary>
    /// <param name="file">The PDF to open.</param>
    /// <param name="cancellationToken">Token to cancel the load.</param>
    /// <returns>The preview, or <see langword="null"/> when the document cannot be opened (e.g. it is
    /// password-protected or corrupt) — callers fall back to a page-less experience rather than failing outright.</returns>
    internal static async Task<PdfPagePreview?> TryLoadAsync(IStorageFile file, CancellationToken cancellationToken)
    {
        try
        {
            StorageFile storageFile = file as StorageFile ?? await StorageFile.GetFileFromPathAsync(file.Path).AsTask(cancellationToken);
            PdfDocument document = await PdfDocument.LoadFromFileAsync(storageFile).AsTask(cancellationToken);
            return new PdfPagePreview(document);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Password-protected, corrupt, or otherwise unreadable by the OS rasterizer.
            return null;
        }
    }

    /// <summary>
    /// Gets the aspect ratio (height divided by width) of a page, used to size its placeholder before the thumbnail
    /// has rendered so the layout does not jump.
    /// </summary>
    /// <param name="pageIndex">Zero-based page index.</param>
    /// <returns>The ratio, defaulting to US Letter when the page cannot be measured.</returns>
    internal double GetPageAspectRatio(int pageIndex)
    {
        const double LetterRatio = 792.0 / 612.0;

        if (_document is null || pageIndex < 0 || pageIndex >= PageCount)
        {
            return LetterRatio;
        }

        try
        {
            using PdfPage page = _document.GetPage((uint)pageIndex);
            double width = Math.Max(1, page.Size.Width);
            double height = Math.Max(1, page.Size.Height);
            return height / width;
        }
        catch (Exception)
        {
            return LetterRatio;
        }
    }

    /// <summary>
    /// Renders a page thumbnail, reusing a cached bitmap when one of the same width is already available.
    /// </summary>
    /// <param name="pageIndex">Zero-based page index.</param>
    /// <param name="pixelWidth">Desired bitmap width in physical pixels.</param>
    /// <param name="cancellationToken">Token to cancel the render.</param>
    /// <returns>The rendered thumbnail, or <see langword="null"/> if the page could not be rendered.</returns>
    internal async Task<BitmapImage?> RenderPageAsync(int pageIndex, int pixelWidth, CancellationToken cancellationToken)
    {
        if (_disposed || _document is null || pageIndex < 0 || pageIndex >= PageCount)
        {
            return null;
        }

        var key = new PageKey(pageIndex, pixelWidth);
        if (TryGetCached(key, out BitmapImage? cached))
        {
            return cached;
        }

        await _renderGate.WaitAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            // Another realization of the same page may have finished while this one waited for the gate.
            if (TryGetCached(key, out cached))
            {
                return cached;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (_disposed || _document is null)
            {
                return null;
            }

            using PdfPage page = _document.GetPage((uint)pageIndex);
            using var stream = new InMemoryRandomAccessStream();

            await page.RenderToStreamAsync(stream, new PdfPageRenderOptions { DestinationWidth = (uint)pixelWidth })
                .AsTask(cancellationToken)
                .ConfigureAwait(true);

            stream.Seek(0);

            var bitmap = new BitmapImage
            {
                DecodePixelType = DecodePixelType.Physical,
                DecodePixelWidth = pixelWidth,
            };
            await bitmap.SetSourceAsync(stream).AsTask(cancellationToken).ConfigureAwait(true);

            AddToCache(key, bitmap);
            return bitmap;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // A single unrenderable page should leave the rest of the picker usable.
            return null;
        }
        finally
        {
            _renderGate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _cache.Clear();
        _cacheNodes.Clear();
        _cacheOrder.Clear();

        // Dropping the document releases the native page memory rather than waiting for finalization.
        _document = null;

        _renderGate.Dispose();
    }

    private bool TryGetCached(PageKey key, out BitmapImage? bitmap)
    {
        if (_cache.TryGetValue(key, out BitmapImage? found) && _cacheNodes.TryGetValue(key, out LinkedListNode<PageKey>? node))
        {
            _cacheOrder.Remove(node);
            _cacheOrder.AddFirst(node);
            bitmap = found;
            return true;
        }

        bitmap = null;
        return false;
    }

    private void AddToCache(PageKey key, BitmapImage bitmap)
    {
        if (_disposed || _cache.ContainsKey(key))
        {
            return;
        }

        var node = new LinkedListNode<PageKey>(key);
        _cacheOrder.AddFirst(node);
        _cacheNodes[key] = node;
        _cache[key] = bitmap;

        while (_cache.Count > MaxCachedPages && _cacheOrder.Last is { } last)
        {
            _cache.Remove(last.Value);
            _cacheNodes.Remove(last.Value);
            _cacheOrder.RemoveLast();
        }
    }

    private readonly record struct PageKey(int PageIndex, int PixelWidth);
}
