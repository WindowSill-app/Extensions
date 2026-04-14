using WindowSill.API;
using WindowSill.VideoHelper.ViewModels;

namespace WindowSill.VideoHelper.Views;

/// <summary>
/// Page displaying conversion results including a summary of successes/failures
/// and an option to open the output folder.
/// </summary>
internal sealed partial class ConvertVideoPopupResultPage : Page
{
    internal ConvertVideoPopupResultPage()
    {
        InitializeComponent();
    }

    internal ConvertVideoPopupViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = (ConvertVideoPopupViewModel)e.Parameter;

        if (ViewModel.Queue is not null)
        {
            int succeeded = ViewModel.Queue.SucceededCount;
            int total = ViewModel.Queue.Tasks.Count;
            SummaryText.Text = string.Format(
                "/WindowSill.VideoHelper/ConvertVideo/ResultSummary".GetLocalizedString(),
                succeeded,
                total);
        }
    }
}
