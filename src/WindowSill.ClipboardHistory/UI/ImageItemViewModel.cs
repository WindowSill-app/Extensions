using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using WindowSill.API;
using WindowSill.ClipboardHistory.Utils;

namespace WindowSill.ClipboardHistory.UI;

internal sealed partial class ImageItemViewModel : ClipboardHistoryItemViewModelBase
{
    private readonly ILogger _logger;
    private readonly SillListViewButtonItem _view;

    private ImageItemViewModel(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
        : base(processInteractionService, item)
    {
        _logger = this.Log();
        _view = new SillListViewButtonItem(base.PasteCommand)
            .DataContext(
                this,
                (view, viewModel) => view
                    .PreviewFlyoutContent(
                        new StackPanel()
                            .Spacing(8)
                            .Padding(8)
                            .Children(
                                new Border()
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .VerticalAlignment(VerticalAlignment.Top)
                                    .CornerRadius(8)
                                    .MaxHeight(x => x.Binding(() => viewModel.MaxHeight))
                                    .MaxWidth(x => x.Binding(() => viewModel.MaxWidth))
                                    .Child(
                                        new Image()
                                            .Stretch(Stretch.Uniform)
                                            .Source(x => x.Binding(() => viewModel.Image))
                                    ),

                                new TextBlock()
                                    .Style(x => x.ThemeResource("CaptionTextBlockStyle"))
                                    .Foreground(x => x.ThemeResource("TextFillColorSecondaryBrush"))
                                    .TextWrapping(TextWrapping.WrapWholeWords)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .Text(x => x.Binding(() => viewModel.Size))
                            )
                    )
                    .Content(
                        new Border()
                            .CornerRadius(4)
                            .Margin(x => x.ThemeResource("SillCommandContentMargin"))
                            .Child(
                                new Image()
                                    .Stretch(Stretch.Uniform)
                                    .Source(x => x.Binding(() => viewModel.Image))
                            )
                    )
            );

        InitializeAsync().Forget();
    }


    internal static (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateView(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
    {
        var viewModel = new ImageItemViewModel(processInteractionService, item);
        return (viewModel, viewModel._view);
    }

    [ObservableProperty]
    public partial string Size { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double MaxHeight { get; set; }

    [ObservableProperty]
    public partial double MaxWidth { get; set; }

    [ObservableProperty]
    public partial BitmapImage? Image { get; set; }

    private async Task InitializeAsync()
    {
        try
        {
            Guard.IsNotNull(Data);
            BitmapImage? bitmap = await DataHelper.GetBitmapAsync(Data);
            Image = bitmap;

            if (bitmap is not null)
            {
                MaxHeight = bitmap.PixelHeight;
                MaxWidth = bitmap.PixelWidth;
                Image = bitmap;
                Size
                    = string.Format(
                        "/WindowSill.ClipboardHistory/Misc/ImageSize".GetLocalizedString(),
                        bitmap.PixelWidth,
                        bitmap.PixelHeight);
            }
            else
            {
                // TODO: Show that we can't preview the image.
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to initialize {nameof(ImageItemViewModel)} control.");
        }
    }
}
