using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WindowSill.API;
using Path = System.IO.Path;

namespace WindowSill.ClipboardHistory.Core;

/// <summary>
/// Persists <see cref="PinnedClipboardItem"/> to DPAPI-encrypted files in the plugin data
/// folder. One JSON file per pin (<c>{id}.dat</c>) plus an optional encrypted image blob
/// (<c>{id}.img</c>). Encryption is bound to the current Windows user
/// (<see cref="DataProtectionScope.CurrentUser"/>), so files cannot be read by another
/// user or on another machine. Writes are atomic (temp file + rename) and serialized with
/// a semaphore; all IO runs off the UI thread.
/// </summary>
internal sealed class PinnedClipboardStore
{
    private const string FolderName = "Pinned";
    private const string DataExtension = ".dat";
    private const string ImageExtension = ".img";

    private readonly string _dataFolder;
    private readonly DisposableSemaphore _semaphore = new();

    /// <param name="pluginDataFolder">The plugin's writable data folder.</param>
    internal PinnedClipboardStore(string pluginDataFolder)
    {
        _dataFolder = Path.Combine(pluginDataFolder, FolderName);
        Directory.CreateDirectory(_dataFolder);
    }

    /// <summary>
    /// Loads all persisted pins, ordered by pin time. Corrupt or unreadable files are
    /// skipped silently.
    /// </summary>
    internal async Task<List<PinnedClipboardItem>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<PinnedClipboardItem>();
        string[] files = Directory.GetFiles(_dataFolder, $"*{DataExtension}");

        foreach (string filePath in files)
        {
            PinnedClipboardItem? item = await ReadAsync(filePath, cancellationToken);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return [.. items.OrderBy(i => i.PinnedAt)];
    }

    /// <summary>
    /// Saves a pin and its optional image blob. Atomic write via temp + rename.
    /// </summary>
    internal async Task SaveAsync(PinnedClipboardItem item, CancellationToken cancellationToken = default)
    {
        using IDisposable _ = await _semaphore.WaitAsync(cancellationToken);

        if (item.HasImage && item.ImageBytes is { Length: > 0 })
        {
            await WriteEncryptedAsync(GetImagePath(item.Id), item.ImageBytes, cancellationToken);
        }

        string json = JsonSerializer.Serialize(item, JsonSerializerOptions.Web);
        byte[] plaintext = Encoding.UTF8.GetBytes(json);
        await WriteEncryptedAsync(GetDataPath(item.Id), plaintext, cancellationToken);
    }

    /// <summary>
    /// Deletes a pin's JSON file and image blob, if present.
    /// </summary>
    internal async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        using IDisposable _ = await _semaphore.WaitAsync(cancellationToken);

        foreach (string path in new[] { GetDataPath(id), GetImagePath(id), GetDataPath(id) + ".tmp", GetImagePath(id) + ".tmp" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private async Task<PinnedClipboardItem?> ReadAsync(string filePath, CancellationToken cancellationToken)
    {
        using IDisposable _ = await _semaphore.WaitAsync(cancellationToken);
        try
        {
            byte[] encrypted = await File.ReadAllBytesAsync(filePath, cancellationToken);
            byte[] plaintext = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            string json = Encoding.UTF8.GetString(plaintext);
            PinnedClipboardItem? item = JsonSerializer.Deserialize<PinnedClipboardItem>(json, JsonSerializerOptions.Web);

            if (item is not null && item.HasImage)
            {
                string imagePath = GetImagePath(item.Id);
                if (File.Exists(imagePath))
                {
                    byte[] encryptedImage = await File.ReadAllBytesAsync(imagePath, cancellationToken);
                    item.ImageBytes = ProtectedData.Unprotect(encryptedImage, null, DataProtectionScope.CurrentUser);
                }
            }

            return item;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static async Task WriteEncryptedAsync(string filePath, byte[] plaintext, CancellationToken cancellationToken)
    {
        byte[] encrypted = ProtectedData.Protect(plaintext, null, DataProtectionScope.CurrentUser);
        string tempPath = filePath + ".tmp";
        await File.WriteAllBytesAsync(tempPath, encrypted, cancellationToken);
        File.Move(tempPath, filePath, overwrite: true);
    }

    private string GetDataPath(string id) => Path.Combine(_dataFolder, SafeFileName(id) + DataExtension);

    private string GetImagePath(string id) => Path.Combine(_dataFolder, SafeFileName(id) + ImageExtension);

    private static string SafeFileName(string id)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        return new string([.. id.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c)]);
    }
}
