using Windows.System;

using WindowSill.API;
using WindowSill.ShortTermReminder.Core;
using WindowSill.ShortTermReminder.ViewModels;

namespace WindowSill.ShortTermReminder.Views;

/// <summary>
/// Popup view for creating a new short-term reminder.
/// </summary>
internal sealed partial class NewReminderPopup : SillPopupContent
{
    internal NewReminderPopup(IReminderService reminderService)
    {
        ViewModel = new NewReminderPopupViewModel(reminderService)
        {
            CloseAction = Close
        };

        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this popup.
    /// </summary>
    internal NewReminderPopupViewModel ViewModel { get; }

    /// <summary>
    /// Creates a new <see cref="SillPopupContent"/> instance for use in the view list.
    /// </summary>
    /// <param name="reminderService">The reminder service.</param>
    /// <returns>A configured <see cref="SillPopupContent"/>.</returns>
    internal static SillPopupContent CreateView(IReminderService reminderService)
    {
        return new NewReminderPopup(reminderService);
    }

    private void OnOpening(object sender, EventArgs args)
    {
        ViewModel.OnOpening();
        ReminderTextBox.Focus(FocusState.Programmatic);
    }

    private void ReminderTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && ViewModel.CanOk)
        {
            e.Handled = true;
            Task.Run(async () =>
            {
                await Task.Delay(100);
                DispatcherQueue.TryEnqueue(() =>
                {
                    ViewModel.OkCommand.Execute(null);
                });
            });
        }
    }
}
