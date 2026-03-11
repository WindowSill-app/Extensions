using WindowSill.VideoHelper.ViewModels;

namespace WindowSill.VideoHelper.Views;

/// <summary>
/// Page displaying custom compression settings with manual control over
/// codec, quality, encoding speed, audio bitrate, resolution, and frame rate.
/// </summary>
internal sealed partial class CompressVideoPopupCompressionLevelCustomPage : Page
{
    internal CompressVideoPopupCompressionLevelCustomPage()
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
