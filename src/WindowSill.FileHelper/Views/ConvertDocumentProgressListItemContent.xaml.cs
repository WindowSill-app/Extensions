using WindowSill.API;
using WindowSill.FileHelper.Services;

namespace WindowSill.FileHelper.Views;

/// <summary>
/// User control displaying conversion queue progress inline in the sill list.
/// </summary>
public sealed partial class ConvertDocumentProgressListItemContent : UserControl
{
    private readonly SillListViewItem _sillListViewItem;
    private bool _isSubscribed;

    internal ConvertDocumentProgressListItemContent(SillListViewItem sillListViewItem, FileOperationQueue viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        _sillListViewItem = sillListViewItem;
        ApplyOrientationState(_sillListViewItem.SillOrientationAndSize);

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    internal FileOperationQueue ViewModel { get; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Guard against double-subscribing: Loaded can fire more than once for the same instance (e.g. when the
        // control is removed from and re-added to the visual tree without ever being garbage collected).
        if (_isSubscribed)
        {
            return;
        }

        _isSubscribed = true;
        _sillListViewItem.IsSillOrientationOrSizeChanged += SillListViewItem_IsSillOrientationOrSizeChanged;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        // The sill's orientation/size may have changed while this control wasn't subscribed (e.g. between an
        // Unloaded and a subsequent Loaded), so re-sync the visual state now that we're listening again.
        ApplyOrientationState(_sillListViewItem.SillOrientationAndSize);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // Guard against double-unsubscribing, and against unsubscribing handlers that were never attached.
        if (!_isSubscribed)
        {
            return;
        }

        _isSubscribed = false;
        _sillListViewItem.IsSillOrientationOrSizeChanged -= SillListViewItem_IsSillOrientationOrSizeChanged;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileOperationQueue.State))
        {
            if (ViewModel.State == FileOperationQueueState.Completed
                || ViewModel.State == FileOperationQueueState.Failed
                || ViewModel.State == FileOperationQueueState.Canceled)
            {
                _sillListViewItem.StartFlashing();
            }
        }
    }

    private void SillListViewItem_IsSillOrientationOrSizeChanged(object? sender, EventArgs e)
    {
        ApplyOrientationState(_sillListViewItem.SillOrientationAndSize);
    }

    private void ApplyOrientationState(SillOrientationAndSize orientationAndSize)
    {
        string stateName = orientationAndSize switch
        {
            SillOrientationAndSize.HorizontalLarge => "HorizontalLarge",
            SillOrientationAndSize.HorizontalMedium => "HorizontalMedium",
            SillOrientationAndSize.HorizontalSmall => "HorizontalSmall",
            SillOrientationAndSize.VerticalLarge => "VerticalLarge",
            SillOrientationAndSize.VerticalMedium => "VerticalMedium",
            SillOrientationAndSize.VerticalSmall => "VerticalSmall",
            _ => throw new NotSupportedException($"Unsupported {nameof(SillOrientationAndSize)}: {orientationAndSize}")
        };

        VisualStateManager.GoToState(this, stateName, useTransitions: true);
    }
}
