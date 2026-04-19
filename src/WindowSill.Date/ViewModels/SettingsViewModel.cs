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
    /// Raised when the user requests to remove an account, before the removal occurs.
    /// The View should show a confirmation dialog and return <see langword="true"/>
    /// to proceed or <see langword="false"/> to cancel.
    /// </summary>
    public event Func<AccountViewModel, Task<bool>>? ConfirmRemoveAccountRequested;

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
        AccountViewModel accountVm = CreateAccountViewModel(account);
        Accounts.Add(accountVm);
        HasNoAccounts = Accounts.Count == 0;

        LoadCalendarsForAccountAsync(accountVm).ForgetSafely();
    }

    private async Task RemoveAccountAsync(AccountViewModel accountViewModel)
    {
        if (ConfirmRemoveAccountRequested is not null)
        {
            bool confirmed = await ConfirmRemoveAccountRequested.Invoke(accountViewModel);
            if (!confirmed)
            {
                return;
            }
        }

        await _calendarAccountManager.RemoveAccountAsync(accountViewModel.Id, CancellationToken.None);
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
                AccountViewModel accountVm = CreateAccountViewModel(account);
                Accounts.Add(accountVm);
                LoadCalendarsForAccountAsync(accountVm).ForgetSafely();
            }

            HasNoAccounts = Accounts.Count == 0;
        });
    }

    private async Task LoadCalendarsForAccountAsync(AccountViewModel accountVm)
    {
        try
        {
            await Task.Delay(3000);

            CalendarAccountClientDecorator client = _calendarAccountManager.GetClientForAccount(accountVm.Id);
            IReadOnlyList<CalendarInfo> calendars = await client.GetCalendarsAsync();
            HashSet<string> hidden = accountVm.Account.HiddenCalendarIds;

            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                accountVm.Calendars.Clear();
                foreach (CalendarInfo cal in calendars)
                {
                    var calVm = new CalendarViewModel(cal, isVisible: !hidden.Contains(cal.Id));
                    calVm.VisibilityChanged += (_, _) => PersistCalendarVisibilityAsync(accountVm).ForgetSafely();
                    accountVm.Calendars.Add(calVm);
                }
            });
        }
        catch
        {
        }
        finally
        {
            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                accountVm.IsLoadingCalendars = false;
            });
        }
    }

    private async Task PersistCalendarVisibilityAsync(AccountViewModel accountVm)
    {
        HashSet<string> hidden = accountVm.Calendars
            .Where(c => !c.IsVisible)
            .Select(c => c.Id)
            .ToHashSet();

        await _calendarAccountManager.UpdateHiddenCalendarsAsync(
            accountVm.Id, hidden, CancellationToken.None);
    }

    private AccountViewModel CreateAccountViewModel(CalendarAccount account)
    {
        ProviderMenuItemViewModel? provider = Providers.FirstOrDefault(p => p.ProviderType == account.ProviderType);
        ImageSource iconSource = provider?.IconSource
            ?? CreateProviderIconSource(_contentDirectory, "package.svg");

        AccountViewModel accountVm = null!;
        accountVm = new AccountViewModel(
            account,
            iconSource,
            new AsyncRelayCommand(() => RemoveAccountAsync(accountVm)));
        return accountVm;
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
