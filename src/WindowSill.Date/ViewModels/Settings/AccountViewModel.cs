using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// View model wrapping a <see cref="CalendarAccount"/> for display in the settings UI.
/// </summary>
internal sealed partial class AccountViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountViewModel"/> class.
    /// </summary>
    /// <param name="account">The calendar account to wrap.</param>
    /// <param name="providerIconSource">The icon source for the provider.</param>
    /// <param name="removeCommand">The command to remove this account.</param>
    public AccountViewModel(CalendarAccount account, ImageSource providerIconSource, IAsyncRelayCommand removeCommand)
    {
        Account = account;
        ProviderIconSource = providerIconSource;
        RemoveCommand = removeCommand;
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

    /// <summary>
    /// Gets the command to remove this account.
    /// </summary>
    public IAsyncRelayCommand RemoveCommand { get; }

    /// <summary>
    /// Gets the collection of calendars within this account.
    /// </summary>
    public ObservableCollection<CalendarViewModel> Calendars { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether calendars are currently loading.
    /// </summary>
    [ObservableProperty]
    public partial bool IsLoadingCalendars { get; set; } = true;
}
