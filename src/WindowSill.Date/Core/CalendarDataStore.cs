using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WindowSill.Date.Core.Models;
using Path = System.IO.Path;

namespace WindowSill.Date.Core;

/// <summary>
/// Persists calendar data to DPAPI-encrypted files in the plugin data folder.
/// Each connected account gets a single encrypted file (<c>{accountId}.dat</c>)
/// containing the account metadata, string credentials (key/value), and an
/// optional raw binary cache (e.g., MSAL token cache). The folder is scanned
/// on startup to discover all accounts — no separate index file needed.
///
/// This avoids the 8KB per-setting limit of <c>ISettingsProvider</c> and keeps
/// the storage model identical across all providers.
/// </summary>
internal sealed class CalendarDataStore : ICalendarCredentialStore
{
    private const string FileExtension = ".dat";

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

    // --- Account discovery and persistence ---

    /// <summary>
    /// Discovers all saved accounts by scanning the data folder for <c>.dat</c> files.
    /// Returns an empty array if no accounts exist.
    /// </summary>
    internal async Task<CalendarAccount[]> LoadAccountsAsync(CancellationToken cancellationToken = default)
    {
        var accounts = new List<CalendarAccount>();

        string[] files = Directory.GetFiles(_dataFolder, $"*{FileExtension}");
        foreach (string filePath in files)
        {
            AccountEnvelope? envelope = await ReadEnvelopeAsync(Path.GetFileName(filePath), cancellationToken);
            if (envelope?.Account is not null)
            {
                accounts.Add(new CalendarAccount
                {
                    Id = envelope.Account.Id,
                    DisplayName = envelope.Account.DisplayName,
                    Email = envelope.Account.Email,
                    ProviderType = envelope.Account.ProviderType,
                });
            }
        }

        return [.. accounts];
    }

    /// <summary>
    /// Saves (creates or updates) the data file for a single account.
    /// Preserves existing credentials and cache if the file already exists.
    /// </summary>
    internal async Task SaveAccountAsync(CalendarAccount account, CancellationToken cancellationToken = default)
    {
        string fileName = GetAccountFileName(account.Id);
        AccountEnvelope envelope = await ReadEnvelopeAsync(fileName, cancellationToken) ?? new();

        envelope.Account = new AccountRecord
        {
            Id = account.Id,
            DisplayName = account.DisplayName,
            Email = account.Email,
            ProviderType = account.ProviderType,
        };

        await WriteEnvelopeAsync(fileName, envelope, cancellationToken);
    }

    /// <summary>
    /// Deletes all data for a specific account.
    /// </summary>
    internal void DeleteAccount(string accountId)
    {
        TryDeleteFile(GetAccountFileName(accountId));
    }

    // --- Per-account raw binary cache (e.g., MSAL token cache) ---

    /// <summary>
    /// Loads the raw binary cache for a specific account.
    /// </summary>
    /// <param name="accountId">The account whose cache to load.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The raw cache bytes, or an empty array if none exists.</returns>
    internal async Task<byte[]> LoadAccountCacheAsync(string accountId, CancellationToken cancellationToken = default)
    {
        AccountEnvelope? envelope = await ReadEnvelopeAsync(GetAccountFileName(accountId), cancellationToken);
        if (envelope?.BinaryCache is null or { Length: 0 })
        {
            return [];
        }

        return Convert.FromBase64String(envelope.BinaryCache);
    }

    /// <summary>
    /// Saves a raw binary cache for a specific account.
    /// </summary>
    /// <param name="accountId">The account whose cache to save.</param>
    /// <param name="cacheData">The raw cache bytes to persist.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    internal async Task SaveAccountCacheAsync(string accountId, byte[] cacheData, CancellationToken cancellationToken = default)
    {
        string fileName = GetAccountFileName(accountId);
        AccountEnvelope envelope = await ReadEnvelopeAsync(fileName, cancellationToken) ?? new();
        envelope.BinaryCache = Convert.ToBase64String(cacheData);
        await WriteEnvelopeAsync(fileName, envelope, cancellationToken);
    }

    // --- ICalendarCredentialStore (per-account key/value strings) ---

    /// <inheritdoc />
    public async Task StoreAsync(string accountId, string key, string value, CancellationToken cancellationToken)
    {
        string fileName = GetAccountFileName(accountId);
        AccountEnvelope envelope = await ReadEnvelopeAsync(fileName, cancellationToken) ?? new();
        envelope.Credentials[key] = value;
        await WriteEnvelopeAsync(fileName, envelope, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> RetrieveAsync(string accountId, string key, CancellationToken cancellationToken)
    {
        AccountEnvelope? envelope = await ReadEnvelopeAsync(GetAccountFileName(accountId), cancellationToken);
        return envelope?.Credentials.GetValueOrDefault(key);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string accountId, CancellationToken cancellationToken)
    {
        DeleteAccount(accountId);
    }

    // --- File I/O helpers ---

    private async Task<AccountEnvelope?> ReadEnvelopeAsync(string fileName, CancellationToken cancellationToken)
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
            byte[] plaintext = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            string json = Encoding.UTF8.GetString(plaintext);
            return JsonSerializer.Deserialize<AccountEnvelope>(json, JsonSerializerOptions.Web);
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task WriteEnvelopeAsync(string fileName, AccountEnvelope envelope, CancellationToken cancellationToken)
    {
        string filePath = Path.Combine(_dataFolder, fileName);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string json = JsonSerializer.Serialize(envelope, JsonSerializerOptions.Web);
            byte[] plaintext = Encoding.UTF8.GetBytes(json);
            byte[] encrypted = ProtectedData.Protect(plaintext, null, DataProtectionScope.CurrentUser);
            await File.WriteAllBytesAsync(filePath, encrypted, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static string GetAccountFileName(string accountId)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string safe = new(accountId.Select(c => Array.IndexOf(invalidChars, c) >= 0 ? '_' : c).ToArray());
        return $"{safe}{FileExtension}";
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

    // --- Envelope model (what gets serialized into each .dat file) ---

    /// <summary>
    /// The single JSON structure persisted per account file. Contains the account
    /// metadata, string credentials, and optional binary cache.
    /// </summary>
    private sealed class AccountEnvelope
    {
        public AccountRecord? Account { get; set; }
        public Dictionary<string, string> Credentials { get; set; } = new();
        public string? BinaryCache { get; set; }
    }

    private sealed record AccountRecord
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public CalendarProviderType ProviderType { get; set; }
    }
}
