using WindowSill.VideoHelper.ViewModels;

namespace WindowSill.VideoHelper.Views;

/// <summary>
/// Page displaying format selection buttons in a uniform grid.
/// </summary>
internal sealed partial class ConvertVideoPopupFormatPage : Page
{
    internal ConvertVideoPopupFormatPage()
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
