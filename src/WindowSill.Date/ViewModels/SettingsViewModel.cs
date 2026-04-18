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

        Providers = calendarAccountManager.Providers
            .Select(p => new ProviderMenuItemViewModel(p, CreateProviderIconSource(contentDirectory, p.IconAssetFileName)))
            .ToList();

        LoadAccountsAsync().ForgetSafely();
    }

    /// <summary>
    /// Gets the collection of connected calendar accounts.
    /// </summary>
    public ObservableCollection<AccountViewModel> Accounts { get; } = [];

    /// <summary>
    /// Gets the provider menu items for the "Add account" flyout.
    /// </summary>
    public IReadOnlyList<ProviderMenuItemViewModel> Providers { get; }

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
        ThreadHelper.ThrowIfNotOnUIThread();
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
        ThreadHelper.ThrowIfNotOnUIThread();
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
        ProviderMenuItemViewModel? provider = Providers.FirstOrDefault(p => p.ProviderType == account.ProviderType);
        ImageSource iconSource = provider?.IconSource
            ?? CreateProviderIconSource(_contentDirectory, "package.svg");
        return new AccountViewModel(account, iconSource);
    }

    private static ImageSource CreateProviderIconSource(string contentDirectory, string iconAssetFileName)
    {
        string path = System.IO.Path.Combine(contentDirectory, "Assets", iconAssetFileName);
        Uri uri = new(path);

        if (iconAssetFileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            return new SvgImageSource(uri);
        }

        return new BitmapImage(uri);
    }
}
