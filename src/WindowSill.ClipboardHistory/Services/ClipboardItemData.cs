using Windows.ApplicationModel.DataTransfer;

namespace WindowSill.ClipboardHistory.Services;

/// <summary>
/// Immutable data transfer object holding a clipboard history item and its detected data type.
/// </summary>
internal sealed record ClipboardItemData(
    ClipboardHistoryItem Item,
    DetectedClipboardDataType DataType);
