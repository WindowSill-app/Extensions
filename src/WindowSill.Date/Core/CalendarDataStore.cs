using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WindowSill.Date.Core.Models;
using Path = System.IO.Path;

namespace WindowSill.Date.Core;

/// <summary>
/// Persists calendar account data and provider-specific caches (tokens, credentials)
/// to encrypted files in the plugin data folder using DPAPI (current-user scope).
/// This avoids the 8KB per-setting limit of <c>ISettingsProvider</c>.
/// Also implements <see cref="ICalendarCredentialStore"/> so CalDAV, Google, and
/// iCloud providers can store per-account credentials in the same encrypted store.
/// </summary>
internal sealed class CalendarDataStore : ICalendarCredentialStore
{
    private const string AccountsFileName = "accounts.dat";
    private const string CredentialsFileName = "credentials.dat";

    private readonly string _dataFolder;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarDataStore"/> class.
    /// </summary>
    /// <param name="pluginDataFolder">The plugin's data folder path from <c>IPluginInfo.GetPluginDataFolder()</c>.</param>
    internal CalendarDataStore(string pluginDataFolder)
    {
        _dataFolder = pluginDataFolder;
        Directory.CreateDirectory(_dataFolder);
    }

    /// <summary>
    /// Loads the saved calendar accounts. Returns an empty array if no data exists.
    /// </summary>
    internal async Task<CalendarAccount[]> LoadAccountsAsync(CancellationToken cancellationToken = default)
    {
        AccountRecord[]? records = await ReadEncryptedAsync<AccountRecord[]>(AccountsFileName, cancellationToken);
        if (records is null or { Length: 0 })
        {
            return [];
        }

        return records
            .Select(r => new CalendarAccount
            {
                Id = r.Id,
                DisplayName = r.DisplayName,
                Email = r.Email,
                ProviderType = r.ProviderType,
            })
            .ToArray();
    }

    /// <summary>
    /// Saves the current list of calendar accounts.
    /// </summary>
    internal async Task SaveAccountsAsync(IReadOnlyList<CalendarAccount> accounts, CancellationToken cancellationToken = default)
    {
        AccountRecord[] records = accounts
            .Select(a => new AccountRecord
            {
                Id = a.Id,
                DisplayName = a.DisplayName,
                Email = a.Email,
                ProviderType = a.ProviderType,
            })
            .ToArray();

        await WriteEncryptedAsync(AccountsFileName, records, cancellationToken);
    }

    /// <summary>
    /// Loads a provider-specific token/credential cache.
    /// Each provider gets its own encrypted file keyed by <paramref name="providerType"/>.
    /// </summary>
    /// <param name="providerType">The provider whose cache to load.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The raw cache bytes, or an empty array if no data exists.</returns>
    internal async Task<byte[]> LoadProviderCacheAsync(
        CalendarProviderType providerType,
        CancellationToken cancellationToken = default)
    {
        return await ReadEncryptedRawAsync(GetProviderCacheFileName(providerType), cancellationToken) ?? [];
    }

    /// <summary>
    /// Saves a provider-specific token/credential cache.
    /// Each provider gets its own encrypted file keyed by <paramref name="providerType"/>.
    /// </summary>
    /// <param name="providerType">The provider whose cache to save.</param>
    /// <param name="cacheData">The raw cache bytes to persist.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    internal async Task SaveProviderCacheAsync(
        CalendarProviderType providerType,
        byte[] cacheData,
        CancellationToken cancellationToken = default)
    {
        await WriteEncryptedRawAsync(GetProviderCacheFileName(providerType), cacheData, cancellationToken);
    }

    /// <summary>
    /// Deletes the provider-specific token/credential cache.
    /// </summary>
    /// <param name="providerType">The provider whose cache to delete.</param>
    internal void ClearProviderCache(CalendarProviderType providerType)
    {
        TryDeleteFile(GetProviderCacheFileName(providerType));
    }

    /// <summary>
    /// Deletes all persisted data (accounts and all provider caches).
    /// </summary>
    internal void Clear()
    {
        TryDeleteFile(AccountsFileName);
        TryDeleteFile(CredentialsFileName);
        foreach (CalendarProviderType providerType in Enum.GetValues<CalendarProviderType>())
        {
            TryDeleteFile(GetProviderCacheFileName(providerType));
        }
    }

    // --- ICalendarCredentialStore implementation ---

    /// <inheritdoc />
    public async Task StoreAsync(string accountId, string key, string value, CancellationToken cancellationToken)
    {
        Dictionary<string, string> credentials = await LoadCredentialsDictionaryAsync(cancellationToken);
        credentials[$"{accountId}:{key}"] = value;
        await WriteEncryptedAsync(CredentialsFileName, credentials, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> RetrieveAsync(string accountId, string key, CancellationToken cancellationToken)
    {
        Dictionary<string, string> credentials = await LoadCredentialsDictionaryAsync(cancellationToken);
        return credentials.GetValueOrDefault($"{accountId}:{key}");
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string accountId, CancellationToken cancellationToken)
    {
        Dictionary<string, string> credentials = await LoadCredentialsDictionaryAsync(cancellationToken);
        string prefix = $"{accountId}:";
        List<string> keysToRemove = credentials.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();

        if (keysToRemove.Count == 0)
        {
            return;
        }

        foreach (string credKey in keysToRemove)
        {
            credentials.Remove(credKey);
        }

        await WriteEncryptedAsync(CredentialsFileName, credentials, cancellationToken);
    }

    private async Task<Dictionary<string, string>> LoadCredentialsDictionaryAsync(CancellationToken cancellationToken)
    {
        return await ReadEncryptedAsync<Dictionary<string, string>>(CredentialsFileName, cancellationToken) ?? new();
    }

    private static string GetProviderCacheFileName(CalendarProviderType providerType)
    {
        return $"{providerType.ToString().ToLowerInvariant()}_tokens.dat";
    }

    private async Task<T?> ReadEncryptedAsync<T>(string fileName, CancellationToken cancellationToken) where T : class
    {
        byte[]? plaintext = await ReadEncryptedRawAsync(fileName, cancellationToken);
        if (plaintext is null or { Length: 0 })
        {
            return null;
        }

        string json = Encoding.UTF8.GetString(plaintext);
        return JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web);
    }

    private async Task WriteEncryptedAsync<T>(string fileName, T data, CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(data, JsonSerializerOptions.Web);
        byte[] plaintext = Encoding.UTF8.GetBytes(json);
        await WriteEncryptedRawAsync(fileName, plaintext, cancellationToken);
    }

    private async Task<byte[]?> ReadEncryptedRawAsync(string fileName, CancellationToken cancellationToken)
    {
        string filePath = Path.Combine(_dataFolder, fileName);
        if (!File.Exists(filePath))
        {
            return null;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            byte[] encrypted = await File.ReadAllBytesAsync(filePath, cancellationToken);
            return ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        }
        catch (Exception)
        {
            // Corrupted or tampered — treat as empty.
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task WriteEncryptedRawAsync(string fileName, byte[] data, CancellationToken cancellationToken)
    {
        string filePath = Path.Combine(_dataFolder, fileName);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            await File.WriteAllBytesAsync(filePath, encrypted, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private void TryDeleteFile(string fileName)
    {
        try
        {
            string filePath = Path.Combine(_dataFolder, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Best effort.
        }
    }

    /// <summary>
    /// Serializable record for calendar account persistence.
    /// </summary>
    private sealed record AccountRecord
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public CalendarProviderType ProviderType { get; set; }
    }
}
