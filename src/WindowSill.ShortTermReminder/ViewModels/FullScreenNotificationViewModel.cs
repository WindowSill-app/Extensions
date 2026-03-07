using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Windows.System;

using WindowSill.ShortTermReminder.Core;

namespace WindowSill.ShortTermReminder.ViewModels;

/// <summary>
/// ViewModel for the full-screen notification window, providing
/// dismiss and snooze actions.
/// </summary>
internal sealed partial class FullScreenNotificationViewModel : ObservableObject
{
    private readonly Reminder _reminder;
    private readonly IReminderService _reminderService;
    private readonly Action _closeAction;

    /// <summary>
    /// Initializes a new instance of the <see cref="FullScreenNotificationViewModel"/> class.
    /// </summary>
    /// <param name="reminder">The reminder that fired.</param>
    /// <param name="reminderService">The reminder service for delete/snooze operations.</param>
    /// <param name="closeAction">The action to close the window.</param>
    public FullScreenNotificationViewModel(Reminder reminder, IReminderService reminderService, Action closeAction)
    {
        _reminder = reminder;
        _reminderService = reminderService;
        _closeAction = closeAction;
        ReminderTitle = reminder.Title;
    }

    /// <summary>
    /// Gets the reminder title text.
    /// </summary>
    [ObservableProperty]
    internal partial string ReminderTitle { get; set; }

    /// <summary>
    /// Opens the given URI in the default browser, then dismisses the reminder and closes the window.
    /// </summary>
    /// <param name="uri">The URI to open.</param>
    [RelayCommand]
    private async Task OpenLinkAndDismissAsync(Uri uri)
    {
        await Launcher.LaunchUriAsync(uri);
        Dismiss();
    }

    /// <summary>
    /// Dismisses (deletes) the reminder and closes the window.
    /// </summary>
    [RelayCommand]
    private void Dismiss()
    {
        _reminderService.DeleteReminder(_reminder.Id);
        _closeAction();
    }

    /// <summary>
    /// Snoozes the reminder for 5 minutes and closes the window.
    /// </summary>
    [RelayCommand]
    private void Snooze()
    {
        _reminderService.SnoozeReminder(_reminder, TimeSpan.FromMinutes(5));
        _closeAction();
    }
}
