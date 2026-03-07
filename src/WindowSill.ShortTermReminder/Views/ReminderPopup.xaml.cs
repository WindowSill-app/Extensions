using WindowSill.API;
using WindowSill.ShortTermReminder.Core;
using WindowSill.ShortTermReminder.ViewModels;

namespace WindowSill.ShortTermReminder.Views;

/// <summary>
/// Popup view displaying an existing reminder with a delete action.
/// </summary>
internal sealed partial class ReminderPopup : SillPopupContent
{
    internal ReminderPopup(Reminder reminder, IReminderService reminderService)
    {
        ViewModel = new ReminderPopupViewModel(reminder, reminderService)
        {
            CloseAction = Close
        };

        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this popup.
    /// </summary>
    internal ReminderPopupViewModel ViewModel { get; }

    /// <summary>
    /// Creates a new <see cref="SillPopupContent"/> instance for a reminder.
    /// </summary>
    /// <param name="reminder">The reminder to display.</param>
    /// <param name="reminderService">The reminder service.</param>
    /// <returns>A configured <see cref="SillPopupContent"/>.</returns>
    internal static SillPopupContent CreateView(Reminder reminder, IReminderService reminderService)
    {
        return new ReminderPopup(reminder, reminderService);
    }

    private void OnOpening(object sender, EventArgs args)
    {
        ViewModel.OnOpening();
    }
}
