using WindowSill.VideoHelper.ViewModels;

namespace WindowSill.VideoHelper.Views;

/// <summary>
/// Page displaying preset compression level buttons in a uniform grid.
/// </summary>
internal sealed partial class CompressVideoPopupCompressionLevelPresetPage : Page
{
    internal CompressVideoPopupCompressionLevelPresetPage()
    {
        InitializeComponent();
    }

    internal CompressVideoPopupViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = (CompressVideoPopupViewModel)e.Parameter;
    }
}
