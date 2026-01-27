using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Converters;

using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media.Animation;

using WindowSill.API;
using WindowSill.MediaControl.UI;

namespace WindowSill.MediaControl;

internal class MediaControlView
{
    private static readonly BoolToObjectConverter isPlayingToGlyphConverter
        = new() { FalseValue = "\uF5B0", TrueValue = "\uE62E" };

    private readonly MediaControlViewModel _viewModel;
    private readonly SillOrientedStackPanel _sillOrientedStackPanel = new();
    private readonly Grid _thumbnailAndTitlesGrid = new();
    private readonly Image _thumbnailImage = new();
    private readonly TextBlock _songTextBlock = new();
    private readonly TextBlock _artistTextBlock = new();
    private readonly Marquee _songAndArtistNameMarqueeText = new();

    // PROBLEMS TO FIX:
    // 1. I'm unable to make VisualState working. Both, C# Markup and XamlReader.Load(). Works
    //    fine if my dll isn't loaded dynamically.
    // 2. If MediaControlView inherits from SillView, then roughly 50% of the time, we get an error that says
    //    "Cannot apply a Style with TargetType 'WindowSill.API.SillView' to an object of type 'Microsoft.UI.Xaml.Controls.ContentControl'.

    public MediaControlView(ISettingsProvider settingsProvider)
    {
        View = new();
        _viewModel = new MediaControlViewModel(settingsProvider);

        _songAndArtistNameMarqueeText.Grid(row: 0, column: 1, rowSpan: 2);
        _songAndArtistNameMarqueeText.MinHeight(16);
        _songAndArtistNameMarqueeText.VerticalAlignment(VerticalAlignment.Center);
        _songAndArtistNameMarqueeText.Behavior = MarqueeBehavior.Looping;
        _songAndArtistNameMarqueeText.Direction = MarqueeDirection.Left;
        _songAndArtistNameMarqueeText.FontSize(x => x.ThemeResource("SillFontSize"));
        _songAndArtistNameMarqueeText.Foreground(x => x.ThemeResource("TextFillColorPrimaryBrush"));
        _songAndArtistNameMarqueeText.RepeatBehavior = RepeatBehavior.Forever;
        _songAndArtistNameMarqueeText.Speed = 25;
        _songAndArtistNameMarqueeText.StopMarquee();
        _songAndArtistNameMarqueeText.Loaded += SongAndArtistNameMarqueeText_Loaded;

        View.DataContext(
            _viewModel,
            (view, viewModel) => view
            .Content(
                _sillOrientedStackPanel
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch)
                    .Spacing(8)
                    .Children(
                        _thumbnailAndTitlesGrid
                            .IsHitTestVisible(true)
                            .Background(new SolidColorBrush(Colors.Transparent))
                            .ColumnSpacing(8)
                            .Visibility(x => x.Binding(() => viewModel.ShowTitleArtistAndThumbnail).Converter(new BoolToVisibilityConverter()))
                            .ColumnDefinitions(
                                new ColumnDefinition() { Width = GridLength.Auto },
                                new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) }
                            )
                            .RowDefinitions(
                                new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) },
                                new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) }
                            )
                            .Children(
                                new Grid()
                                    .Grid(row: 0, column: 0, rowSpan: 2)
                                    .CornerRadius(x => x.ThemeResource("ControlCornerRadius"))
                                    .Children(
                                        _thumbnailImage
                                            .Stretch(Stretch.Uniform)
                                            .Source(() => viewModel.Thumbnail)
                                    ),

                                _songAndArtistNameMarqueeText
                                    .Content(x => x.Binding(() => viewModel.SongAndArtistName).OneWay()),

                                _songTextBlock
                                    .Grid(row: 0, column: 1)
                                    .Style(x => x.ThemeResource("BodyTextBlockStyle"))
                                    .VerticalAlignment(VerticalAlignment.Bottom)
                                    .FontSize(x => x.ThemeResource("SillFontSize"))
                                    .TextWrapping(TextWrapping.NoWrap)
                                    .TextTrimming(TextTrimming.CharacterEllipsis)
                                    .Text(() => viewModel.SongName),

                                _artistTextBlock
                                    .Grid(row: 1, column: 1)
                                    .Style(x => x.ThemeResource("CaptionTextBlockStyle"))
                                    .VerticalAlignment(VerticalAlignment.Top)
                                    .FontSize(12)
                                    .FontWeight(FontWeights.SemiLight)
                                    .Opacity(0.5)
                                    .TextWrapping(TextWrapping.NoWrap)
                                    .TextTrimming(TextTrimming.CharacterEllipsis)
                                    .Text(() => viewModel.ArtistName)
                            ),

                        new StackPanel()
                            .Orientation(Orientation.Horizontal)
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .Spacing(8)
                            .Children(
                                new Button()
                                    .Style(x => x.StaticResource("IconButton"))
                                    .IsEnabled(() => viewModel.IsPreviousAvailable)
                                    .Command(x => x.Binding(() => viewModel.PreviousCommand).OneTime())
                                    .Content("\uE622"),
                                new Button()
                                    .Style(x => x.StaticResource("IconButton"))
                                    .IsEnabled(() => viewModel.IsPlayPauseAvailable)
                                    .Command(x => x.Binding(() => viewModel.PlayPauseCommand).OneTime())
                                    .Content(x => x.Binding(() => viewModel.IsPlaying).Converter(isPlayingToGlyphConverter)),
                                new Button()
                                    .Style(x => x.StaticResource("IconButton"))
                                    .IsEnabled(() => viewModel.IsNextAvailable)
                                    .Command(x => x.Binding(() => viewModel.NextCommand).OneTime())
                                    .Content("\uE623")
                            )
                    )
            )
            .ShouldAppearInSill(() => viewModel.ShouldAppearInSill)
            .PreviewFlyoutPlacementTarget(_thumbnailAndTitlesGrid)
            .PreviewFlyoutContent(
                new Image()
                    .Stretch(Stretch.Uniform)
                    .MaxHeight(200)
                    .Source(() => viewModel.ThumbnailLarge))
        );

        OnIsSillOrientationOrSizeChanged(null, EventArgs.Empty);
        View.IsSillOrientationOrSizeChanged += OnIsSillOrientationOrSizeChanged;
        _thumbnailAndTitlesGrid.PointerPressed += ThumbnailAndTitlesGrid_PointerPressed;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        ArtistNameUpdated();
    }

    internal SillView View { get; }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MediaControlViewModel.SongAndArtistName))
        {
            ArtistNameUpdated();
        }
    }

    private void ThumbnailAndTitlesGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _viewModel.SwitchToPlayingSourceWindow();
    }

    private void SongAndArtistNameMarqueeText_Loaded(object sender, RoutedEventArgs e)
    {
        OnIsSillOrientationOrSizeChanged(null, EventArgs.Empty);
    }

    private void OnIsSillOrientationOrSizeChanged(object? sender, EventArgs e)
    {
        _songAndArtistNameMarqueeText.StopMarquee();
        switch (View.SillOrientationAndSize)
        {
            case SillOrientationAndSize.HorizontalLarge:
                _songAndArtistNameMarqueeText.Visibility(Visibility.Collapsed);
                _artistTextBlock.Visibility(Visibility.Visible);
                _songTextBlock.Visibility(Visibility.Visible);
                _thumbnailImage.Visibility(Visibility.Visible);

                _artistTextBlock.FontSize(12);
                _songTextBlock.FontSize(16);
                _thumbnailAndTitlesGrid.Width(190);
                break;

            case SillOrientationAndSize.HorizontalMedium:
                _songAndArtistNameMarqueeText.Visibility(Visibility.Collapsed);
                _artistTextBlock.Visibility(Visibility.Visible);
                _songTextBlock.Visibility(Visibility.Visible);
                _thumbnailImage.Visibility(Visibility.Visible);

                _artistTextBlock.FontSize(10);
                _songTextBlock.FontSize(10);
                _thumbnailAndTitlesGrid.Width(160);
                break;

            case SillOrientationAndSize.HorizontalSmall:
                _songAndArtistNameMarqueeText.Visibility(Visibility.Visible);
                _artistTextBlock.Visibility(Visibility.Collapsed);
                _songTextBlock.Visibility(Visibility.Collapsed);
                _thumbnailImage.Visibility(Visibility.Visible);
                _songAndArtistNameMarqueeText.InvalidateArrange();
                _songAndArtistNameMarqueeText.InvalidateMeasure();

                _artistTextBlock.FontSize(12);
                _songTextBlock.FontSize(x => x.ThemeResource("SillFontSize"));
                _thumbnailAndTitlesGrid.Width(140);
                _songAndArtistNameMarqueeText.StartMarquee();
                break;

            case SillOrientationAndSize.VerticalLarge:
                _songAndArtistNameMarqueeText.Visibility(Visibility.Collapsed);
                _artistTextBlock.Visibility(Visibility.Visible);
                _songTextBlock.Visibility(Visibility.Visible);
                _thumbnailImage.Visibility(Visibility.Visible);

                _artistTextBlock.FontSize(12);
                _songTextBlock.FontSize(16);
                _thumbnailAndTitlesGrid.Width(double.NaN);
                break;

            case SillOrientationAndSize.VerticalMedium:
                _songAndArtistNameMarqueeText.Visibility(Visibility.Collapsed);
                _artistTextBlock.Visibility(Visibility.Visible);
                _songTextBlock.Visibility(Visibility.Visible);
                _thumbnailImage.Visibility(Visibility.Visible);

                _artistTextBlock.FontSize(12);
                _songTextBlock.FontSize(x => x.ThemeResource("SillFontSize"));
                _thumbnailAndTitlesGrid.Width(double.NaN);
                break;

            case SillOrientationAndSize.VerticalSmall:
                _songAndArtistNameMarqueeText.Visibility(Visibility.Collapsed);
                _artistTextBlock.Visibility(Visibility.Visible);
                _songTextBlock.Visibility(Visibility.Visible);
                _thumbnailImage.Visibility(Visibility.Collapsed);

                _artistTextBlock.FontSize(12);
                _songTextBlock.FontSize(x => x.ThemeResource("SillFontSize"));
                _thumbnailAndTitlesGrid.Width(double.NaN);
                break;

            default:
                throw new NotSupportedException($"Unsupported SillOrientationAndSize: {View.SillOrientationAndSize}");
        }
    }

    private void ArtistNameUpdated()
    {
        if (string.IsNullOrEmpty(_viewModel.ArtistName))
        {
            _songTextBlock.Grid(row: 0, column: 1, rowSpan: 2);
            _songTextBlock.VerticalAlignment(VerticalAlignment.Center);
        }
        else
        {
            _songTextBlock.Grid(row: 0, column: 1, rowSpan: 1);
            _songTextBlock.VerticalAlignment(VerticalAlignment.Bottom);
        }
    }
}
