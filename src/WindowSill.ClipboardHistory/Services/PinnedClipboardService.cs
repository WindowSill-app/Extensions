using System.ComponentModel.Composition;
using Windows.Storage;
using Windows.Storage.Streams;
using WindowSill.API;
using WindowSill.ClipboardHistory.Core;

namespace WindowSill.ClipboardHistory.Services;

/// <summary>
/// Singleton service that owns the persisted pinned clipboard items. Pins survive Windows
/// clipboard history eviction and application restarts. All capture, encryption, and file
/// IO runs off the UI thread.
/// </summary>
[Export(typeof(PinnedClipboardService))]
internal sealed class PinnedClipboardService
{
    private readonly PinnedClipboardStore _store;
    private readonly DisposableSemaphore _semaphore = new();
    private readonly List<ClipboardItemData> _pinned = [];
    private readonly HashSet<string> _signatures = new(StringComparer.Ordinal);

    private bool _loaded;

    [ImportingConstructor]
    public PinnedClipboardService(IPluginInfo pluginInfo)
    {
        _store = new PinnedClipboardStore(pluginInfo.GetPluginDataFolder());
    }

    /// <summary>
    /// Raised when the set of pinned items changes.
    /// </summary>
    public event EventHandler? PinsChanged;

    /// <summary>
    /// Gets a snapshot of the pinned items, newest first.
    /// </summary>
    public IReadOnlyList<ClipboardItemData> GetPinnedItems()
        => [.. _pinned.OrderByDescending(p => ((PinnedClipboardItemSource)p.Source).Model.PinnedAt)];

    /// <summary>
    /// Gets whether an item with the given content signature is already pinned.
    /// </summary>
    public bool IsPinnedSignature(string signature) => _signatures.Contains(signature);

    /// <summary>
    /// Loads persisted pins into memory. Safe to call multiple times.
    /// </summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        using IDisposable _ = await _semaphore.WaitAsync(cancellationToken);
        if (_loaded)
        {
            return;
        }

        List<PinnedClipboardItem> models = await _store.LoadAllAsync(cancellationToken);
        foreach (PinnedClipboardItem model in models)
        {
            ClipboardItemData data = await CreateDataAsync(model);
            _pinned.Add(data);
            _signatures.Add(model.ContentSignature);
        }

        _loaded = true;
    }

    /// <summary>
    /// Pins the given clipboard item. Returns <c>false</c> if the item is already pinned.
    /// </summary>
    public async Task<bool> PinAsync(ClipboardItemData itemData, CancellationToken cancellationToken = default)
    {
        using IDisposable _ = await _semaphore.WaitAsync(cancellationToken);

        if (_signatures.Contains(itemData.ContentSignature))
        {
            return false;
        }

        PinnedClipboardItem model = await ClipboardContentCapturer.CaptureAsync(itemData.Source.Data, itemData.DataType);
        await _store.SaveAsync(model, cancellationToken);

        ClipboardItemData data = await CreateDataAsync(model);
        _pinned.Add(data);
        _signatures.Add(model.ContentSignature);

        PinsChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>
    /// Removes the pin with the given id.
    /// </summary>
    public async Task UnpinAsync(string id, CancellationToken cancellationToken = default)
    {
        using IDisposable _ = await _semaphore.WaitAsync(cancellationToken);

        int index = _pinned.FindIndex(p => string.Equals(p.Source.Id, id, StringComparison.Ordinal));
        if (index < 0)
        {
            return;
        }

        _signatures.Remove(((PinnedClipboardItemSource)_pinned[index].Source).Model.ContentSignature);
        _pinned.RemoveAt(index);
        await _store.DeleteAsync(id, cancellationToken);

        PinsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static async Task<ClipboardItemData> CreateDataAsync(PinnedClipboardItem model)
    {
        RandomAccessStreamReference? imageReference =
            await PinnedClipboardRebuilder.CreateImageReferenceAsync(model.ImageBytes);
        IReadOnlyList<IStorageItem>? storageItems =
            await PinnedClipboardRebuilder.ResolveStorageItemsAsync(model.FilePaths);

        var source = new PinnedClipboardItemSource(
            model,
            () => PinnedClipboardRebuilder.Build(model, imageReference, storageItems));

        return new ClipboardItemData(source, model.DataType, model.ContentSignature);
    }
}
