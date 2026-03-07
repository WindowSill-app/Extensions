using CommunityToolkit.Mvvm.ComponentModel;

using System.Timers;

using WindowSill.API;
using WindowSill.ShortTermReminder.Core;

using Timer = System.Timers.Timer;

namespace WindowSill.ShortTermReminder.ViewModels;

/// <summary>
/// ViewModel for a reminder list item, managing the countdown timer
/// and exposing progress state for the view.
/// </summary>
internal sealed partial class ReminderListItemViewModel : ObservableObject, IDisposable
{
    private readonly Timer _timer;
    private readonly IReminderService _reminderService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReminderListItemViewModel"/> class.
    /// </summary>
    /// <param name="reminder">The reminder to track.</param>
    /// <param name="reminderService">The reminder service for notifications.</param>
    public ReminderListItemViewModel(Reminder reminder, IReminderService reminderService)
    {
        Reminder = reminder;
        _reminderService = reminderService;

        _timer = new Timer(1_000);
        _timer.Elapsed += TimerElapsed;
    }

    /// <summary>
    /// Gets the reminder being tracked.
    /// </summary>
    internal Reminder Reminder { get; }

    /// <summary>
    /// Gets the maximum value for the progress ring (total seconds).
    /// </summary>
    [ObservableProperty]
    internal partial double ProgressMaximum { get; set; }

    /// <summary>
    /// Gets the current value for the progress ring (remaining seconds).
    /// </summary>
    [ObservableProperty]
    internal partial double ProgressValue { get; set; }

    /// <summary>
    /// Gets the text displaying remaining minutes inside the progress ring.
    /// </summary>
    [ObservableProperty]
    internal partial string InnerMinuteText { get; set; } = string.Empty;

    /// <summary>
    /// Gets the text displaying remaining minutes outside the progress ring.
    /// </summary>
    [ObservableProperty]
    internal partial string OuterMinuteText { get; set; } = string.Empty;

    /// <summary>
    /// Gets the reminder title text.
    /// </summary>
    [ObservableProperty]
    internal partial string ReminderTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets the preview flyout text showing remaining time.
    /// </summary>
    [ObservableProperty]
    internal partial string PreviewTimeText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the dispatcher queue used to dispatch UI updates.
    /// </summary>
    internal Microsoft.UI.Dispatching.DispatcherQueue? DispatcherQueue { get; set; }

    /// <summary>
    /// Gets or sets the action to start the flashing animation on the view list item.
    /// </summary>
    internal Action? StartFlashingAction { get; set; }

    /// <summary>
    /// Refreshes the displayed title from the underlying reminder data.
    /// </summary>
    internal void RefreshTitle()
    {
        ReminderTitle = Reminder.Title;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }

    /// <summary>
    /// Ensures the countdown timer is running and resets progress bounds.
    /// </summary>
    internal void EnsureTimerRunning()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        ProgressMaximum = Reminder.OriginalReminderDuration.TotalSeconds;
        ProgressValue = Reminder.OriginalReminderDuration.TotalSeconds;
        ReminderTitle = Reminder.Title;

        if (!_timer.Enabled)
        {
            _timer.Start();
            UpdateState();
        }
    }

    private void TimerElapsed(object? sender, ElapsedEventArgs e)
    {
        UpdateState();
    }

    private void UpdateState()
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            try
            {
                TimeSpan remainingTime = Reminder.ReminderTime - DateTime.Now;
                if (remainingTime.TotalSeconds <= 0)
                {
                    _timer.Stop();
                    PreviewTimeText = "/WindowSill.ShortTermReminder/ReminderSillListViewPopupItem/ReminderPassed".GetLocalizedString();
                    InnerMinuteText = "0";
                    OuterMinuteText = "0";
                    ProgressValue = 0;

                    StartFlashingAction?.Invoke();
                    _reminderService.NotifyUserAsync(Reminder).Forget();
                }
                else
                {
                    PreviewTimeText = string.Format(
                        "/WindowSill.ShortTermReminder/ReminderSillListViewPopupItem/ReminderRemainingTime".GetLocalizedString(),
                        remainingTime.Minutes + 1,
                        Reminder.ReminderTime.ToString("h:mm tt"));
                    InnerMinuteText = remainingTime.TotalMinutes.ToString("0");
                    OuterMinuteText = remainingTime.TotalMinutes.ToString("0");
                    ProgressValue = remainingTime.TotalSeconds;
                }
            }
            catch
            {
                // Silently handle exceptions (e.g., if UI thread access fails)
            }
        });
    }
}
