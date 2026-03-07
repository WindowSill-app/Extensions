using WindowSill.API;
using WindowSill.ImageHelper.ViewModels;

namespace WindowSill.ImageHelper.Views;

/// <summary>
/// Page showing conversion progress for all files.
/// </summary>
internal sealed partial class ConversionProgressPage : Page
{
    public ConversionProgressPage()
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

        ViewModel.RunConversionAsync(ViewModel.CancellationToken).Forget();
    }
}
