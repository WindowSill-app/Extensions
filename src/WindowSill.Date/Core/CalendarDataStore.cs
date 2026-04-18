using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WindowSill.API;
using Path = System.IO.Path;

namespace WindowSill.Date.Core;

/// <summary>
/// Persists <see cref="AccountData"/> to DPAPI-encrypted files in the plugin data
/// folder. One file per account. Three operations: load all, save, delete.
/// Uses atomic writes (temp file + rename) to prevent data loss on crash.
/// </summary>
internal sealed class CalendarDataStore
{
    private const string FileExtension = ".dat";

    private readonly string _dataFolder;
    private readonly DisposableSemaphore _semaphore = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarDataStore"/> class.
    /// </summary>
    /// <param name="pluginDataFolder">The plugin's data folder path.</param>
    internal CalendarDataStore(string pluginDataFolder)
    {
        _dataFolder = pluginDataFolder;
        Directory.CreateDirectory(_dataFolder);
    }

    /// <summary>
    /// Discovers all saved accounts by scanning the data folder.
    /// Corrupt or unreadable files are silently skipped.
    /// </summary>
    internal async Task<AccountData[]> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        var accounts = new List<AccountData>();
        string[] files = Directory.GetFiles(_dataFolder, $"*{FileExtension}");

        foreach (string filePath in files)
        {
            AccountData? data = await ReadAsync(filePath, cancellationToken);
            if (data is not null)
            {
                accounts.Add(data);
            }
        }

        return [.. accounts];
    }

    /// <summary>
    /// Saves an account's data to its encrypted file. Atomic write via temp + rename.
    /// </summary>
    internal async Task SaveAsync(AccountData data, CancellationToken cancellationToken = default)
    {
        string filePath = Path.Combine(_dataFolder, GetFileName(data.Id));
        string tempPath = filePath + ".tmp";

        using IDisposable _ = await _semaphore.WaitAsync(cancellationToken);
        byte[] encrypted = Encrypt(data);
        await File.WriteAllBytesAsync(tempPath, encrypted, cancellationToken);
        File.Move(tempPath, filePath, overwrite: true);
    }

    /// <summary>
    /// Deletes an account's data file.
    /// </summary>
    internal async Task DeleteAsync(string accountId, CancellationToken cancellationToken = default)
    {
        using IDisposable _ = await _semaphore.WaitAsync(cancellationToken);

        string filePath = Path.Combine(_dataFolder, GetFileName(accountId));
        string tempPath = filePath + ".tmp";

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }
    }

    private async Task<AccountData?> ReadAsync(string filePath, CancellationToken cancellationToken)
    {
        using IDisposable _ = await _semaphore.WaitAsync(cancellationToken);
        try
        {
            byte[] encrypted = await File.ReadAllBytesAsync(filePath, cancellationToken);
            byte[] plaintext = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            string json = Encoding.UTF8.GetString(plaintext);
            return JsonSerializer.Deserialize<AccountData>(json, JsonSerializerOptions.Web);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static byte[] Encrypt(AccountData data)
    {
        string json = JsonSerializer.Serialize(data, JsonSerializerOptions.Web);
        byte[] plaintext = Encoding.UTF8.GetBytes(json);
        return ProtectedData.Protect(plaintext, null, DataProtectionScope.CurrentUser);
    }

    private static string GetFileName(string accountId)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string safe = new(accountId.Select(c => Array.IndexOf(invalidChars, c) >= 0 ? '_' : c).ToArray());
        return $"{safe}{FileExtension}";
    }
}
