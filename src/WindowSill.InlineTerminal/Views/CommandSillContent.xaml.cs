using Microsoft.UI.Xaml.Media.Imaging;
using WindowSill.API;
using WindowSill.InlineTerminal.ViewModels;
using Path = System.IO.Path;

namespace WindowSill.InlineTerminal.Views;

public sealed partial class CommandSillContent : UserControl, IDisposable
{
    private readonly SillListViewItem _sillListViewItem;

    internal CommandSillContent(
        IPluginInfo pluginInfo,
        SillListViewItem sillListViewItem,
        CommandViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        CompletedImage.Source = new SvgImageSource(new Uri(Path.Combine(pluginInfo.GetPluginContentDirectory(), "Assets", "ok.svg")));

        _sillListViewItem = sillListViewItem;
        _sillListViewItem.IsSillOrientationOrSizeChanged += SillListViewItem_IsSillOrientationOrSizeChanged;
        ApplyOrientationState(_sillListViewItem.SillOrientationAndSize);
    }

    internal CommandViewModel ViewModel { get; }

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

    public void Dispose()
    {
        // Disposed by TerminalSill when the sill is removed from the view list.
        ViewModel.Dispose();
    }
}
