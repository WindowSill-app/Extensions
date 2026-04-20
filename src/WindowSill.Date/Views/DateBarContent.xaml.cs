using WindowSill.API;
using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

/// <summary>
/// Displays the Date sill bar content — either a calendar icon or live date/time text.
/// Adapts layout based on sill orientation and size via visual states.
/// </summary>
internal sealed partial class DateBarContent : UserControl
{
    private DateBarContent(DateBarViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for data binding.
    /// </summary>
    internal DateBarViewModel ViewModel { get; }

    /// <summary>
    /// Creates a <see cref="SillListViewPopupItem"/> with the date bar content and wires up
    /// orientation change handling.
    /// </summary>
    /// <param name="viewModel">The date bar view model.</param>
    /// <param name="popupView">The popup view to show when clicked.</param>
    /// <returns>A configured <see cref="SillListViewPopupItem"/>.</returns>
    internal static SillListViewPopupItem CreateViewListItem(DateBarViewModel viewModel, DatePopupView popupView)
    {
        var content = new DateBarContent(viewModel);

        var viewItem = new SillListViewPopupItem
        {
            Content = content,
            PopupContent = popupView,
        };

        viewItem.IsSillOrientationOrSizeChanged += (_, _) =>
        {
            content.ApplyOrientationState(viewItem.SillOrientationAndSize);
        };
        content.ApplyOrientationState(viewItem.SillOrientationAndSize);

        // Start the timer on the UI thread now that we have a dispatcher.
        viewModel.StartTimer(content.DispatcherQueue);

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
