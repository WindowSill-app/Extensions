using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using WindowSill.API;
using WindowSill.ClipboardHistory.Utils;

namespace WindowSill.ClipboardHistory.Services;

/// <summary>
/// Singleton service that centralizes clipboard history management and data type detection.
/// Computes <see cref="DetectedClipboardDataType"/> once per clipboard item on a background thread,
/// then notifies subscribers when the cache is updated.
/// </summary>
[Export(typeof(ClipboardHistoryDataService))]
internal sealed class ClipboardHistoryDataService
{
    private readonly DisposableSemaphore _semaphore = new();
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, DetectedClipboardDataType> _dataTypeCache = new();

    private IReadOnlyList<ClipboardItemData> _cachedItems = [];
    private int _subscriberCount;

    [ImportingConstructor]
    public ClipboardHistoryDataService()
    {
        _logger = this.Log();
    }

    /// <summary>
    /// Raised when the clipboard history data has been updated.
    /// Subscribers should refresh their UI with the new data from <see cref="GetCachedItems"/>.
    /// </summary>
    public event EventHandler? DataUpdated;

    /// <summary>
    /// Gets whether clipboard history is enabled in Windows.
    /// </summary>
    public bool IsHistoryEnabled => Clipboard.IsHistoryEnabled();

    /// <summary>
    /// Gets the current cached clipboard items with their detected data types.
    /// </summary>
    public IReadOnlyList<ClipboardItemData> GetCachedItems() => _cachedItems;

    /// <summary>
    /// Subscribes to clipboard events. Call this when a consumer activates.
    /// The service only listens to clipboard events when there's at least one subscriber.
    /// </summary>
    public void Subscribe()
    {
        if (Interlocked.Increment(ref _subscriberCount) == 1)
        {
            Clipboard.ContentChanged += Clipboard_ContentChanged;
            Clipboard.HistoryChanged += Clipboard_HistoryChanged;
            Clipboard.HistoryEnabledChanged += Clipboard_HistoryEnabledChanged;
        }
    }

    /// <summary>
    /// Unsubscribes from clipboard events. Call this when a consumer deactivates.
    /// </summary>
    public void Unsubscribe()
    {
        if (Interlocked.Decrement(ref _subscriberCount) == 0)
        {
            Clipboard.ContentChanged -= Clipboard_ContentChanged;
            Clipboard.HistoryChanged -= Clipboard_HistoryChanged;
            Clipboard.HistoryEnabledChanged -= Clipboard_HistoryEnabledChanged;
        }
    }

    /// <summary>
    /// Triggers a refresh of the clipboard history data.
    /// Data types are computed on a background thread, then <see cref="DataUpdated"/> is raised.
    /// </summary>
    /// <param name="maxItems">Maximum number of items to retrieve.</param>
    public async Task RefreshAsync(int maxItems)
    {
        await Task.Run(async () =>
        {
            ThreadHelper.ThrowIfOnUIThread();

            using (await _semaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false))
            {
                IReadOnlyList<ClipboardHistoryItem> clipboardItems = await GetClipboardHistoryItemsAsync(maxItems);

                // Compute data types on background thread
                var itemDataList = new List<ClipboardItemData>(clipboardItems.Count);
                foreach (ClipboardHistoryItem item in clipboardItems)
                {
                    DetectedClipboardDataType dataType = await GetOrComputeDataTypeAsync(item);
                    itemDataList.Add(new ClipboardItemData(item, dataType));
                }

                // Prune cache of items no longer in history
                PruneCache(clipboardItems);

                _cachedItems = itemDataList;
            }
        });
    }

    /// <summary>
    /// Clears the data type cache. Call this when settings change that affect how items are displayed
    /// (e.g., HidePasswords setting).
    /// </summary>
    public void ClearCache()
    {
        _dataTypeCache.Clear();
    }

    private async Task<DetectedClipboardDataType> GetOrComputeDataTypeAsync(ClipboardHistoryItem item)
    {
        if (_dataTypeCache.TryGetValue(item.Id, out DetectedClipboardDataType cachedType))
        {
            return cachedType;
        }

        try
        {
            DetectedClipboardDataType dataType = await DataHelper.GetDetectedClipboardDataTypeAsync(item);
            _dataTypeCache.TryAdd(item.Id, dataType);
            return dataType;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect clipboard data type for item {ItemId}.", item.Id);
            return DetectedClipboardDataType.Unknown;
        }
    }

    private void PruneCache(IReadOnlyList<ClipboardHistoryItem> currentItems)
    {
        var currentIds = new HashSet<string>(currentItems.Select(i => i.Id));
        foreach (string cachedId in _dataTypeCache.Keys)
        {
            if (!currentIds.Contains(cachedId))
            {
                _dataTypeCache.TryRemove(cachedId, out _);
            }
        }
    }

    private async Task<IReadOnlyList<ClipboardHistoryItem>> GetClipboardHistoryItemsAsync(int maxItems)
    {
        try
        {
            if (Clipboard.IsHistoryEnabled())
            {
                ClipboardHistoryItemsResult clipboardHistory = await Clipboard.GetHistoryItemsAsync();
                if (clipboardHistory.Status == ClipboardHistoryItemsResultStatus.Success)
                {
                    return clipboardHistory.Items
                        .Take(maxItems)
                        .ToList();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get clipboard history items.");
        }

        return [];
    }

    private void Clipboard_HistoryEnabledChanged(object? sender, object e)
    {
        // Subscribers will call RefreshAsync with their own maxItems setting
        DataUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void Clipboard_HistoryChanged(object? sender, ClipboardHistoryChangedEventArgs e)
    {
        // Subscribers will call RefreshAsync with their own maxItems setting
        DataUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void Clipboard_ContentChanged(object? sender, object e)
    {
        // Subscribers will call RefreshAsync with their own maxItems setting
        DataUpdated?.Invoke(this, EventArgs.Empty);
    }
}
