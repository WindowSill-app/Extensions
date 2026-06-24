using System.Text.Json.Serialization;

namespace WindowSill.ClipboardHistory.Core;

/// <summary>
/// A pinned clipboard item persisted to disk so it survives Windows clipboard history
/// eviction, history clears, and application restarts. Image payloads are stored in a
/// separate encrypted blob (not inline in JSON); the in-memory <see cref="ImageBytes"/>
/// holds the decrypted bytes while the app is running.
/// </summary>
internal sealed class PinnedClipboardItem
{
    /// <summary>
    /// Stable identifier assigned when the item is pinned.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// The detected data type, used to reconstruct the clipboard package and pick a view.
    /// </summary>
    public DetectedClipboardDataType DataType { get; set; }

    /// <summary>
    /// When the item was pinned. Used to order the pinned section.
    /// </summary>
    public DateTimeOffset PinnedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Canonical content signature used to deduplicate the live history list against pins.
    /// </summary>
    public string ContentSignature { get; set; } = string.Empty;

    public string? Text { get; set; }

    public string? Html { get; set; }

    public string? Rtf { get; set; }

    public string? Uri { get; set; }

    public string? Color { get; set; }

    public string[]? FilePaths { get; set; }

    /// <summary>
    /// Whether this item has an associated image blob stored alongside the JSON file.
    /// </summary>
    public bool HasImage { get; set; }

    /// <summary>
    /// The decrypted image bytes held in memory while running. Never serialized to JSON.
    /// </summary>
    [JsonIgnore]
    public byte[]? ImageBytes { get; set; }
}
