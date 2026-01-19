using CommunityToolkit.Diagnostics;
using CommunityToolkit.WinUI.Controls;

using WindowSill.API;

namespace WindowSill.ImageHelper.ConvertImage;

internal sealed class MainPage : XamlLessPage
{
    private readonly UniformGrid _uniformGrid = new();

    public MainPage()
    {
        _uniformGrid.Columns = 2;

        this.DataContext(
            () => ViewModel,
            (view, viewModel) => view
            .Content(
                new Grid()
                    .RowDefinitions(
                        new GridLength(1, GridUnitType.Star),
                        GridLength.Auto
                    )
                    .Children(
                        new ScrollViewer()
                            .Grid(row: 0)
                            .VerticalScrollMode(ScrollMode.Auto)
                            .HorizontalScrollMode(ScrollMode.Disabled)
                            .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                            .Content(
                                new StackPanel()
                                    .Spacing(8)
                                    .Margin(24)
                                    .Children(
                                        new UniformGrid()
                                            .VerticalAlignment(VerticalAlignment.Top)
                                            .ColumnSpacing(8)
                                            .RowSpacing(8)
                                            .Children(
                                                new ShortcutButton()
                                                    .Style(x => x.StaticResource("LargeShortcutButtonStyle"))
                                                    .Content("AVIF")
                                                    .CommandParameter(ImageMagick.MagickFormat.Avif)
                                                    .Command(() => viewModel.ConvertCommand),
                                                new ShortcutButton()
                                                    .Style(x => x.StaticResource("LargeShortcutButtonStyle"))
                                                    .Content("BMP")
                                                    .CommandParameter(ImageMagick.MagickFormat.Bmp)
                                                    .Command(() => viewModel.ConvertCommand),
                                                new ShortcutButton()
                                                    .Style(x => x.StaticResource("LargeShortcutButtonStyle"))
                                                    .Content("EPS")
                                                    .CommandParameter(ImageMagick.MagickFormat.Eps)
                                                    .Command(() => viewModel.ConvertCommand),
                                                new ShortcutButton()
                                                    .Style(x => x.StaticResource("LargeShortcutButtonStyle"))
                                                    .Content("GIF")
                                                    .CommandParameter(ImageMagick.MagickFormat.Gif)
                                                    .Command(() => viewModel.ConvertCommand),
                                                new ShortcutButton()
                                                    .Style(x => x.StaticResource("LargeShortcutButtonStyle"))
                                                    .Content("PDF")
                                                    .CommandParameter(ImageMagick.MagickFormat.Pdf)
                                                    .Command(() => viewModel.ConvertCommand),
                                                new ShortcutButton()
                                                    .Style(x => x.StaticResource("LargeShortcutButtonStyle"))
                                                    .Content("TIFF")
                                                    .CommandParameter(ImageMagick.MagickFormat.Tiff)
                                                    .Command(() => viewModel.ConvertCommand),
                                                new ShortcutButton()
                                                    .Style(x => x.StaticResource("LargeShortcutButtonStyle"))
                                                    .Content("WEBP")
                                                    .CommandParameter(ImageMagick.MagickFormat.WebP)
                                                    .Command(() => viewModel.ConvertCommand)
                                            )
                                    )
                            ),

                        new Border()
                            .Grid(row: 1)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .VerticalAlignment(VerticalAlignment.Bottom)
                            .Padding(24)
                            .BorderThickness(0, 1, 0, 0)
                            .BorderBrush(x => x.ThemeResource("CardStrokeColorDefaultBrush"))
                            .Background(x => x.ThemeResource("LayerOnAcrylicFillColorDefaultBrush"))
                            .Child(
                                new Grid()
                                    .ColumnSpacing(8)
                                    .ColumnDefinitions(
                                        new GridLength(1, GridUnitType.Star),
                                        new GridLength(1, GridUnitType.Star)
                                    )
                                    .Children(
                                        new ShortcutButton()
                                            .Grid(column: 0)
                                            .Style(x => x.StaticResource("LargeShortcutButtonStyle"))
                                            .Content("PNG")
                                            .CommandParameter(ImageMagick.MagickFormat.Png)
                                            .Command(() => viewModel.ConvertCommand),
                                        new ShortcutButton()
                                            .Grid(column: 1)
                                            .Style(x => x.StaticResource("LargeShortcutButtonStyle"))
                                            .Content("JPEG")
                                            .CommandParameter(ImageMagick.MagickFormat.Jpeg)
                                            .Command(() => viewModel.ConvertCommand)
                                    )
                            )
                    )
            )
        );
    }

    internal ConvertImageViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(XamlLessNavigationEventArgs e)
    {
        Guard.IsNotNull(e.Parameter);
        Guard.IsOfType<ConvertImageViewModel>(e.Parameter);
        ViewModel = (ConvertImageViewModel)e.Parameter;
    }
}
