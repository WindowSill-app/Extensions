using Microsoft.UI.Xaml.Media.Animation;
using WindowSill.API;
using WindowSill.VideoHelper.Services;
using WindowSill.VideoHelper.ViewModels;

namespace WindowSill.VideoHelper.Views;

/// <summary>
/// Popup content for video conversion, managing Frame-based navigation
/// between format selection, progress, and result pages.
/// </summary>
internal sealed partial class ConvertVideoPopup : SillPopupContent
{
    private readonly IVideoConversionService _videoConversionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConvertVideoPopup"/> class.
    /// </summary>
    /// <param name="videoConversionService">The conversion service.</param>
    /// <param name="viewModel">The view model for this popup.</param>
    internal ConvertVideoPopup(IVideoConversionService videoConversionService, ConvertVideoPopupViewModel viewModel)
    {
        _videoConversionService = videoConversionService;
        ViewModel = viewModel;

        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this popup.
    /// </summary>
    internal ConvertVideoPopupViewModel ViewModel { get; }

    private void SillPopupContent_Opening(object sender, EventArgs e)
    {
        // Re-subscribe to events (Closing unsubscribes them)
        ViewModel.ConversionStarted += ViewModel_ConversionStarted;
        ViewModel.ConversionCompleted += ViewModel_ConversionCompleted;

        // Wire up the FFmpeg availability check using this popup's XamlRoot
        if (ViewModel.PluginInfo is not null)
        {
            ViewModel.EnsureFfmpegAvailableAsync = () =>
                FFmpegDownloadDialogContent.ShowAndDownloadAsync(XamlRoot, ViewModel.PluginInfo);
        }

        // If this is the original "start new conversion" popup and a previous
        // queue already ran, reset so the user can start a fresh conversion.
        if (ViewModel.Files.Count > 0 && ViewModel.Queue is not null)
        {
            ViewModel.Reset();
        }

        if (ViewModel.Queue is null)
        {
            // No existing queue — show format selection
            ContentFrame.Navigate(typeof(ConvertVideoPopupFormatPage), ViewModel);
        }
        else if (ViewModel.Queue.State is VideoConversionQueueState.Completed or VideoConversionQueueState.Failed)
        {
            // Queue already finished — show results
            ContentFrame.Navigate(typeof(ConvertVideoPopupResultPage), ViewModel);
        }
        else
        {
            // Queue in progress — show progress
            ViewModel.ObserveQueueCompletion();
            ContentFrame.Navigate(typeof(ConvertVideoPopupProgressPage), ViewModel);
        }
    }

    private void SillPopupContent_Closing(object sender, EventArgs e)
    {
        // Do NOT cancel conversion — the service runs independently
        ViewModel.ConversionStarted -= ViewModel_ConversionStarted;
        ViewModel.ConversionCompleted -= ViewModel_ConversionCompleted;
        ViewModel.StopObservingQueue();

        // If the queue already completed or failed, remove it when the popup closes.
        if (ViewModel.Queue is not null && ViewModel.Queue.State is VideoConversionQueueState.Completed or VideoConversionQueueState.Failed)
        {
            _videoConversionService.Queues.Remove(ViewModel.Queue);
        }
    }

    private void ViewModel_ConversionStarted(object? sender, EventArgs e)
    {
        ContentFrame.Navigate(
            typeof(ConvertVideoPopupProgressPage),
            ViewModel,
            new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
    }

    private void ViewModel_ConversionCompleted(object? sender, EventArgs e)
    {
        ThreadHelper.RunOnUIThreadAsync(() =>
        {
            ContentFrame.Navigate(
                typeof(ConvertVideoPopupResultPage),
                ViewModel,
                new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
        }).ForgetSafely();
    }
}
