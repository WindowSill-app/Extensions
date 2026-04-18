using WindowSill.Date.Core.Models;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// View model wrapping a <see cref="CalendarAccount"/> for display in the settings UI.
/// </summary>
internal sealed class AccountViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountViewModel"/> class.
    /// </summary>
    /// <param name="account">The calendar account to wrap.</param>
    /// <param name="providerIconSource">The icon source for the provider.</param>
    public AccountViewModel(CalendarAccount account, ImageSource providerIconSource)
    {
        Account = account;
        ProviderIconSource = providerIconSource;
    }

    /// <summary>
    /// Gets the underlying calendar account.
    /// </summary>
    public CalendarAccount Account { get; }

    /// <summary>
    /// Gets the unique identifier of the account.
    /// </summary>
    public string Id => Account.Id;

    /// <summary>
    /// Gets the user-facing display name.
    /// </summary>
    public string DisplayName => Account.DisplayName;

    /// <summary>
    /// Gets the email address associated with this account.
    /// </summary>
    public string Email => Account.Email;

    /// <summary>
    /// Gets the provider type for display purposes.
    /// </summary>
    public CalendarProviderType ProviderType => Account.ProviderType;

    /// <summary>
    /// Gets a human-readable label for the provider type.
    /// </summary>
    public string ProviderLabel => Account.ProviderType switch
    {
        CalendarProviderType.Outlook => "Outlook",
        CalendarProviderType.Google => "Google",
        CalendarProviderType.ICloud => "iCloud",
        CalendarProviderType.CalDav => "CalDAV",
        _ => Account.ProviderType.ToString(),
    };

    /// <summary>
    /// Gets the icon source for the calendar provider.
    /// </summary>
    public ImageSource ProviderIconSource { get; }
}
