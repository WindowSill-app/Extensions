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
    /// <returns>A configured <see cref="SillListViewPopupItem"/>.</returns>
    internal static SillListViewPopupItem CreateViewListItem(DateBarViewModel viewModel)
    {
        var content = new DateBarContent(viewModel);

        var viewItem = new SillListViewPopupItem
        {
            Content = content,
            PopupContent = new SillPopupContent(),
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

        if (!VisualStateManager.GoToState(this, stateName, useTransitions: true))
        {
            ApplyOrientationFallback(orientationAndSize);
        }
    }

    /// <summary>
    /// Fallback for when VisualStateManager.GoToState returns false in dynamically loaded extensions.
    /// </summary>
    private void ApplyOrientationFallback(SillOrientationAndSize orientationAndSize)
    {
        bool isLarge = orientationAndSize
            is SillOrientationAndSize.HorizontalLarge
            or SillOrientationAndSize.VerticalLarge;

        bool isVertical = orientationAndSize
            is SillOrientationAndSize.VerticalLarge
            or SillOrientationAndSize.VerticalMedium
            or SillOrientationAndSize.VerticalSmall;

        bool isSmall = orientationAndSize
            is SillOrientationAndSize.HorizontalSmall
            or SillOrientationAndSize.VerticalSmall;

        // Layout: two-line for Large or any Vertical; single-line for Horizontal Medium/Small.
        if (isLarge || isVertical)
        {
            DateTimePanel.Orientation = Orientation.Vertical;
            DateTimePanel.Spacing = 0;
            DateText.HorizontalAlignment = HorizontalAlignment.Center;
            TimeText.HorizontalAlignment = HorizontalAlignment.Center;
        }
        else
        {
            DateTimePanel.Orientation = Orientation.Horizontal;
            DateTimePanel.Spacing = 6;
            DateText.HorizontalAlignment = HorizontalAlignment.Left;
            TimeText.HorizontalAlignment = HorizontalAlignment.Left;
        }

        // Icon size
        double iconSize = isSmall ? 12 : (isLarge ? 20 : 16);
        CalendarIcon.Width = iconSize;
        CalendarIcon.Height = iconSize;

        // Margin
        RootPanel.Margin = isSmall
            ? new Thickness(0)
            : (Thickness)Application.Current.Resources["SillCommandContentMargin"];
    }
}
