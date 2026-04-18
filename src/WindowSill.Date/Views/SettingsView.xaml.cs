using System.Runtime.InteropServices;
using WindowSill.API;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;
using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

internal sealed partial class SettingsView : UserControl
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();
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
        ShowConnectDialog(CalendarProviderType.Outlook);
    }

    private void AddGoogleMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ShowConnectDialog(CalendarProviderType.Google);
    }

    private void AddICloudMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ShowConnectDialog(CalendarProviderType.ICloud);
    }

    private void AddCalDavMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ShowConnectDialog(CalendarProviderType.CalDav);
    }

    private void ShowConnectDialog(CalendarProviderType providerType)
    {
        ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            ConnectExperience experience = ViewModel.CreateConnectExperience(providerType);

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

            if (isOAuthFlow)
            {
                // OAuth flow: start auth immediately, close dialog on completion.
                IntPtr hwnd = GetActiveWindow();
                Task<CalendarAccount> connectTask = experience.ConnectAsync(hwnd, cts.Token);

                // Show dialog concurrently — Cancel will trigger the CTS.
                Task<ContentDialogResult> dialogTask = dialog.ShowAsync().AsTask();

                // Wait for whichever finishes first.
                Task completed = await Task.WhenAny(connectTask, dialogTask);

                if (completed == connectTask)
                {
                    dialog.Hide();

                    // Observe the dialog task to prevent unobserved exception.
                    _ = dialogTask.ContinueWith(_ => { }, TaskScheduler.Default);

                    if (connectTask.IsCompletedSuccessfully)
                    {
                        await ViewModel.RegisterAccountAsync(connectTask.Result, CancellationToken.None);
                    }
                    else
                    {
                        // Observe and rethrow the connect exception so it can be handled.
                        await connectTask;
                    }
                }
                else
                {
                    // User clicked Cancel — cancel the auth flow and observe its result.
                    await cts.CancelAsync();
                    try
                    {
                        await connectTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when user cancels.
                    }
                    catch (Exception)
                    {
                        // Auth may have already failed before cancellation was observed.
                    }
                }
            }
            else
            {
                // Credential flow: wait for primary button click, then connect.
                ContentDialogResult result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        CalendarAccount account = await experience.ConnectAsync(GetActiveWindow(), cts.Token);
                        await ViewModel.RegisterAccountAsync(account, CancellationToken.None);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // TODO: Surface error to user (e.g., invalid credentials).
                    }
                }
            }
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
