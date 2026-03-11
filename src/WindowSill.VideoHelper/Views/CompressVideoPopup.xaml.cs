using Microsoft.UI.Xaml.Media.Animation;
using WindowSill.API;
using WindowSill.VideoHelper.Services;
using WindowSill.VideoHelper.ViewModels;

namespace WindowSill.VideoHelper.Views;

/// <summary>
/// Popup content for video compression, managing Frame-based navigation
/// between compression level selection, progress, and result pages.
/// </summary>
internal sealed partial class CompressVideoPopup : SillPopupContent
{
    private readonly IVideoCompressionService _videoCompressionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompressVideoPopup"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for this popup.</param>
    internal CompressVideoPopup(IVideoCompressionService videoCompressionService, CompressVideoPopupViewModel viewModel)
    {
        _videoCompressionService = videoCompressionService;
        ViewModel = viewModel;

        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this popup.
    /// </summary>
    internal CompressVideoPopupViewModel ViewModel { get; }

    private void SillPopupContent_Opening(object sender, EventArgs e)
    {
        // Re-subscribe to events (Closing unsubscribes them)
        ViewModel.CompressionStarted += ViewModel_CompressionStarted;
        ViewModel.CompressionCompleted += ViewModel_CompressionCompleted;

        // Wire up the FFmpeg availability check using this popup's XamlRoot
        if (ViewModel.PluginInfo is not null)
        {
            ViewModel.EnsureFfmpegAvailableAsync = () =>
                FFmpegDownloadDialogContent.ShowAndDownloadAsync(XamlRoot, ViewModel.PluginInfo);
        }

        // If this is the original "start new compression" popup and a previous
        // queue already ran, reset so the user can start a fresh compression.
        if (ViewModel.Files.Count > 0 && ViewModel.Queue is not null)
        {
            ViewModel.Reset();
        }

        if (ViewModel.Queue is null)
        {
            // No existing queue — show compression level selection
            ContentFrame.Navigate(typeof(CompressVideoPopupCompressionLevelPage), ViewModel);
        }
        else if (ViewModel.Queue.State is VideoCompressionQueueState.Completed or VideoCompressionQueueState.Failed)
        {
            // Queue already finished — show results
            ContentFrame.Navigate(typeof(CompressVideoPopupResultPage), ViewModel);
        }
        else
        {
            // Queue in progress — show progress
            ViewModel.ObserveQueueCompletion();
            ContentFrame.Navigate(typeof(CompressVideoPopupProgressPage), ViewModel);
        }
    }

    private void SillPopupContent_Closing(object sender, EventArgs e)
    {
        // Do NOT cancel compression — the service runs independently
        ViewModel.CompressionStarted -= ViewModel_CompressionStarted;
        ViewModel.CompressionCompleted -= ViewModel_CompressionCompleted;
        ViewModel.StopObservingQueue();

        // If the queue already completed or failed, remove it when the popup closes. This will
        // cause the list of Sills to refresh and our item to disappear.
        if (ViewModel.Queue is not null && ViewModel.Queue.State is VideoCompressionQueueState.Completed or VideoCompressionQueueState.Failed)
        {
            _videoCompressionService.Queues.Remove(ViewModel.Queue);
        }
    }

    private void ViewModel_CompressionStarted(object? sender, EventArgs e)
    {
        ContentFrame.Navigate(
            typeof(CompressVideoPopupProgressPage),
            ViewModel,
            new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
    }

    private void ViewModel_CompressionCompleted(object? sender, EventArgs e)
    {
        ThreadHelper.RunOnUIThreadAsync(() =>
        {
            ContentFrame.Navigate(
                typeof(CompressVideoPopupResultPage),
                ViewModel,
                new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
        }).ForgetSafely();
    }
}

