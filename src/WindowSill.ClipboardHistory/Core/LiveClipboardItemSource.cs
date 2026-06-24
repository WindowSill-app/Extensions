using Windows.ApplicationModel.DataTransfer;

namespace WindowSill.ClipboardHistory.Core;

/// <summary>
/// A clipboard item source backed by a live Windows clipboard history item.
/// </summary>
internal sealed class LiveClipboardItemSource : IClipboardItemSource
{
    private readonly ClipboardHistoryItem _item;

    internal LiveClipboardItemSource(ClipboardHistoryItem item)
    {
        _item = item;
    }

    /// <summary>
    /// Gets the underlying Windows clipboard history item.
    /// </summary>
    internal ClipboardHistoryItem Item => _item;

    /// <inheritdoc />
    public string Id => _item.Id;

    /// <inheritdoc />
    public bool IsPinned => false;

    /// <inheritdoc />
    public DataPackageView Data => _item.Content;

    /// <inheritdoc />
    public void SetAsClipboardContent() => Clipboard.SetHistoryItemAsContent(_item);

    /// <inheritdoc />
    public void DeleteFromHistory() => Clipboard.DeleteItemFromHistory(_item);
}
