using WindowSill.API;
using WindowSill.FileHelper.Services;
using WindowSill.FileHelper.ViewModels;

namespace WindowSill.FileHelper.Views;

/// <summary>
/// Page displaying conversion results including a summary of successes/failures
/// and an option to open the output folder.
/// </summary>
internal sealed partial class ConvertDocumentPopupResultPage : Page
{
    internal ConvertDocumentPopupResultPage()
    {
        InitializeComponent();
    }

    internal ConvertDocumentPopupViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = (ConvertDocumentPopupViewModel)e.Parameter;

        if (ViewModel.Queue is not null)
        {
            int succeeded = ViewModel.Queue.SucceededCount;
            int total = ViewModel.Queue.Tasks.Count;
            bool anySucceeded = succeeded > 0;
            bool wasCanceled = ViewModel.Queue.State == FileOperationQueueState.Canceled;

            if (wasCanceled)
            {
                // Cancellation is a distinct, neutral outcome — not a failure — even if some files had already
                // converted successfully before the user cancelled the rest of the queue.
                ResultIcon.Glyph = "\uE711";
                ResultIcon.Foreground = (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["SystemFillColorCautionBrush"];
                ResultTitle.Text = "/WindowSill.FileHelper/ConvertDocument/ResultCanceledTitle".GetLocalizedString();
            }
            else
            {
                ResultIcon.Glyph = anySucceeded ? "\uE73E" : "\uE783";
                ResultIcon.Foreground = anySucceeded
                    ? (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["SystemFillColorSuccessBrush"]
                    : (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["SystemFillColorCriticalBrush"];
                ResultTitle.Text = anySucceeded
                    ? "/WindowSill.FileHelper/ConvertDocument/ResultTitle".GetLocalizedString()
                    : "/WindowSill.FileHelper/ConvertDocument/ResultFailedTitle".GetLocalizedString();
            }

            SummaryText.Text = string.Format(
                "/WindowSill.FileHelper/ConvertDocument/ResultSummary".GetLocalizedString(),
                succeeded,
                total);

            OpenFolderButton.IsEnabled = anySucceeded;

            // Surface an actionable, per-file reason for every failed task instead of only the aggregate count,
            // so a failed conversion is never a silent/inert dead end.
            List<FileOperationTaskItem> failedTasks = ViewModel.Queue.Tasks.Where(task => task.IsFailed).ToList();
            FailedFilesList.ItemsSource = failedTasks;
            FailedFilesList.Visibility = failedTasks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
