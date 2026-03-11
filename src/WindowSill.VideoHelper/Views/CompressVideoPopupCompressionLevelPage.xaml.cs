using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Media.Animation;
using WindowSill.VideoHelper.ViewModels;

namespace WindowSill.VideoHelper.Views;

/// <summary>
/// Page containing a SelectorBar with Preset and Custom tabs for choosing compression level.
/// </summary>
internal sealed partial class CompressVideoPopupCompressionLevelPage : Page
{
    internal CompressVideoPopupCompressionLevelPage()
    {
        InitializeComponent();
    }

    internal CompressVideoPopupViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = (CompressVideoPopupViewModel)e.Parameter;
        TabFrame.Navigate(typeof(CompressVideoPopupCompressionLevelPresetPage), ViewModel);
    }

    private void TabSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem == PresetTab)
        {
            TabFrame.Navigate(typeof(CompressVideoPopupCompressionLevelPresetPage), ViewModel, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft });
        }
        else if (sender.SelectedItem == CustomTab)
        {
            TabFrame.Navigate(typeof(CompressVideoPopupCompressionLevelCustomPage), ViewModel, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }
    }
}
