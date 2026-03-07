using WindowSill.API;
using WindowSill.ImageHelper.ViewModels;

namespace WindowSill.ImageHelper.Views;

/// <summary>
/// Popup view for image compression.
/// </summary>
internal sealed partial class CompressImagePopup : SillPopupContent
{
    internal CompressImagePopup(CompressImageViewModel viewModel)
    {
        DefaultStyleKey = typeof(CompressImagePopup);
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this popup.
    /// </summary>
    internal CompressImageViewModel ViewModel { get; }

    private void SillPopupContent_Opening(object sender, EventArgs e)
    {
        ViewModel.OnOpening();
    }

    private void SillPopupContent_Closing(object sender, EventArgs e)
    {
        ViewModel.OnClosing();
    }
}
