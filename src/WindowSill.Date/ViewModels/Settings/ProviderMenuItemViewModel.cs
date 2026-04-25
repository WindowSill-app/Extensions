using WindowSill.Date.Core.Models;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// Represents a calendar provider as a menu item in the "Add account" flyout.
/// </summary>
/// <param name="DisplayName">The human-readable provider name.</param>
/// <param name="ProviderType">The provider type used to create a connect experience.</param>
/// <param name="IconSource">The icon image source for the provider.</param>
internal sealed record ProviderMenuItemViewModel(
    string DisplayName,
    CalendarProviderType ProviderType,
    ImageSource IconSource)
{
    /// <summary>
    /// Initializes a new instance from an <see cref="Core.ICalendarProvider"/> and a resolved icon source.
    /// </summary>
    /// <param name="provider">The calendar provider.</param>
    /// <param name="iconSource">The resolved icon image source.</param>
    public ProviderMenuItemViewModel(Core.ICalendarProvider provider, ImageSource iconSource)
        : this(provider.DisplayName, provider.ProviderType, iconSource)
    {
    }
}
