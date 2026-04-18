namespace WindowSill.Date.Core.Models;

/// <summary>
/// Represents a calendar (e.g., "Work", "Personal") within an account.
/// </summary>
public sealed class CalendarInfo
{
    /// <summary>
    /// Gets the provider-specific identifier for this calendar.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the account identifier that owns this calendar.
    /// </summary>
    public required string AccountId { get; init; }

    /// <summary>
    /// Gets the display name of this calendar.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the color associated with this calendar (hex string, e.g., "#FF5733").
    /// </summary>
    public string? Color { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is the account's default calendar.
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Gets a value indicating whether this calendar is read-only.
    /// </summary>
    public bool IsReadOnly { get; init; }
}
