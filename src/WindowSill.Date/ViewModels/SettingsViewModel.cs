using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using WindowSill.API;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Settings;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// View model for the Date extension settings page.
/// Manages calendar account list, add/remove operations, and display settings.
/// </summary>
internal sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly CalendarAccountManager _calendarAccountManager;
    private readonly string _contentDirectory;
    private readonly IReadOnlyList<FormatOptionItem<DateFormat>> _allDateFormats;
    private readonly IReadOnlyList<FormatOptionItem<TimeFormat>> _allTimeFormats;

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

        AvailableDisplayModes = BuildDisplayModeItems();
        _allDateFormats = BuildDateFormatItems();
        _allTimeFormats = BuildTimeFormatItems();

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
    /// Gets the available display mode options.
    /// </summary>
    public IReadOnlyList<FormatOptionItem<SillDisplayMode>> AvailableDisplayModes { get; }

    /// <summary>
    /// Gets the available date format options. The "(None)" item is excluded
    /// when the time format is already set to None.
    /// </summary>
    public IReadOnlyList<FormatOptionItem<DateFormat>> AvailableDateFormats
        => _settingsProvider.GetSetting(Settings.Settings.TimeFormat) == TimeFormat.None
            ? _allDateFormats.Where(i => i.Value != DateFormat.None).ToList()
            : _allDateFormats;

    /// <summary>
    /// Gets the available time format options. The "(None)" item is excluded
    /// when the date format is already set to None.
    /// </summary>
    public IReadOnlyList<FormatOptionItem<TimeFormat>> AvailableTimeFormats
        => _settingsProvider.GetSetting(Settings.Settings.DateFormat) == DateFormat.None
            ? _allTimeFormats.Where(i => i.Value != TimeFormat.None).ToList()
            : _allTimeFormats;

    /// <summary>
    /// Gets or sets the selected display mode item.
    /// </summary>
    public FormatOptionItem<SillDisplayMode>? SelectedDisplayMode
    {
        get => AvailableDisplayModes.FirstOrDefault(i => i.Value == _settingsProvider.GetSetting(Settings.Settings.DisplayMode));
        set
        {
            if (value is not null
                && value.Value != _settingsProvider.GetSetting(Settings.Settings.DisplayMode))
            {
                _settingsProvider.SetSetting(Settings.Settings.DisplayMode, value.Value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDateTimeMode));
                OnPropertyChanged(nameof(IsShowSecondsVisible));
                OnPropertyChanged(nameof(IsIconModeInfoVisible));
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected date format item.
    /// </summary>
    public FormatOptionItem<DateFormat>? SelectedDateFormat
    {
        get => _allDateFormats.FirstOrDefault(i => i.Value == _settingsProvider.GetSetting(Settings.Settings.DateFormat));
        set
        {
            if (value is null
                || value.Value == _settingsProvider.GetSetting(Settings.Settings.DateFormat))
            {
                return;
            }

            _settingsProvider.SetSetting(Settings.Settings.DateFormat, value.Value);
            OnPropertyChanged();
            // Refresh the time format combo to show/hide its None option.
            OnPropertyChanged(nameof(AvailableTimeFormats));
            OnPropertyChanged(nameof(SelectedTimeFormat));
        }
    }

    /// <summary>
    /// Gets or sets the selected time format item.
    /// </summary>
    public FormatOptionItem<TimeFormat>? SelectedTimeFormat
    {
        get => _allTimeFormats.FirstOrDefault(i => i.Value == _settingsProvider.GetSetting(Settings.Settings.TimeFormat));
        set
        {
            if (value is null
                || value.Value == _settingsProvider.GetSetting(Settings.Settings.TimeFormat))
            {
                return;
            }

            _settingsProvider.SetSetting(Settings.Settings.TimeFormat, value.Value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsShowSecondsVisible));
            // Refresh the date format combo to show/hide its None option.
            OnPropertyChanged(nameof(AvailableDateFormats));
            OnPropertyChanged(nameof(SelectedDateFormat));
        }
    }

    /// <summary>
    /// Gets or sets whether to show seconds in the time display.
    /// </summary>
    public bool ShowSeconds
    {
        get => _settingsProvider.GetSetting(Settings.Settings.ShowSeconds);
        set => _settingsProvider.SetSetting(Settings.Settings.ShowSeconds, value);
    }

    /// <summary>
    /// Gets whether the date/time settings section should be visible.
    /// </summary>
    public bool IsDateTimeMode
        => _settingsProvider.GetSetting(Settings.Settings.DisplayMode) == SillDisplayMode.DateTime;

    /// <summary>
    /// Gets whether the "Show seconds" toggle should be visible.
    /// Only visible in DateTime mode when a time format is selected.
    /// </summary>
    public bool IsShowSecondsVisible
        => IsDateTimeMode
        && _settingsProvider.GetSetting(Settings.Settings.TimeFormat) != TimeFormat.None;

    /// <summary>
    /// Gets whether the icon-mode info message should be visible.
    /// </summary>
    public bool IsIconModeInfoVisible
        => _settingsProvider.GetSetting(Settings.Settings.DisplayMode) == SillDisplayMode.Icon;

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

    private static IReadOnlyList<FormatOptionItem<SillDisplayMode>> BuildDisplayModeItems()
    {
        return
        [
            new FormatOptionItem<SillDisplayMode>(
                SillDisplayMode.Icon,
                "/WindowSill.Date/Display/DisplayModeIcon".GetLocalizedString()),
            new FormatOptionItem<SillDisplayMode>(
                SillDisplayMode.DateTime,
                "/WindowSill.Date/Display/DisplayModeDateTime".GetLocalizedString()),
        ];
    }

    private static IReadOnlyList<FormatOptionItem<DateFormat>> BuildDateFormatItems()
    {
        DateTime now = DateTime.Now;
        var items = new List<FormatOptionItem<DateFormat>>
        {
            new(DateFormat.None, "/WindowSill.Date/Display/FormatNone".GetLocalizedString()),
        };

        DateFormat[] formats =
        [
            DateFormat.AbbreviatedDayMonth,
            DateFormat.ShortMonthDay,
            DateFormat.DayShortMonth,
            DateFormat.FullDayMonth,
            DateFormat.MonthSlashDayCompact,
            DateFormat.MonthSlashDay,
            DateFormat.DaySlashMonthCompact,
            DateFormat.DaySlashMonth,
            DateFormat.MonthDayYear,
            DateFormat.DayMonthYear,
            DateFormat.Iso8601,
        ];

        foreach (DateFormat format in formats)
        {
            string preview = format.FormatDate(now);
            string? suffix = format.GetLabelSuffix();
            string displayName = suffix is null ? preview : $"{preview}  {suffix}";
            items.Add(new FormatOptionItem<DateFormat>(format, displayName));
        }

        return items;
    }

    private static IReadOnlyList<FormatOptionItem<TimeFormat>> BuildTimeFormatItems()
    {
        DateTime now = DateTime.Now;
        return
        [
            new FormatOptionItem<TimeFormat>(
                TimeFormat.None,
                "/WindowSill.Date/Display/FormatNone".GetLocalizedString()),
            new FormatOptionItem<TimeFormat>(
                TimeFormat.TwelveHour,
                TimeFormat.TwelveHour.FormatTime(now, showSeconds: false)),
            new FormatOptionItem<TimeFormat>(
                TimeFormat.TwentyFourHour,
                TimeFormat.TwentyFourHour.FormatTime(now, showSeconds: false)),
        ];
    }
}
