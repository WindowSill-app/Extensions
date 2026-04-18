namespace WindowSill.Date.Core;

/// <summary>
/// Securely stores and retrieves authentication credentials (tokens, passwords)
/// for calendar accounts. Implementations should use platform-appropriate secure
/// storage (e.g., Windows Credential Manager, DPAPI).
/// </summary>
public interface ICalendarCredentialStore
{
    /// <summary>
    /// Stores a credential for the specified account.
    /// </summary>
    /// <param name="accountId">The account identifier to associate the credential with.</param>
    /// <param name="key">The credential key (e.g., "access_token", "refresh_token").</param>
    /// <param name="value">The credential value to store.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task StoreAsync(string accountId, string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a stored credential for the specified account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="key">The credential key to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The credential value, or <see langword="null"/> if not found.</returns>
    Task<string?> RetrieveAsync(string accountId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all stored credentials for the specified account.
    /// </summary>
    /// <param name="accountId">The account identifier whose credentials should be removed.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task RemoveAsync(string accountId, CancellationToken cancellationToken = default);
}
