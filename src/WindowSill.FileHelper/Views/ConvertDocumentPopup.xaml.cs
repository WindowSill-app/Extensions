using Microsoft.UI.Xaml.Media.Animation;
using WindowSill.API;
using WindowSill.FileHelper.Core;
using WindowSill.FileHelper.Services;
using WindowSill.FileHelper.ViewModels;

namespace WindowSill.FileHelper.Views;

/// <summary>
/// Popup content for the file actions experience, managing Frame-based navigation between the action list, the
/// optional configuration page for actions that need one (arranging a merge, picking pages to extract), and the
/// progress and result pages.
/// </summary>
internal sealed partial class ConvertDocumentPopup : SillPopupContent
{
    private readonly IFileOperationService _fileOperationService;

    /// <summary>
    /// The configuration page's view model, held so its PDF preview (and the native memory behind it) is released
    /// as soon as the user leaves that page or closes the popup.
    /// </summary>
    private IDisposable? _configurationViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConvertDocumentPopup"/> class.
    /// </summary>
    /// <param name="fileOperationService">The file operation service.</param>
    /// <param name="viewModel">The view model for this popup.</param>
    internal ConvertDocumentPopup(IFileOperationService fileOperationService, ConvertDocumentPopupViewModel viewModel)
    {
        _fileOperationService = fileOperationService;
        ViewModel = viewModel;

        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this popup.
    /// </summary>
    internal ConvertDocumentPopupViewModel ViewModel { get; }

    private void SillPopupContent_Opening(object sender, EventArgs e)
    {
        // Re-subscribe to events (Closing unsubscribes them)
        ViewModel.ConversionStarted += ViewModel_ConversionStarted;
        ViewModel.ConversionCompleted += ViewModel_ConversionCompleted;
        ViewModel.ConfigurationRequested += ViewModel_ConfigurationRequested;

        // If this is the original "start new conversion" popup and a previous
        // queue already ran, reset so the user can start a fresh conversion.
        if (ViewModel.Files.Count > 0 && ViewModel.Queue is not null)
        {
            ViewModel.Reset();
        }

        if (ViewModel.Queue is null)
        {
            ContentFrame.Navigate(typeof(ConvertDocumentPopupStartPage), ViewModel);
        }
        else if (ViewModel.Queue.State is FileOperationQueueState.Completed or FileOperationQueueState.Failed or FileOperationQueueState.Canceled)
        {
            // Queue already finished — show results.
            ContentFrame.Navigate(typeof(ConvertDocumentPopupResultPage), ViewModel);
        }
        else
        {
            // Queue in progress — show progress.
            ViewModel.ObserveQueueCompletion();
            ContentFrame.Navigate(typeof(ConvertDocumentPopupProgressPage), ViewModel);
        }
    }

    private void SillPopupContent_Closing(object sender, EventArgs e)
    {
        // Do NOT cancel conversion — the service runs independently of the popup lifetime.
        ViewModel.ConversionStarted -= ViewModel_ConversionStarted;
        ViewModel.ConversionCompleted -= ViewModel_ConversionCompleted;
        ViewModel.ConfigurationRequested -= ViewModel_ConfigurationRequested;
        ViewModel.StopObservingQueue();
        DisposeConfiguration();

        // If the queue already completed or failed, remove it when the popup closes.
        if (ViewModel.Queue is not null && ViewModel.Queue.State is FileOperationQueueState.Completed or FileOperationQueueState.Failed or FileOperationQueueState.Canceled)
        {
            _fileOperationService.Queues.Remove(ViewModel.Queue);
        }
    }

    /// <summary>
    /// Navigates to the page that lets the user configure the chosen action before it runs.
    /// </summary>
    private void ViewModel_ConfigurationRequested(object? sender, PdfAction action)
    {
        DisposeConfiguration();

        var transition = new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight };

        switch (action)
        {
            case PdfAction.Merge:
                var mergeViewModel = new MergeOrderViewModel(ViewModel);
                _configurationViewModel = mergeViewModel;
                ContentFrame.Navigate(typeof(MergeOrderPage), mergeViewModel, transition);
                break;

            case PdfAction.Extract when ViewModel.Files.Count > 0:
                var extractViewModel = new ExtractPagesViewModel(ViewModel, ViewModel.Files[0]);
                _configurationViewModel = extractViewModel;
                ContentFrame.Navigate(typeof(ExtractPagesPage), extractViewModel, transition);
                break;

            case PdfAction.Protect when ViewModel.Files.Count > 0:
            case PdfAction.Unlock when ViewModel.Files.Count > 0:
                var passwordViewModel = new PdfPasswordViewModel(
                    ViewModel,
                    ViewModel.Files[0].Path,
                    protect: action == PdfAction.Protect);
                ContentFrame.Navigate(typeof(PdfPasswordPage), passwordViewModel, transition);
                break;
        }
    }

    private void ViewModel_ConversionStarted(object? sender, EventArgs e)
    {
        // Leaving the configuration page: release its preview and cached page bitmaps straight away rather than
        // holding the document open for the duration of the work.
        DisposeConfiguration();

        ContentFrame.Navigate(
            typeof(ConvertDocumentPopupProgressPage),
            ViewModel,
            new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
    }

    private void DisposeConfiguration()
    {
        _configurationViewModel?.Dispose();
        _configurationViewModel = null;
    }

    private void ViewModel_ConversionCompleted(object? sender, EventArgs e)
    {
        ThreadHelper.RunOnUIThreadAsync(() =>
        {
            ContentFrame.Navigate(
                typeof(ConvertDocumentPopupResultPage),
                ViewModel,
                new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
        }).ForgetSafely();
    }
}
