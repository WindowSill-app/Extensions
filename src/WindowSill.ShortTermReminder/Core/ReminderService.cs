using System.ComponentModel.Composition;

using CommunityToolkit.Diagnostics;

using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

using System.Collections.ObjectModel;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

using WindowSill.API;
using WindowSill.ShortTermReminder.Views;

namespace WindowSill.ShortTermReminder.Core;

/// <summary>
/// Singleton service that manages reminder lifecycle, persistence, and notifications.
/// </summary>
[Export(typeof(IReminderService))]
internal sealed class ReminderService : IReminderService
{
    private ISettingsProvider? _settingsProvider;

    [ImportingConstructor]
    internal ReminderService()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        AppNotificationManager.Default.NotificationInvoked += OnToastNotificationInvoked;
        try
        {
            AppNotificationManager.Default.Register();
        }
        catch
        {
            /* It throws if already registered by another extension. */
        }

        ViewList.Add(
            new SillListViewPopupItem(
                new NewReminderListItemContent(),
                "/WindowSill.ShortTermReminder/NewReminderSillListViewPopupItem/NewReminderTooltip".GetLocalizedString(),
                NewReminderPopup.CreateView(this)));
    }

    /// <inheritdoc />
    public ObservableCollection<SillListViewItem> ViewList { get; } = new();

    /// <inheritdoc />
    public async Task InitializeAsync(ISettingsProvider settingsProvider)
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            Guard.IsNotNull(settingsProvider);
            _settingsProvider = settingsProvider;

            Reminder[] reminders = _settingsProvider.GetSetting(Settings.Settings.Reminders);
            for (int i = 0; i < reminders?.Length; i++)
            {
                ViewList.Add(ReminderListItemContent.CreateViewListItem(reminders[i], this));
            }
        });
    }

    /// <inheritdoc />
    public void AddNewReminder(string reminderText, TimeSpan originalReminderDuration, DateTime reminderTime)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Guard.IsNotNull(_settingsProvider);

        var reminder = new Reminder
        {
            Title = reminderText,
            OriginalReminderDuration = originalReminderDuration,
            ReminderTime = reminderTime
        };

        int insertIndex;

        for (insertIndex = 1; insertIndex < ViewList.Count; insertIndex++)
        {
            if (ViewList[insertIndex].DataContext is ViewModels.ReminderListItemViewModel reminderItem)
            {
                if (reminderItem.Reminder.ReminderTime > reminderTime)
                {
                    break;
                }
            }
        }

        ViewList.Insert(insertIndex, ReminderListItemContent.CreateViewListItem(reminder, this));

        SaveReminders();
    }

    /// <inheritdoc />
    public void UpdateReminderTitle(Reminder reminder)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Guard.IsNotNull(_settingsProvider);

        ViewModels.ReminderListItemViewModel? listItemViewModel
            = ViewList.Select(v => v.DataContext)
            .OfType<ViewModels.ReminderListItemViewModel>()
            .FirstOrDefault(r => r.Reminder == reminder);
        listItemViewModel?.RefreshTitle();

        SaveReminders();
    }

    /// <inheritdoc />
    public void DeleteReminder(Guid reminderId)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Guard.IsNotNull(_settingsProvider);

        SillListViewItem? itemToRemove = ViewList
            .FirstOrDefault(
                r => r.DataContext is ViewModels.ReminderListItemViewModel reminderViewModel
                && reminderViewModel.Reminder.Id == reminderId);

        if (itemToRemove is not null)
        {
            ViewList.Remove(itemToRemove);
            ((ViewModels.ReminderListItemViewModel)itemToRemove.DataContext).Dispose();
        }

        SaveReminders();
    }

    /// <inheritdoc />
    public void SnoozeReminder(Reminder reminder, TimeSpan snoozeDuration)
    {
        Guard.IsNotNull(_settingsProvider);

        reminder.OriginalReminderDuration = snoozeDuration;
        reminder.ReminderTime = DateTime.Now + snoozeDuration;

        ViewModels.ReminderListItemViewModel? itemToUpdate
            = ViewList.Select(v => v.DataContext)
            .OfType<ViewModels.ReminderListItemViewModel>()
            .FirstOrDefault(r => r.Reminder == reminder);
        itemToUpdate?.EnsureTimerRunning();

        SaveReminders();
    }

    /// <inheritdoc />
    public async Task NotifyUserAsync(Reminder reminder)
    {
        Guard.IsNotNull(_settingsProvider);
        if (_settingsProvider.GetSetting(Settings.Settings.UseFullScreenNotification))
        {
            // Enumerate all monitors
            var monitors = new List<RECT>();
            unsafe
            {
                PInvoke.EnumDisplayMonitors(
                    (HDC)default,
                    null,
                    (HMONITOR hMonitor, HDC hdcMonitor, RECT* lprcMonitor, LPARAM dwData) =>
                    {
                        if (lprcMonitor != null)
                        {
                            monitors.Add(*lprcMonitor);
                        }
                        return true;
                    },
                    (LPARAM)0);
            }

            if (monitors.Count == 0)
            {
                // Fallback to single window if no monitors detected
                var fallbackWindow = new FullScreenNotificationWindow(reminder, this);
                await fallbackWindow.ShowAsync();
            }
            else
            {
                var windows = new List<FullScreenNotificationWindow>();
                Action<FullScreenNotificationWindow> closeAllWindows = (FullScreenNotificationWindow closedWindow) =>
                {
                    // Close all other windows when one is closed
                    foreach (FullScreenNotificationWindow window in windows)
                    {
                        if (window != closedWindow)
                        {
                            window.Close();
                        }
                    }
                };

                // Create a window for each monitor
                bool isFirstWindow = true;
                foreach (RECT monitorRect in monitors)
                {
                    var window = new FullScreenNotificationWindow(reminder, this, monitorRect, closeAllWindows, playAudio: isFirstWindow);
                    windows.Add(window);
                    isFirstWindow = false;
                }

                // Show all windows and wait for the first one to close
                Task[] tasks = windows.Select(w => w.ShowAsync()).ToArray();
                await Task.WhenAny(tasks);
            }
        }
        else
        {
            AppNotification notification
                = new AppNotificationBuilder()
                    .AddArgument("reminderId", reminder.Id.ToString())
                    .AddText("/WindowSill.ShortTermReminder/Misc/ToastNotificationTitle".GetLocalizedString())
                    .AddText(reminder.Title)
                    .SetAudioEvent(AppNotificationSoundEvent.Reminder)
                    .BuildNotification();
            notification.ExpiresOnReboot = true;
            notification.Priority = AppNotificationPriority.High;

            AppNotificationManager.Default.Show(notification);
        }
    }

    private void SaveReminders()
    {
        Guard.IsNotNull(_settingsProvider);
        Reminder[] reminders
            = ViewList
            .Select(viewItem => viewItem.DataContext)
            .OfType<ViewModels.ReminderListItemViewModel>()
            .Select(reminderItem => reminderItem.Reminder)
            .ToArray();
        _settingsProvider.SetSetting(Settings.Settings.Reminders, reminders);
    }

    private void OnToastNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        if (args.Arguments.TryGetValue("reminderId", out string? reminderIdString) && Guid.TryParse(reminderIdString, out Guid reminderId))
        {
            ThreadHelper.RunOnUIThreadAsync(() =>
            {
                DeleteReminder(reminderId);
            });
        }
    }
}
