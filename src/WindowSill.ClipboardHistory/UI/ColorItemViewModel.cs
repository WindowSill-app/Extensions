using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using WindowSill.API;
using WindowSill.ClipboardHistory.Utils;

namespace WindowSill.ClipboardHistory.UI;

internal sealed partial class ColorItemViewModel : ClipboardHistoryItemViewModelBase
{
    private readonly ILogger _logger;
    private readonly SillListViewButtonItem _view;

    private ColorItemViewModel(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
        : base(processInteractionService, item)
    {
        _logger = this.Log();
        _view = new SillListViewButtonItem(base.PasteCommand)
            .DataContext(
                this,
                (view, viewModel) => view
                    .PreviewFlyoutContent(
                        new Border()
                            .Name("PreviewFlyoutColorBorder")
                            .MinHeight(128)
                            .MinWidth(128)
                            .Padding(16)
                            .Background(x => x.Binding(() => viewModel.BackgroundBrush))
                            .Child(
                                new TextBlock()
                                    .Name("PreviewFlyoutColorTextBlock")
                                    .Style(x => x.ThemeResource("SubtitleTextBlockStyle"))
                                    .VerticalAlignment(VerticalAlignment.Center)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .HorizontalTextAlignment(TextAlignment.Center)
                                    .Foreground(x => x.Binding(() => viewModel.ForegroundBrush))
                                    .Text(x => x.Binding(() => viewModel.ColorText))

                            )
                    )
                    .Content(
                        new Border()
                            .Name("ColorBorder")
                            .CornerRadius(8)
                            .Padding(4, 2, 4, 2)
                            .Margin(x => x.ThemeResource("SillCommandContentMargin"))
                            .BorderBrush(x => x.ThemeResource("SurfaceStrokeColorDefaultBrush"))
                            .BorderThickness(1)
                            .Background(x => x.Binding(() => viewModel.BackgroundBrush))
                            .Child(
                                new TextBlock()
                                    .Name("ColorTextBlock")
                                    .VerticalAlignment(VerticalAlignment.Center)
                                    .TextTrimming(TextTrimming.CharacterEllipsis)
                                    .TextWrapping(TextWrapping.NoWrap)
                                    .Foreground(x => x.Binding(() => viewModel.ForegroundBrush))
                                    .Text(x => x.Binding(() => viewModel.ColorText))
                            ))
            );

        InitializeAsync().Forget();
    }

    internal static (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateView(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
    {
        var viewModel = new ColorItemViewModel(processInteractionService, item);
        return (viewModel, viewModel._view);
    }

    [ObservableProperty]
    public partial string ColorText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SolidColorBrush? BackgroundBrush { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush? ForegroundBrush { get; set; }

    private async Task InitializeAsync()
    {
        try
        {
            Guard.IsNotNull(Data);
            string colorString = await Data.GetTextAsync();
            ColorText = colorString;

            (SolidColorBrush background, SolidColorBrush foreground) = DataHelper.GetBackgroundAndForegroundBrushes(colorString);
            BackgroundBrush = background;
            ForegroundBrush = foreground;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to initialize {nameof(ColorItemViewModel)} control.");
        }
    }
}
