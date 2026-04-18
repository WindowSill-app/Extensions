using WindowSill.Date.Core.Models;

namespace WindowSill.Date.Settings;

/// <summary>
/// Serializable record for persisting calendar account information.
/// Kept separate from <see cref="CalendarAccount"/> so the domain model
/// doesn't need serialization concerns.
/// </summary>
internal sealed record AccountRecord
{
    /// <summary>
    /// Gets or sets the unique identifier for this account.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider type.
    /// </summary>
    public CalendarProviderType ProviderType { get; set; }

    /// <summary>
    /// Converts this record to a <see cref="CalendarAccount"/> domain model.
    /// </summary>
    public CalendarAccount ToCalendarAccount()
    {
        return new CalendarAccount
        {
            Id = Id,
            DisplayName = DisplayName,
            Email = Email,
            ProviderType = ProviderType,
        };
    }

    /// <summary>
    /// Creates an <see cref="AccountRecord"/> from a <see cref="CalendarAccount"/> domain model.
    /// </summary>
    public static AccountRecord FromCalendarAccount(CalendarAccount account)
    {
        return new AccountRecord
        {
            Id = account.Id,
            DisplayName = account.DisplayName,
            Email = account.Email,
            ProviderType = account.ProviderType,
        };
    }
}
