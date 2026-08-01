using WindowSill.API;
using WindowSill.ImageHelper.ViewModels;

namespace WindowSill.ImageHelper.Views;

/// <summary>
/// Popup view for combining images into a single PDF.
/// </summary>
internal sealed partial class CombineImagesPopup : SillPopupContent
{
    internal CombineImagesPopup(CombineImagesViewModel viewModel)
    {
        DefaultStyleKey = typeof(CombineImagesPopup);
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this popup.
    /// </summary>
    internal CombineImagesViewModel ViewModel { get; }

    private void SillPopupContent_Opening(object sender, EventArgs e)
    {
        ViewModel.OnOpening();
    }

    private void SillPopupContent_Closing(object sender, EventArgs e)
    {
        ViewModel.OnClosing();
    }
}
