using WindowSill.API;
using WindowSill.VideoHelper.ViewModels;

namespace WindowSill.VideoHelper.Views;

/// <summary>
/// Page displaying compression results including a summary of successes/failures
/// and an option to open the output folder.
/// </summary>
internal sealed partial class CompressVideoPopupResultPage : Page
{
    internal CompressVideoPopupResultPage()
    {
        InitializeComponent();
    }

    internal CompressVideoPopupViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = (CompressVideoPopupViewModel)e.Parameter;

        if (ViewModel.Queue is not null)
        {
            int succeeded = ViewModel.Queue.SucceededCount;
            int total = ViewModel.Queue.Tasks.Count;
            SummaryText.Text = string.Format(
                "/WindowSill.VideoHelper/CompressVideo/ResultSummary".GetLocalizedString(),
                succeeded,
                total);
        }
    }
}
