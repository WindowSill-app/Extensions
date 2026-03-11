using WindowSill.VideoHelper.ViewModels;

namespace WindowSill.VideoHelper.Views;

/// <summary>
/// Page displaying per-file compression progress with a cancel option.
/// </summary>
internal sealed partial class CompressVideoPopupProgressPage : Page
{
    internal CompressVideoPopupProgressPage()
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
