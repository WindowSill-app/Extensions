using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Win32;
using WindowSill.API;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;
using WindowSill.Date.ViewModels;

namespace WindowSill.Date.FirstTimeSetup;

/// <summary>
/// View for the accounts step of the first-time setup experience.
/// Lets the user add and remove calendar accounts without calendar customization.
/// </summary>
internal sealed partial class AccountsSetupContributorView : UserControl
{
    private readonly CalendarAccountManager _calendarAccountManager;
    private readonly string _contentDirectory;
    private readonly DateFirstTimeSetupState _setupState;
    private readonly ObservableCollection<AccountViewModel> _accounts = [];
    private readonly IReadOnlyList<ProviderMenuItemViewModel> _providers;

    internal AccountsSetupContributorView(
        CalendarAccountManager calendarAccountManager,
        string contentDirectory,
        DateFirstTimeSetupState setupState)
    {
        _calendarAccountManager = calendarAccountManager;
        _contentDirectory = contentDirectory;
        _setupState = setupState;

        _providers = calendarAccountManager.Providers
            .Select(p => new ProviderMenuItemViewModel(p, CreateProviderIconSource(contentDirectory, p.IconAssetFileName)))
            .ToList();

        InitializeComponent();

        AccountsRepeater.ItemsSource = _accounts;
        PopulateAddAccountMenu();
        LoadExistingAccountsAsync().ForgetSafely();
    }

    private async Task LoadExistingAccountsAsync()
    {
        IReadOnlyList<CalendarAccount> accounts = await _calendarAccountManager.GetAccountsAsync();

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            foreach (CalendarAccount account in accounts)
            {
                AccountViewModel accountVm = CreateAccountViewModel(account);
                _accounts.Add(accountVm);
            }

            _setupState.HasAddedAccount = _accounts.Count > 0;
            UpdateEmptyState();
        });
    }

    private void PopulateAddAccountMenu()
    {
        foreach (ProviderMenuItemViewModel provider in _providers)
        {
            var item = new MenuFlyoutItem
            {
                Text = provider.DisplayName,
                Icon = new ImageIcon { Source = provider.IconSource },
            };

            CalendarProviderType providerType = provider.ProviderType;
            item.Click += (_, _) => ShowConnectDialog(providerType);

            AddAccountMenuFlyout.Items.Add(item);
        }
    }

    private void ShowConnectDialog(CalendarProviderType providerType)
    {
        ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            ConnectExperience experience = _calendarAccountManager.CreateConnectExperience(providerType);

            using var cts = new CancellationTokenSource();

            var dialog = new ContentDialog
            {
                Title = "/WindowSill.Date/Settings/ConnectDialogTitle".GetLocalizedString(),
                Content = experience.Content,
                CloseButtonText = "/WindowSill.Date/Settings/RemoveAccountDialogCancel".GetLocalizedString(),
                XamlRoot = XamlRoot,
            };

            bool isOAuthFlow = experience.PrimaryButtonText is null;

            if (!isOAuthFlow)
            {
                dialog.PrimaryButtonText = experience.PrimaryButtonText;
                dialog.PrimaryButtonStyle = Application.Current.Resources["AccentButtonStyle"] as Style;
                dialog.IsPrimaryButtonEnabled = experience.CanSubmit;

                experience.CanSubmitChanged += (_, _) =>
                {
                    dialog.IsPrimaryButtonEnabled = experience.CanSubmit;
                };
            }

            CalendarAccount? connectedAccount = null;

            if (isOAuthFlow)
            {
                IntPtr hwnd = PInvoke.GetActiveWindow();
                Task<CalendarAccount> connectTask = experience.ConnectAsync(hwnd, cts.Token);
                Task<ContentDialogResult> dialogTask = dialog.ShowAsync().AsTask();

                Task completed = await Task.WhenAny(connectTask, dialogTask);

                if (completed == connectTask)
                {
                    dialog.Hide();
                    _ = dialogTask.ContinueWith(_ => { }, TaskScheduler.Default);

                    if (connectTask.IsCompletedSuccessfully)
                    {
                        connectedAccount = connectTask.Result;
                    }
                    else
                    {
                        await connectTask;
                    }
                }
                else
                {
                    await cts.CancelAsync();
                    try { await connectTask; }
                    catch (OperationCanceledException) { }
                    catch { }
                }
            }
            else
            {
                ContentDialogResult result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        connectedAccount = await experience.ConnectAsync(PInvoke.GetActiveWindow(), cts.Token);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException) { }
                }
            }

            if (connectedAccount is not null)
            {
                await _calendarAccountManager.RegisterAccountAsync(connectedAccount, CancellationToken.None);
                AccountViewModel accountVm = CreateAccountViewModel(connectedAccount);
                _accounts.Add(accountVm);
                _setupState.HasAddedAccount = true;
                UpdateEmptyState();
            }
        }).ForgetSafely();
    }

    private async Task RemoveAccountAsync(AccountViewModel accountVm)
    {
        await _calendarAccountManager.RemoveAccountAsync(accountVm.Id, CancellationToken.None);
        _accounts.Remove(accountVm);
        _setupState.HasAddedAccount = _accounts.Count > 0;
        UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        NoAccountsText.Visibility = _accounts.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private AccountViewModel CreateAccountViewModel(CalendarAccount account)
    {
        ProviderMenuItemViewModel? provider = _providers.FirstOrDefault(p => p.ProviderType == account.ProviderType);
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
