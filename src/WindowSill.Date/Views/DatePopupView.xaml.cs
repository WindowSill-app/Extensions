using Windows.System;

using WindowSill.API;
using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

/// <summary>
/// Popup view for the Date sill, displaying a calendar, event list, and world clocks.
/// </summary>
internal sealed partial class DatePopupView : SillPopupContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatePopupView"/> class.
    /// </summary>
    /// <param name="viewModel">The popup view model.</param>
    public DatePopupView(DatePopupViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        // Select today in the CalendarView.
        CalendarControl.SelectedDates.Add(DateTimeOffset.Now);
    }

    /// <summary>
    /// Gets the view model for data binding.
    /// </summary>
    internal DatePopupViewModel ViewModel { get; }

    /// <summary>
    /// Called when the popup opens.
    /// </summary>
    public override void OnOpening()
    {
        ViewModel.OnPopupOpening(DispatcherQueue);
    }

    /// <summary>
    /// Called when the popup closes.
    /// </summary>
    public override void OnClosing()
    {
        ViewModel.OnPopupClosing();
    }

    private void CalendarControl_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
    {
        if (args.AddedDates.Count > 0)
        {
            ViewModel.SelectedDate = args.AddedDates[0];
        }
    }

    private void CalendarControl_DayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
    {
        // Phase 0: basic setup. Density bars could be added here in a future pass
        // by fetching events for the visible month range and adding colors per day.
    }

    private void JoinMeeting_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { DataContext: EventItemViewModel eventVm } && eventVm.VideoCallUrl is not null)
        {
            Launcher.LaunchUriAsync(eventVm.VideoCallUrl).AsTask().ForgetSafely();
        }
    }

    private void OpenInCalendar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { DataContext: EventItemViewModel eventVm } && eventVm.WebLink is not null)
        {
            Launcher.LaunchUriAsync(eventVm.WebLink).AsTask().ForgetSafely();
        }
    }
}
