using WindowSill.API;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;
using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

internal sealed partial class SettingsView : UserControl
{
    public SettingsView(
        ISettingsProvider settingsProvider,
        CalendarAccountManager calendarAccountManager,
        string contentDirectory)
    {
        ViewModel = new SettingsViewModel(settingsProvider, calendarAccountManager, contentDirectory);
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for the settings view.
    /// </summary>
    internal SettingsViewModel ViewModel { get; }

    private void AddOutlookMenuItem_Click(object sender, RoutedEventArgs e)
    {
        AddAccount(CalendarProviderType.Outlook);
    }

    private void AddGoogleMenuItem_Click(object sender, RoutedEventArgs e)
    {
        AddAccount(CalendarProviderType.Google);
    }

    private void AddICloudMenuItem_Click(object sender, RoutedEventArgs e)
    {
        AddAccount(CalendarProviderType.ICloud);
    }

    private void AddCalDavMenuItem_Click(object sender, RoutedEventArgs e)
    {
        AddAccount(CalendarProviderType.CalDav);
    }

    private void AddAccount(CalendarProviderType providerType)
    {
        ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            await ViewModel.AddAccountCommand.ExecuteAsync(providerType);
        }).ForgetSafely();
    }

    private void RemoveAccountButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: AccountViewModel account })
        {
            return;
        }

        ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = "/WindowSill.Date/Settings/RemoveAccountDialogTitle".GetLocalizedString(),
                Content = string.Format(
                    "/WindowSill.Date/Settings/RemoveAccountDialogContent".GetLocalizedString(),
                    account.Email),
                PrimaryButtonText = "/WindowSill.Date/Settings/RemoveAccountDialogConfirm".GetLocalizedString(),
                CloseButtonText = "/WindowSill.Date/Settings/RemoveAccountDialogCancel".GetLocalizedString(),
                PrimaryButtonStyle = Application.Current.Resources["AccentButtonStyle"] as Style,
                XamlRoot = XamlRoot,
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.RemoveAccountCommand.ExecuteAsync(account);
            }
        }).ForgetSafely();
    }
}
