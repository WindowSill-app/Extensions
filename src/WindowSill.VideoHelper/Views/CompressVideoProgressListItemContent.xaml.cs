using WindowSill.API;
using WindowSill.VideoHelper.Services;

namespace WindowSill.VideoHelper.Views;

public sealed partial class CompressVideoProgressListItemContent : UserControl
{
    private readonly SillListViewItem _sillListViewItem;

    internal CompressVideoProgressListItemContent(SillListViewItem sillListViewItem, VideoCompressionQueue viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        _sillListViewItem = sillListViewItem;
        _sillListViewItem.IsSillOrientationOrSizeChanged += SillListViewItem_IsSillOrientationOrSizeChanged;
        ApplyOrientationState(_sillListViewItem.SillOrientationAndSize);

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    internal VideoCompressionQueue ViewModel { get; }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VideoCompressionQueue.State))
        {
            if (ViewModel.State == VideoCompressionQueueState.Completed || ViewModel.State == VideoCompressionQueueState.Failed)
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
