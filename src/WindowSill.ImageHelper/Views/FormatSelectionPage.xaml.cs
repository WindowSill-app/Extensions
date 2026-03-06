using Microsoft.UI.Xaml.Navigation;

using WindowSill.ImageHelper.ViewModels;

namespace WindowSill.ImageHelper.Views;

/// <summary>
/// Page for selecting the target image format.
/// </summary>
internal sealed partial class FormatSelectionPage : Page
{
    public FormatSelectionPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this page.
    /// </summary>
    internal ConvertImageViewModel ViewModel { get; private set; } = null!;

    /// <inheritdoc/>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = (ConvertImageViewModel)e.Parameter;
    }
}
