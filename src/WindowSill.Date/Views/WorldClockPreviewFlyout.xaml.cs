using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

/// <summary>
/// Preview flyout content for a pinned world clock sill item.
/// Shows day/night icon, city name, time, date, and UTC offset.
/// </summary>
internal sealed partial class WorldClockPreviewFlyout : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorldClockPreviewFlyout"/> class.
    /// </summary>
    /// <param name="viewModel">The world clock sill item view model.</param>
    public WorldClockPreviewFlyout(WorldClockSillItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for data binding.
    /// </summary>
    internal WorldClockSillItemViewModel ViewModel { get; }
}
