using WindowSill.API;
using WindowSill.VideoHelper.Services;

namespace WindowSill.VideoHelper.Views;

/// <summary>
/// User control displaying conversion queue progress inline in the sill list.
/// </summary>
public sealed partial class ConvertVideoProgressListItemContent : UserControl
{
    private readonly SillListViewItem _sillListViewItem;

    internal ConvertVideoProgressListItemContent(SillListViewItem sillListViewItem, VideoConversionQueue viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        _sillListViewItem = sillListViewItem;
        _sillListViewItem.IsSillOrientationOrSizeChanged += SillListViewItem_IsSillOrientationOrSizeChanged;
        ApplyOrientationState(_sillListViewItem.SillOrientationAndSize);

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    internal VideoConversionQueue ViewModel { get; }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VideoConversionQueue.State))
        {
            if (ViewModel.State == VideoConversionQueueState.Completed || ViewModel.State == VideoConversionQueueState.Failed)
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
