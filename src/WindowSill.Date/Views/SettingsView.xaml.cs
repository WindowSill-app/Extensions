using System.Runtime.InteropServices;
using Microsoft.UI;
using WindowSill.API;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Services;
using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

internal sealed partial class SettingsView : UserControl
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();
    public SettingsView(
        ISettingsProvider settingsProvider,
        CalendarAccountManager calendarAccountManager,
        string contentDirectory,
        MeetingStateService? meetingStateService = null)
    {
        ViewModel = new SettingsViewModel(settingsProvider, calendarAccountManager, contentDirectory, meetingStateService);
        ViewModel.ConfirmRemoveAccountRequested += OnConfirmRemoveAccountRequested;
        InitializeComponent();
        PopulateAddAccountMenu();
    }

    /// <summary>
    /// Gets the view model for the settings view.
    /// </summary>
    internal SettingsViewModel ViewModel { get; }

    private void PopulateAddAccountMenu()
    {
        foreach (ProviderMenuItemViewModel provider in ViewModel.Providers)
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

    private async Task<bool> OnConfirmRemoveAccountRequested(AccountViewModel account)
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

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private void ToggleCalendarVisibility_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: CalendarViewModel calVm })
        {
            calVm.IsVisible = !calVm.IsVisible;
        }
    }

    private void CalendarColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not CalendarViewModel calVm)
        {
            return;
        }

        var picker = new ColorPicker
        {
            ColorSpectrumShape = ColorSpectrumShape.Ring,
            IsAlphaEnabled = false,
            IsMoreButtonVisible = false,
        };

        if (calVm.Color is string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                byte r = Convert.ToByte(hex[..2], 16);
                byte g = Convert.ToByte(hex[2..4], 16);
                byte b = Convert.ToByte(hex[4..6], 16);
                picker.Color = ColorHelper.FromArgb(255, r, g, b);
            }
        }

        // Update the preview circle live while the user drags.
        picker.ColorChanged += (_, args) =>
        {
            calVm.PreviewColor($"#{args.NewColor.R:X2}{args.NewColor.G:X2}{args.NewColor.B:X2}");
        };

        var flyout = new Flyout { Content = picker };

        // Persist once when the flyout closes (not on every drag).
        flyout.Closed += (_, _) =>
        {
            calVm.CommitColor();
        };

        flyout.ShowAt(button);
    }

    private void SyncNowCard_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SyncNowCommand.Execute(null);
    }
}
