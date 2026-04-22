using WindowSill.API;
using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

/// <summary>
/// Bar content for a pinned world clock sill item.
/// Shows city name + time in single line (Medium/Small) or two lines (Large).
/// </summary>
internal sealed partial class WorldClockBarContent : UserControl
{
    private WorldClockBarContent(WorldClockSillItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for data binding.
    /// </summary>
    internal WorldClockSillItemViewModel ViewModel { get; }

    /// <summary>
    /// Creates a new <see cref="WorldClockBarContent"/> instance.
    /// </summary>
    /// <param name="viewModel">The world clock sill item view model.</param>
    /// <returns>The bar content control.</returns>
    internal static WorldClockBarContent Create(WorldClockSillItemViewModel viewModel)
    {
        return new WorldClockBarContent(viewModel);
    }

    /// <summary>
    /// Applies the visual state for the given orientation and size.
    /// </summary>
    /// <param name="orientationAndSize">The current sill orientation and size.</param>
    internal void ApplyOrientationState(SillOrientationAndSize orientationAndSize)
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
