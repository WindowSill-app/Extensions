using Microsoft.UI.Xaml.Media.Animation;

using WindowSill.API;
using WindowSill.ImageHelper.ViewModels;

namespace WindowSill.ImageHelper.Views;

/// <summary>
/// Popup view for image format conversion, using Frame navigation.
/// </summary>
internal sealed partial class ConvertImagePopup : SillPopupContent
{
    internal ConvertImagePopup(ConvertImageViewModel viewModel)
    {
        DefaultStyleKey = typeof(ConvertImagePopup);
        ViewModel = viewModel;
        InitializeComponent();

        ViewModel.FormatSelected += ViewModel_FormatSelected;
    }

    /// <summary>
    /// Gets the view model for this popup.
    /// </summary>
    internal ConvertImageViewModel ViewModel { get; }

    private void SillPopupContent_Opening(object sender, EventArgs e)
    {
        ContentFrame.Navigate(typeof(FormatSelectionPage), ViewModel);
    }

    private void SillPopupContent_Closing(object sender, EventArgs e)
    {
        ViewModel.OnClosing();
    }

    private void ViewModel_FormatSelected(object? sender, ImageMagick.MagickFormat e)
    {
        ContentFrame.Navigate(
            typeof(ConversionProgressPage),
            ViewModel,
            new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
    }
}
