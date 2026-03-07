using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WindowSill.API;
using WindowSill.ShortTermReminder.Core;

namespace WindowSill.ShortTermReminder.ViewModels;

/// <summary>
/// ViewModel for the new reminder popup, handling input validation
/// and reminder creation.
/// </summary>
internal sealed partial class NewReminderPopupViewModel : ObservableObject
{
    private const int DefaultReminderDurationMinutes = 30;

    private readonly IReminderService _reminderService;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewReminderPopupViewModel"/> class.
    /// </summary>
    /// <param name="reminderService">The reminder service for creating reminders.</param>
    public NewReminderPopupViewModel(IReminderService reminderService)
    {
        _reminderService = reminderService;
    }

    /// <summary>
    /// Gets or sets the reminder description text.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    internal partial string ReminderText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of minutes until the reminder fires.
    /// </summary>
    [ObservableProperty]
    internal partial double Minutes { get; set; } = DefaultReminderDurationMinutes;

    /// <summary>
    /// Gets the formatted text showing the exact time the reminder will fire.
    /// </summary>
    [ObservableProperty]
    internal partial string ExactTimeText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action to close the popup.
    /// </summary>
    internal Action? CloseAction { get; set; }

    /// <summary>
    /// Gets a value indicating whether the OK button should be enabled.
    /// </summary>
    internal bool CanOk
        => !string.IsNullOrWhiteSpace(ReminderText) && !double.IsNaN(Minutes) && Minutes > 0;

    /// <summary>
    /// Resets the popup state when it is opened.
    /// </summary>
    internal void OnOpening()
    {
        ReminderText = string.Empty;
        Minutes = DefaultReminderDurationMinutes;
        UpdateExactTimeText();
    }

    /// <summary>
    /// Updates the exact time text based on the current minutes value.
    /// </summary>
    internal void UpdateExactTimeText()
    {
        double effectiveMinutes = double.IsNaN(Minutes) ? DefaultReminderDurationMinutes : Minutes;
        DateTime reminderTime = DateTime.Now.AddMinutes(effectiveMinutes);
        ExactTimeText = string.Format(
            "/WindowSill.ShortTermReminder/NewReminderPopup/WillRemindAt".GetLocalizedString(),
            reminderTime.ToString("h:mm tt"));
    }

    partial void OnMinutesChanged(double value)
    {
        UpdateExactTimeText();
        OkCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Creates the reminder and closes the popup.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOk))]
    private void Ok()
    {
        if (string.IsNullOrWhiteSpace(ReminderText))
        {
            return;
        }

        double effectiveMinutes = double.IsNaN(Minutes) ? DefaultReminderDurationMinutes : Minutes;
        var originalReminderDuration = TimeSpan.FromMinutes(effectiveMinutes);
        _reminderService.AddNewReminder(
            ReminderText,
            originalReminderDuration,
            DateTime.Now + originalReminderDuration);

        CloseAction?.Invoke();
    }
}
