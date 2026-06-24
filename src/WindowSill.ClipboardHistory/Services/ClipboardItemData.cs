using WindowSill.ClipboardHistory.Core;

namespace WindowSill.ClipboardHistory.Services;

/// <summary>
/// Immutable data transfer object holding a clipboard item source, its detected data type,
/// and the canonical content signature used to deduplicate live items against pins.
/// </summary>
internal sealed record ClipboardItemData(
    IClipboardItemSource Source,
    DetectedClipboardDataType DataType,
    string ContentSignature);
