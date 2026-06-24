using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WindowSill.ClipboardHistory.Core;

/// <summary>
/// Reconstructs a <see cref="DataPackage"/> from a persisted <see cref="PinnedClipboardItem"/>
/// so the existing per-type view models can render it and it can be pasted. Image and file
/// resources are resolved once (asynchronously) and reused by the synchronous builder.
/// </summary>
internal static class PinnedClipboardRebuilder
{
    /// <summary>
    /// Creates a reusable image stream reference from raw image bytes, or <c>null</c> when
    /// there is no image payload.
    /// </summary>
    internal static async Task<RandomAccessStreamReference?> CreateImageReferenceAsync(byte[]? imageBytes)
    {
        if (imageBytes is not { Length: > 0 })
        {
            return null;
        }

        var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(imageBytes.AsBuffer());
        stream.Seek(0);
        return RandomAccessStreamReference.CreateFromStream(stream);
    }

    /// <summary>
    /// Resolves the persisted file paths into storage items, skipping any that no longer
    /// exist. Returns <c>null</c> when there are no resolvable items.
    /// </summary>
    internal static async Task<IReadOnlyList<IStorageItem>?> ResolveStorageItemsAsync(string[]? filePaths)
    {
        if (filePaths is not { Length: > 0 })
        {
            return null;
        }

        var resolved = new List<IStorageItem>(filePaths.Length);
        foreach (string path in filePaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    resolved.Add(await StorageFile.GetFileFromPathAsync(path));
                }
                else if (Directory.Exists(path))
                {
                    resolved.Add(await StorageFolder.GetFolderFromPathAsync(path));
                }
            }
            catch (Exception)
            {
                // Skip files that can no longer be resolved.
            }
        }

        return resolved.Count > 0 ? resolved : null;
    }

    /// <summary>
    /// Builds a fresh <see cref="DataPackage"/> for the pinned item using pre-resolved
    /// image and storage resources. Safe to call repeatedly (once per paste).
    /// </summary>
    internal static DataPackage Build(
        PinnedClipboardItem model,
        RandomAccessStreamReference? imageReference,
        IReadOnlyList<IStorageItem>? storageItems)
    {
        var package = new DataPackage();

        switch (model.DataType)
        {
            case DetectedClipboardDataType.Image:
                if (imageReference is not null)
                {
                    package.SetBitmap(imageReference);
                }
                break;

            case DetectedClipboardDataType.File:
                if (storageItems is { Count: > 0 })
                {
                    package.SetStorageItems(storageItems);
                }
                else if (model.FilePaths is { Length: > 0 })
                {
                    package.SetText(string.Join(Environment.NewLine, model.FilePaths));
                }
                break;

            case DetectedClipboardDataType.Html:
                if (!string.IsNullOrEmpty(model.Html))
                {
                    package.SetHtmlFormat(model.Html);
                }
                if (!string.IsNullOrEmpty(model.Text))
                {
                    package.SetText(model.Text);
                }
                break;

            case DetectedClipboardDataType.Rtf:
                if (!string.IsNullOrEmpty(model.Rtf))
                {
                    package.SetRtf(model.Rtf);
                }
                if (!string.IsNullOrEmpty(model.Text))
                {
                    package.SetText(model.Text);
                }
                break;

            case DetectedClipboardDataType.Uri:
                string? urlText = model.Text ?? model.Uri;
                if (Uri.TryCreate(model.Uri, UriKind.Absolute, out Uri? uri))
                {
                    package.SetWebLink(uri);
                }
                if (!string.IsNullOrEmpty(urlText))
                {
                    package.SetText(urlText);
                }
                break;

            default:
                string? text = model.Text ?? model.Color;
                if (!string.IsNullOrEmpty(text))
                {
                    package.SetText(text);
                }
                break;
        }

        return package;
    }
}
