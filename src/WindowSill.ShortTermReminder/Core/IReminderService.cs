using System.Collections.ObjectModel;

using WindowSill.API;

namespace WindowSill.ShortTermReminder.Core;

/// <summary>
/// Manages the lifecycle of short-term reminders including creation,
/// deletion, snoozing, and user notifications.
/// </summary>
internal interface IReminderService
{
    /// <summary>
    /// Gets the observable collection of view list items displayed in the sill.
    /// </summary>
    ObservableCollection<SillListViewItem> ViewList { get; }

    /// <summary>
    /// Initializes the service with the given settings provider and loads persisted reminders.
    /// </summary>
    /// <param name="settingsProvider">The settings provider for reading and writing reminder data.</param>
    Task InitializeAsync(ISettingsProvider settingsProvider);

    /// <summary>
    /// Creates a new reminder and inserts it into the view list in chronological order.
    /// </summary>
    /// <param name="reminderText">The reminder description text.</param>
    /// <param name="originalReminderDuration">The original duration before the reminder fires.</param>
    /// <param name="reminderTime">The absolute time when the reminder should fire.</param>
    void AddNewReminder(string reminderText, TimeSpan originalReminderDuration, DateTime reminderTime);

    /// <summary>
    /// Updates the title of a reminder and persists the change.
    /// </summary>
    /// <param name="reminder">The reminder whose title was updated.</param>
    void UpdateReminderTitle(Reminder reminder);

    /// <summary>
    /// Deletes a reminder by its unique identifier.
    /// </summary>
    /// <param name="reminderId">The unique identifier of the reminder to delete.</param>
    void DeleteReminder(Guid reminderId);

    /// <summary>
    /// Snoozes an existing reminder for the specified duration.
    /// </summary>
    /// <param name="reminder">The reminder to snooze.</param>
    /// <param name="snoozeDuration">The duration to snooze for.</param>
    void SnoozeReminder(Reminder reminder, TimeSpan snoozeDuration);

    /// <summary>
    /// Notifies the user that a reminder has fired, using either a full-screen
    /// notification or a toast notification based on settings.
    /// </summary>
    /// <param name="reminder">The reminder that has fired.</param>
    Task NotifyUserAsync(Reminder reminder);
}
