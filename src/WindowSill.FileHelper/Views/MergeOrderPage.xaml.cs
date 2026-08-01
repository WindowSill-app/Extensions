using WindowSill.API;
using WindowSill.FileHelper.ViewModels;

namespace WindowSill.FileHelper.Views;

/// <summary>
/// Page that lets the user arrange the selected PDFs into the order they should be merged in.
/// </summary>
internal sealed partial class MergeOrderPage : Page
{
    internal MergeOrderPage()
    {
        InitializeComponent();
    }

    internal MergeOrderViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = (MergeOrderViewModel)e.Parameter;

        // Navigation supplies the view model after InitializeComponent() already ran the first x:Bind pass
        // against a null root, so the bindings need re-evaluating.
        Bindings.Update();

        // Thumbnails and page counts arrive asynchronously; the rows render immediately without them.
        ViewModel.LoadPreviewsAsync().ForgetSafely();
    }
}
