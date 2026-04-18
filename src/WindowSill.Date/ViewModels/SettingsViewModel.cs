using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using WindowSill.API;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// View model for the Date extension settings page.
/// Manages calendar account list and add/remove operations.
/// </summary>
internal sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly CalendarAccountManager _calendarAccountManager;
    private readonly string _contentDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsProvider">The settings provider for persisting preferences.</param>
    /// <param name="calendarAccountManager">The manager for calendar account operations.</param>
    /// <param name="contentDirectory">The plugin content directory for resolving asset paths.</param>
    public SettingsViewModel(
        ISettingsProvider settingsProvider,
        CalendarAccountManager calendarAccountManager,
        string contentDirectory)
    {
        _settingsProvider = settingsProvider;
        _calendarAccountManager = calendarAccountManager;
        _contentDirectory = contentDirectory;

        OutlookIconSource = CreateProviderIconSource(contentDirectory, CalendarProviderType.Outlook);
        GoogleIconSource = CreateProviderIconSource(contentDirectory, CalendarProviderType.Google);
        ICloudIconSource = CreateProviderIconSource(contentDirectory, CalendarProviderType.ICloud);
        CalDavIconSource = CreateProviderIconSource(contentDirectory, CalendarProviderType.CalDav);

        LoadAccountsAsync().ForgetSafely();
    }

    /// <summary>
    /// Gets the collection of connected calendar accounts.
    /// </summary>
    public ObservableCollection<AccountViewModel> Accounts { get; } = [];

    /// <summary>
    /// Gets the icon source for the Outlook provider.
    /// </summary>
    public ImageSource OutlookIconSource { get; }

    /// <summary>
    /// Gets the icon source for the Google provider.
    /// </summary>
    public ImageSource GoogleIconSource { get; }

    /// <summary>
    /// Gets the icon source for the iCloud provider.
    /// </summary>
    public ImageSource ICloudIconSource { get; }

    /// <summary>
    /// Gets the icon source for the CalDAV provider.
    /// </summary>
    public ImageSource CalDavIconSource { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the account list is empty.
    /// </summary>
    [ObservableProperty]
    public partial bool HasNoAccounts { get; set; } = true;

    /// <summary>
    /// Creates a connect experience for the specified provider type.
    /// </summary>
    /// <param name="providerType">The provider to connect.</param>
    /// <returns>A connect experience that drives the authentication UI.</returns>
    public ConnectExperience CreateConnectExperience(CalendarProviderType providerType)
    {
        return _calendarAccountManager.CreateConnectExperience(providerType);
    }

    /// <summary>
    /// Registers a newly connected account after a successful connect experience.
    /// </summary>
    /// <param name="account">The account returned by the connect experience.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RegisterAccountAsync(CalendarAccount account, CancellationToken cancellationToken)
    {
        await _calendarAccountManager.RegisterAccountAsync(account, cancellationToken);
        Accounts.Add(CreateAccountViewModel(account));
        HasNoAccounts = Accounts.Count == 0;
    }

    /// <summary>
    /// Removes the specified account after the caller has confirmed with the user.
    /// </summary>
    /// <param name="accountViewModel">The account to remove.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    [RelayCommand]
    private async Task RemoveAccountAsync(AccountViewModel accountViewModel, CancellationToken cancellationToken)
    {
        await _calendarAccountManager.RemoveAccountAsync(accountViewModel.Id, cancellationToken);
        Accounts.Remove(accountViewModel);
        HasNoAccounts = Accounts.Count == 0;
    }

    private async Task LoadAccountsAsync()
    {
        IReadOnlyList<CalendarAccount> accounts = await _calendarAccountManager.GetAccountsAsync();

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            foreach (CalendarAccount account in accounts)
            {
                Accounts.Add(CreateAccountViewModel(account));
            }

            HasNoAccounts = Accounts.Count == 0;
        });
    }

    private AccountViewModel CreateAccountViewModel(CalendarAccount account)
    {
        return new AccountViewModel(account, CreateProviderIconSource(_contentDirectory, account.ProviderType));
    }

    private static ImageSource CreateProviderIconSource(string contentDirectory, CalendarProviderType providerType)
    {
        string fileName = providerType switch
        {
            CalendarProviderType.Outlook => "outlook.png",
            CalendarProviderType.Google => "google-calendar.png",
            CalendarProviderType.ICloud => "icloud.png",
            CalendarProviderType.CalDav => "package.svg",
            _ => "package.svg",
        };

        string path = System.IO.Path.Combine(contentDirectory, "Assets", fileName);
        Uri uri = new(path);

        if (fileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            return new SvgImageSource(uri);
        }

        return new BitmapImage(uri);
    }
}
