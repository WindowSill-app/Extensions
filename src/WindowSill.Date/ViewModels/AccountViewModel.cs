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
    /// Gets the icon source for the calendar provider.
    /// </summary>
    public ImageSource ProviderIconSource { get; }
}
