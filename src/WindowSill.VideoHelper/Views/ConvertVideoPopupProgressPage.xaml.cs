using WindowSill.VideoHelper.ViewModels;

namespace WindowSill.VideoHelper.Views;

/// <summary>
/// Page displaying per-file conversion progress with a cancel option.
/// </summary>
internal sealed partial class ConvertVideoPopupProgressPage : Page
{
    internal ConvertVideoPopupProgressPage()
    {
        InitializeComponent();
    }

    internal ConvertVideoPopupViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = (ConvertVideoPopupViewModel)e.Parameter;
    }
}
