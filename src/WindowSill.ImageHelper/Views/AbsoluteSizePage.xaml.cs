using WindowSill.ImageHelper.ViewModels;

namespace WindowSill.ImageHelper.Views;

/// <summary>
/// Page for absolute size resize controls (width, height, aspect ratio).
/// </summary>
internal sealed partial class AbsoluteSizePage : Page
{
    public AbsoluteSizePage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this page.
    /// </summary>
    internal ResizeImageViewModel ViewModel { get; private set; } = null!;

    /// <inheritdoc/>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = (ResizeImageViewModel)e.Parameter;
    }
}
