using WindowSill.FileHelper.ViewModels;

namespace WindowSill.FileHelper.Views;

/// <summary>
/// Page offering one button per format the selected files can be converted to.
/// </summary>
internal sealed partial class ConvertDocumentPopupStartPage : Page
{
    internal ConvertDocumentPopupStartPage()
    {
        InitializeComponent();
    }

    internal ConvertDocumentPopupViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = (ConvertDocumentPopupViewModel)e.Parameter;

        // The view model only exists once navigation supplies it, which is after InitializeComponent() already ran
        // the initial x:Bind pass against a null root. Re-run that pass so the target buttons are populated.
        Bindings.Update();
    }
}
