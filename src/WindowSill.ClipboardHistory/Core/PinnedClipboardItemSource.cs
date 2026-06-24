using Windows.ApplicationModel.DataTransfer;

namespace WindowSill.ClipboardHistory.Core;

/// <summary>
/// A clipboard item source backed by a persisted pinned item. The content is rebuilt into
/// a fresh <see cref="DataPackage"/> on demand so the same pin can be pasted repeatedly.
/// </summary>
internal sealed class PinnedClipboardItemSource : IClipboardItemSource
{
    private readonly Func<DataPackage> _rebuild;

    /// <param name="model">The persisted pinned item.</param>
    /// <param name="rebuild">
    /// Factory that produces a fresh <see cref="DataPackage"/> from the pinned content.
    /// </param>
    internal PinnedClipboardItemSource(PinnedClipboardItem model, Func<DataPackage> rebuild)
    {
        Model = model;
        _rebuild = rebuild;
        Data = rebuild().GetView();
    }

    /// <summary>
    /// Gets the persisted pinned item backing this source.
    /// </summary>
    internal PinnedClipboardItem Model { get; }

    /// <inheritdoc />
    public string Id => Model.Id;

    /// <inheritdoc />
    public bool IsPinned => true;

    /// <inheritdoc />
    public DataPackageView Data { get; }

    /// <inheritdoc />
    public void SetAsClipboardContent() => Clipboard.SetContent(_rebuild());

    /// <inheritdoc />
    public void DeleteFromHistory()
    {
        // Pinned items are not part of the Windows clipboard history; removal is handled
        // by the pin store when unpinning.
    }
}
