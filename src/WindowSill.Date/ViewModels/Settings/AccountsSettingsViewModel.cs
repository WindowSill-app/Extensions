using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using WindowSill.API;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Services;
using WindowSill.Date.Settings;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// View model for the Date extension settings page.
/// Manages calendar account list, add/remove operations, and display settings.
/// </summary>
internal sealed partial class AccountsSettingsViewModel : ObservableObject
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly CalendarAccountManager _calendarAccountManager;
    private readonly MeetingStateService? _meetingStateService;
    private readonly string _contentDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountsSettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsProvider">The settings provider for persisting preferences.</param>
    /// <param name="calendarAccountManager">The manager for calendar account operations.</param>
    /// <param name="contentDirectory">The plugin content directory for resolving asset paths.</param>
    /// <param name="meetingStateService">The meeting state service for on-demand sync.</param>
    public AccountsSettingsViewModel(
        ISettingsProvider settingsProvider,
        CalendarAccountManager calendarAccountManager,
        string contentDirectory,
        MeetingStateService? meetingStateService = null)
    {
        _settingsProvider = settingsProvider;
        _calendarAccountManager = calendarAccountManager;
        _meetingStateService = meetingStateService;
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
    /// Gets the available poll interval options.
    /// </summary>
    public IReadOnlyList<FormatOptionItem<int>> PollIntervalOptions { get; } =
    [
        new(60, "/WindowSill.Date/Meetings/PollInterval1Min".GetLocalizedString()),
        new(180, "/WindowSill.Date/Meetings/PollInterval3Min".GetLocalizedString()),
        new(300, "/WindowSill.Date/Meetings/PollInterval5Min".GetLocalizedString()),
        new(900, "/WindowSill.Date/Meetings/PollInterval15Min".GetLocalizedString()),
        new(1800, "/WindowSill.Date/Meetings/PollInterval30Min".GetLocalizedString()),
        new(3600, "/WindowSill.Date/Meetings/PollInterval1Hour".GetLocalizedString()),
        new(7200, "/WindowSill.Date/Meetings/PollInterval2Hours".GetLocalizedString()),
    ];

    /// <summary>
    /// Gets or sets the selected poll interval item.
    /// </summary>
    public FormatOptionItem<int>? SelectedPollInterval
    {
        get => PollIntervalOptions.FirstOrDefault(i => i.Value == _settingsProvider.GetSetting(Settings.Settings.MeetingPollIntervalSeconds));
        set
        {
            if (value is not null
                && value.Value != _settingsProvider.GetSetting(Settings.Settings.MeetingPollIntervalSeconds))
            {
                _settingsProvider.SetSetting(Settings.Settings.MeetingPollIntervalSeconds, value.Value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Triggers an immediate refresh of upcoming meetings.
    /// </summary>
    [RelayCommand]
    private void SyncNow()
    {
        _meetingStateService?.RequestRefresh();
    }

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
                    accountVm.Account.CalendarColorOverrides.TryGetValue(cal.Id, out string? colorOverride);
                    var calVm = new CalendarViewModel(cal, isVisible: !hidden.Contains(cal.Id), colorOverride);
                    calVm.VisibilityChanged += (_, _) => PersistCalendarVisibilityAsync(accountVm).ForgetSafely();
                    calVm.ColorChanged += (_, _) => PersistCalendarColorAsync(accountVm).ForgetSafely();
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
        var hidden = accountVm.Calendars
            .Where(c => !c.IsVisible)
            .Select(c => c.Id)
            .ToHashSet();

        await _calendarAccountManager.UpdateHiddenCalendarsAsync(
            accountVm.Id, hidden, CancellationToken.None);
    }

    private async Task PersistCalendarColorAsync(AccountViewModel accountVm)
    {
        var overrides = accountVm.Calendars
            .Where(c => c.Color != c.CalendarInfo.Color)
            .ToDictionary(c => c.Id, c => c.Color!);

        await _calendarAccountManager.UpdateCalendarColorAsync(
            accountVm.Id, overrides, CancellationToken.None);
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
