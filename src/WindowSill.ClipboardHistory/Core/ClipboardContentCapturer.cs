using System.Security.Cryptography;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WindowSill.ClipboardHistory.Core;

/// <summary>
/// Extracts the content of a clipboard item into a persistable <see cref="PinnedClipboardItem"/>
/// and computes the canonical content signature used to deduplicate the live history list
/// against pinned items. All work is intended to run on a background thread.
/// </summary>
internal static class ClipboardContentCapturer
{
    /// <summary>
    /// Captures the content of the given clipboard data into a new pinned item, including
    /// any image bytes (held in memory on the returned item).
    /// </summary>
    internal static async Task<PinnedClipboardItem> CaptureAsync(DataPackageView data, DetectedClipboardDataType dataType)
    {
        var item = new PinnedClipboardItem
        {
            DataType = dataType,
            PinnedAt = DateTimeOffset.UtcNow,
        };

        IReadOnlyList<string> formats = data.AvailableFormats;

        if (formats.Contains(StandardDataFormats.Text))
        {
            item.Text = await data.GetTextAsync();
        }

        switch (dataType)
        {
            case DetectedClipboardDataType.Html:
                if (formats.Contains(StandardDataFormats.Html))
                {
                    item.Html = await data.GetHtmlFormatAsync();
                }
                break;

            case DetectedClipboardDataType.Rtf:
                if (formats.Contains(StandardDataFormats.Rtf))
                {
                    item.Rtf = await data.GetRtfAsync();
                }
                break;

            case DetectedClipboardDataType.Uri:
                item.Uri = item.Text;
                if (string.IsNullOrEmpty(item.Uri) && formats.Contains(StandardDataFormats.WebLink))
                {
                    item.Uri = (await data.GetWebLinkAsync()).ToString();
                }
                break;

            case DetectedClipboardDataType.Color:
                item.Color = item.Text;
                break;

            case DetectedClipboardDataType.File:
                item.FilePaths = await GetFilePathsAsync(data);
                break;

            case DetectedClipboardDataType.Image:
                item.ImageBytes = await GetImageBytesAsync(data);
                item.HasImage = item.ImageBytes is { Length: > 0 };
                break;
        }

        item.ContentSignature = ComputeSignature(dataType, item.Text, item.Uri, item.Color, item.FilePaths, item.ImageBytes);
        return item;
    }

    /// <summary>
    /// Computes the canonical content signature for a live clipboard item so it can be
    /// matched against pinned items. Mirrors the signature stored on pinned items.
    /// </summary>
    internal static async Task<string> ComputeSignatureAsync(DataPackageView data, DetectedClipboardDataType dataType)
    {
        string? text = null;
        string? uri = null;
        string? color = null;
        string[]? filePaths = null;
        byte[]? imageBytes = null;

        IReadOnlyList<string> formats = data.AvailableFormats;

        if (formats.Contains(StandardDataFormats.Text))
        {
            text = await data.GetTextAsync();
        }

        switch (dataType)
        {
            case DetectedClipboardDataType.Uri:
                uri = text;
                if (string.IsNullOrEmpty(uri) && formats.Contains(StandardDataFormats.WebLink))
                {
                    uri = (await data.GetWebLinkAsync()).ToString();
                }
                break;

            case DetectedClipboardDataType.Color:
                color = text;
                break;

            case DetectedClipboardDataType.File:
                filePaths = await GetFilePathsAsync(data);
                break;

            case DetectedClipboardDataType.Image:
                imageBytes = await GetImageBytesAsync(data);
                break;
        }

        return ComputeSignature(dataType, text, uri, color, filePaths, imageBytes);
    }

    private static string ComputeSignature(
        DetectedClipboardDataType dataType,
        string? text,
        string? uri,
        string? color,
        string[]? filePaths,
        byte[]? imageBytes)
    {
        string canonical = dataType switch
        {
            DetectedClipboardDataType.Image when imageBytes is { Length: > 0 }
                => "img:" + Convert.ToHexString(SHA256.HashData(imageBytes)),
            DetectedClipboardDataType.File when filePaths is { Length: > 0 }
                => "file:" + string.Join('|', filePaths),
            DetectedClipboardDataType.Uri => "uri:" + (uri ?? string.Empty),
            DetectedClipboardDataType.Color => "color:" + (color ?? string.Empty),
            _ => $"{dataType}:{text ?? string.Empty}",
        };

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash);
    }

    private static async Task<string[]?> GetFilePathsAsync(DataPackageView data)
    {
        if (!data.AvailableFormats.Contains(StandardDataFormats.StorageItems))
        {
            return null;
        }

        IReadOnlyList<IStorageItem> items = await data.GetStorageItemsAsync();
        return [.. items.Select(i => i.Path).Where(p => !string.IsNullOrEmpty(p))];
    }

    private static async Task<byte[]?> GetImageBytesAsync(DataPackageView data)
    {
        if (!data.AvailableFormats.Contains(StandardDataFormats.Bitmap))
        {
            return null;
        }

        RandomAccessStreamReference streamRef = await data.GetBitmapAsync();
        using IRandomAccessStreamWithContentType stream = await streamRef.OpenReadAsync();
        using var memory = new MemoryStream();
        using (Stream input = stream.AsStreamForRead())
        {
            await input.CopyToAsync(memory);
        }

        return memory.ToArray();
    }
}
