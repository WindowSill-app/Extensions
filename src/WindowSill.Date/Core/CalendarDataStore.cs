using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WindowSill.API;
using WindowSill.Date.Core.Models;
using Path = System.IO.Path;

namespace WindowSill.Date.Core;

/// <summary>
/// Persists <see cref="CalendarAccount"/> to DPAPI-encrypted files in the plugin data
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
    internal async Task<CalendarAccount[]> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        var accounts = new List<CalendarAccount>();
        string[] files = Directory.GetFiles(_dataFolder, $"*{FileExtension}");

        foreach (string filePath in files)
        {
            CalendarAccount? account = await ReadAsync(filePath, cancellationToken);
            if (account is not null)
            {
                accounts.Add(account);
            }
        }

        return [.. accounts];
    }

    /// <summary>
    /// Saves an account to its encrypted file. Atomic write via temp + rename.
    /// </summary>
    internal async Task SaveAsync(CalendarAccount account, CancellationToken cancellationToken = default)
    {
        string filePath = Path.Combine(_dataFolder, GetFileName(account.Id));
        string tempPath = filePath + ".tmp";

        using IDisposable _ = await _semaphore.WaitAsync(cancellationToken);
        byte[] encrypted = Encrypt(account);
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

    private async Task<CalendarAccount?> ReadAsync(string filePath, CancellationToken cancellationToken)
    {
        using IDisposable _ = await _semaphore.WaitAsync(cancellationToken);
        try
        {
            byte[] encrypted = await File.ReadAllBytesAsync(filePath, cancellationToken);
            byte[] plaintext = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            string json = Encoding.UTF8.GetString(plaintext);
            return JsonSerializer.Deserialize<CalendarAccount>(json, JsonSerializerOptions.Web);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static byte[] Encrypt(CalendarAccount account)
    {
        string json = JsonSerializer.Serialize(account, JsonSerializerOptions.Web);
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
