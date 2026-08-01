using WindowSill.API;
using WindowSill.FileHelper.ViewModels;

namespace WindowSill.FileHelper.Views;

/// <summary>
/// Non-clickable inline content that shows a ZIP archive's metadata summary (compression outcome, file/folder
/// counts and sizes) directly in the sill, adapting its density to the sill's orientation and size so it stays
/// legible within the limited width of a sill item.
/// </summary>
public sealed partial class ZipInfoSillContent : UserControl
{
    private readonly SillViewBase _host;
    private bool _isSubscribed;

    internal ZipInfoSillContent(ZipInfoViewModel viewModel, SillViewBase host)
    {
        ViewModel = viewModel;
        InitializeComponent();

        // The hosting list item is itself a SillViewBase: the sill framework keeps its SillOrientationAndSize in
        // sync with the bar size/location and raises IsSillOrientationOrSizeChanged when they change. We mirror
        // MediaControlView and drive our layout off that authoritative signal rather than watching settings
        // ourselves, which avoids ordering/staleness races between our read and the framework's own update.
        _host = host;
        ApplyOrientationState(_host.SillOrientationAndSize);
        ApplyHeadlineColor();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    internal ZipInfoViewModel ViewModel { get; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Guard against double-subscribing: Loaded can fire more than once for the same instance (e.g. when the
        // control is removed from and re-added to the visual tree without ever being garbage collected).
        if (_isSubscribed)
        {
            return;
        }

        _isSubscribed = true;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        _host.IsSillOrientationOrSizeChanged += Host_IsSillOrientationOrSizeChanged;

        // Re-sync now that we're listening again, in case the sill size/orientation or the load outcome changed
        // while this control wasn't subscribed.
        ApplyOrientationState(_host.SillOrientationAndSize);
        ApplyHeadlineColor();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // Guard against double-unsubscribing, and against unsubscribing handlers that were never attached.
        if (!_isSubscribed)
        {
            return;
        }

        _isSubscribed = false;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        _host.IsSillOrientationOrSizeChanged -= Host_IsSillOrientationOrSizeChanged;
    }

    private void Host_IsSillOrientationOrSizeChanged(object? sender, EventArgs e)
    {
        // The framework raises this synchronously on the UI thread after updating the host's SillOrientationAndSize,
        // so apply the new layout immediately — exactly like MediaControlView. Deferring via the dispatcher can let a
        // later realization overwrite the result, which left smaller tiers (e.g. Small) showing a stale line count.
        ApplyOrientationState(_host.SillOrientationAndSize);
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // The compression outcome (which drives the headline color) is only known once inspection finishes, so
        // re-apply the color whenever the outcome flags — or the loading state that gates them — change.
        if (e.PropertyName is nameof(ZipInfoViewModel.IsLoading)
            or nameof(ZipInfoViewModel.IsSmaller)
            or nameof(ZipInfoViewModel.IsLarger)
            or nameof(ZipInfoViewModel.IsNeutral))
        {
            ApplyHeadlineColor();
        }
    }

    /// <summary>
    /// Adapts which summary lines are shown, their font sizes, and the panel width to the current sill orientation
    /// and size tier. Horizontal sill bars are short, so only the tallest (Large) horizontal tier has room for a
    /// second line; the narrower vertical sill is tall enough to stack more detail. The most important line — the
    /// compression outcome — is always shown.
    /// </summary>
    private void ApplyOrientationState(SillOrientationAndSize orientationAndSize)
    {
        switch (orientationAndSize)
        {
            case SillOrientationAndSize.HorizontalLarge:
                // ~48px bar: fits two comfortable lines (headline + counts), like Media Control's song + artist.
                SetLines(showCounts: true, showSizes: false, headlineFontSize: 15, countsFontSize: 12, maxWidth: 240);
                break;

            case SillOrientationAndSize.HorizontalMedium:
                // ~32px bar: only one line of text fits without the second line being clipped, so show just the
                // single most important line (the compression outcome), at a comfortable size for that one line.
                SetLines(showCounts: false, showSizes: false, headlineFontSize: 13, countsFontSize: 10, maxWidth: 200);
                break;

            case SillOrientationAndSize.HorizontalSmall:
                // A small bar only has room for the single most important line: the compression outcome.
                SetLines(showCounts: false, showSizes: false, headlineFontSize: 12, countsFontSize: 10, maxWidth: 120);
                break;

            case SillOrientationAndSize.VerticalLarge:
                // The item's content area in a vertical bar fits about two lines (like Media Control's song + artist),
                // so show the outcome + counts and leave the exact byte sizes to the details popup. Showing a third
                // line here gets clipped.
                SetLines(showCounts: true, showSizes: false, headlineFontSize: 15, countsFontSize: 12, maxWidth: double.PositiveInfinity);
                break;

            case SillOrientationAndSize.VerticalMedium:
                SetLines(showCounts: true, showSizes: false, headlineFontSize: 13, countsFontSize: 11, maxWidth: double.PositiveInfinity);
                break;

            case SillOrientationAndSize.VerticalSmall:
                SetLines(showCounts: false, showSizes: false, headlineFontSize: 12, countsFontSize: 11, maxWidth: double.PositiveInfinity);
                break;

            default:
                throw new NotSupportedException($"Unsupported {nameof(SillOrientationAndSize)}: {orientationAndSize}");
        }
    }

    private void SetLines(bool showCounts, bool showSizes, double headlineFontSize, double countsFontSize, double maxWidth)
    {
        HeadlineText.FontSize = headlineFontSize;
        CountsText.FontSize = countsFontSize;

        CountsText.Visibility = showCounts ? Visibility.Visible : Visibility.Collapsed;
        SizesText.Visibility = showSizes ? Visibility.Visible : Visibility.Collapsed;

        // FrameworkElement.MaxWidth (unlike Width) rejects NaN with an ArgumentException, so an "unbounded" width
        // must be expressed as PositiveInfinity. Guard here as well so callers can't reintroduce the crash.
        SummaryPanel.MaxWidth = double.IsNaN(maxWidth) ? double.PositiveInfinity : maxWidth;
    }

    private void ApplyHeadlineColor()
    {
        string brushKey = ViewModel switch
        {
            { IsSmaller: true } => "SystemFillColorSuccessBrush",
            { IsLarger: true } => "SystemFillColorCautionBrush",
            _ => "TextFillColorPrimaryBrush",
        };

        if (Application.Current.Resources.TryGetValue(brushKey, out object? brush) && brush is Brush headlineBrush)
        {
            HeadlineText.Foreground = headlineBrush;
        }
    }
}
