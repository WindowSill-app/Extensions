using CommunityToolkit.Diagnostics;
using CommunityToolkit.WinUI.Converters;

using Microsoft.UI.Xaml.Media.Animation;

using WindowSill.API;
using WindowSill.ImageHelper.ViewModels;

namespace WindowSill.ImageHelper.Views;

/// <summary>
/// Popup view for image resizing with tab-based navigation.
/// </summary>
internal sealed partial class ResizeImagePopup : SillPopupContent
{
    private int _previousSelectedIndex = -1;

    internal ResizeImagePopup(ResizeImageViewModel viewModel)
    {
        DefaultStyleKey = typeof(ResizeImagePopup);
        ViewModel = viewModel;

        Resources["InverseBoolConverter"] = new BoolNegationConverter();

        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this popup.
    /// </summary>
    internal ResizeImageViewModel ViewModel { get; }

    private void SillPopupContent_Opening(object sender, EventArgs e)
    {
        ViewModel.OnOpening();
    }

    private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        SelectorBarItem selectedItem = ResizeModeSelectorBar.SelectedItem;
        int currentSelectedIndex = ResizeModeSelectorBar.Items.IndexOf(selectedItem);
        Type pageType;

        switch (currentSelectedIndex)
        {
            case 0:
                ViewModel.ResizeMode = ResizeMode.AbsoluteSize;
                pageType = typeof(AbsoluteSizePage);
                break;

            case 1:
                ViewModel.ResizeMode = ResizeMode.Percentage;
                pageType = typeof(PercentagePage);
                break;

            default:
                ThrowHelper.ThrowNotSupportedException();
                return;
        }

        if (_previousSelectedIndex == -1)
        {
            ContentFrame.Navigate(pageType, ViewModel, new EntranceNavigationTransitionInfo());
        }
        else
        {
            SlideNavigationTransitionEffect effect
                = currentSelectedIndex - _previousSelectedIndex > 0
                ? SlideNavigationTransitionEffect.FromRight
                : SlideNavigationTransitionEffect.FromLeft;

            ContentFrame.Navigate(pageType, ViewModel, new SlideNavigationTransitionInfo { Effect = effect });
        }

        _previousSelectedIndex = currentSelectedIndex;
    }
}
