using WindowSill.API;
using WindowSill.ShortTermReminder.Core;
using WindowSill.ShortTermReminder.ViewModels;

namespace WindowSill.ShortTermReminder.Views;

/// <summary>
/// UserControl providing the visual content for a reminder list item,
/// including a countdown progress ring and minute indicators.
/// </summary>
internal sealed partial class ReminderListItemContent : UserControl
{
    private ReminderListItemContent(Reminder reminder, IReminderService reminderService)
    {
        ViewModel = new ReminderListItemViewModel(reminder, reminderService);
        InitializeComponent();

        ViewModel.DispatcherQueue = DispatcherQueue;
    }

    /// <summary>
    /// Gets the view model for this list item content.
    /// </summary>
    internal ReminderListItemViewModel ViewModel { get; }

    /// <summary>
    /// Creates a <see cref="SillListViewPopupItem"/> with the reminder content and popup.
    /// </summary>
    /// <param name="reminder">The reminder to display.</param>
    /// <param name="reminderService">The reminder service.</param>
    /// <returns>A configured <see cref="SillListViewPopupItem"/>.</returns>
    internal static SillListViewPopupItem CreateViewListItem(Reminder reminder, IReminderService reminderService)
    {
        var content = new ReminderListItemContent(reminder, reminderService);

        var previewFlyout = new ReminderPreviewFlyoutContent(content.ViewModel);

        SillListViewPopupItem viewItem = new SillListViewPopupItem()
            .PreviewFlyoutContent(previewFlyout);

        viewItem.DataContext = content.ViewModel;
        viewItem.Content = content;
        viewItem.PopupContent = ReminderPopup.CreateView(reminder, reminderService);

        content.ViewModel.StartFlashingAction = viewItem.StartFlashing;

        content.ViewModel.EnsureTimerRunning();

        viewItem.IsSillOrientationOrSizeChanged += (sender, e) =>
        {
            content.ApplyOrientationState(viewItem.SillOrientationAndSize);
        };
        content.ApplyOrientationState(viewItem.SillOrientationAndSize);

        return viewItem;
    }

    private void ApplyOrientationState(SillOrientationAndSize orientationAndSize)
    {
        string stateName = orientationAndSize switch
        {
            SillOrientationAndSize.HorizontalLarge => "HorizontalLarge",
            SillOrientationAndSize.HorizontalMedium => "HorizontalMedium",
            SillOrientationAndSize.HorizontalSmall => "HorizontalSmall",
            SillOrientationAndSize.VerticalLarge => "VerticalLarge",
            SillOrientationAndSize.VerticalMedium => "VerticalMedium",
            SillOrientationAndSize.VerticalSmall => "VerticalSmall",
            _ => throw new NotSupportedException($"Unsupported {nameof(SillOrientationAndSize)}: {orientationAndSize}")
        };

        VisualStateManager.GoToState(this, stateName, useTransitions: true);
    }
}
