using Windows.ApplicationModel.DataTransfer;

namespace WindowSill.ClipboardHistory.Core;

/// <summary>
/// Abstraction over the origin of a clipboard item shown in the list. This frees the
/// view models from depending directly on a live <see cref="ClipboardHistoryItem"/>, so
/// the same view models can render both live Windows-history items and restored pinned
/// items that no longer have a live history entry.
/// </summary>
internal interface IClipboardItemSource
{
    /// <summary>
    /// Gets a stable identifier for the item. For live items this is the Windows history
    /// item id; for pinned items this is the persisted pin id.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets whether this source represents a pinned (persisted) item.
    /// </summary>
    bool IsPinned { get; }

    /// <summary>
    /// Gets the content view used by the view models to render the item.
    /// </summary>
    DataPackageView Data { get; }

    /// <summary>
    /// Places this item's content onto the system clipboard, ready to be pasted.
    /// </summary>
    void SetAsClipboardContent();

    /// <summary>
    /// Removes this item from the Windows clipboard history when applicable.
    /// Pinned sources do not touch Windows history.
    /// </summary>
    void DeleteFromHistory();
}
