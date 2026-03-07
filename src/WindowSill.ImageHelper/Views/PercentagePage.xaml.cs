using WindowSill.ImageHelper.ViewModels;

namespace WindowSill.ImageHelper.Views;

/// <summary>
/// Page for percentage-based resize controls.
/// </summary>
internal sealed partial class PercentagePage : Page
{
    public PercentagePage()
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
