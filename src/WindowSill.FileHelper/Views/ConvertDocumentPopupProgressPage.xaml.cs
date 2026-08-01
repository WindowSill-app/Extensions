using WindowSill.FileHelper.ViewModels;

namespace WindowSill.FileHelper.Views;

/// <summary>
/// Page displaying per-file conversion progress with a cancel option.
/// </summary>
internal sealed partial class ConvertDocumentPopupProgressPage : Page
{
    internal ConvertDocumentPopupProgressPage()
    {
        InitializeComponent();
    }

    internal ConvertDocumentPopupViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = (ConvertDocumentPopupViewModel)e.Parameter;
    }
}
