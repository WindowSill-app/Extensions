using WindowSill.API;
using WindowSill.MediaControl.ViewModels;

namespace WindowSill.MediaControl.Views;

/// <summary>
/// The main media control view displayed in the sill.
/// </summary>
internal sealed partial class MediaControlView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MediaControlView"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for this view.</param>
    public MediaControlView(MediaControlViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        SillView = new SillView { Content = this };
        ConfigureSillView();

        SillView.IsSillOrientationOrSizeChanged += OnSillOrientationOrSizeChanged;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        ApplySillOrientationState();
    }

    /// <summary>
    /// Gets the view model.
    /// </summary>
    public MediaControlViewModel ViewModel { get; }

    /// <summary>
    /// Gets the <see cref="API.SillView"/> wrapper for this view.
    /// </summary>
    internal SillView SillView { get; }

    private void ConfigureSillView()
    {
        // Bind ShouldAppearInSill manually since SillView is created in code-behind.
        SillView.ShouldAppearInSill = ViewModel.ShouldAppearInSill;
        SillView.PreviewFlyoutPlacementTarget = ThumbnailAndTitlesGrid;

        var previewImage = new Image
        {
            Stretch = Stretch.Uniform,
            MaxHeight = 200,
        };
        previewImage.SetBinding(
            Image.SourceProperty,
            new Microsoft.UI.Xaml.Data.Binding
            {
                Source = ViewModel,
                Path = new PropertyPath(nameof(MediaControlViewModel.ThumbnailLarge)),
                Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay,
            });
        SillView.PreviewFlyoutContent = previewImage;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MediaControlViewModel.ShouldAppearInSill))
        {
            SillView.ShouldAppearInSill = ViewModel.ShouldAppearInSill;
        }
        else if (e.PropertyName == nameof(MediaControlViewModel.HasArtistName))
        {
            UpdateSongTextBlockAlignment();
        }
    }

    private void ThumbnailAndTitlesGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        ViewModel.SwitchToPlayingSourceWindowCommand.Execute(null);
    }

    private void MarqueeText_Loaded(object sender, RoutedEventArgs e)
    {
        ApplySillOrientationState();
    }

    private void OnSillOrientationOrSizeChanged(object? sender, EventArgs e)
    {
        MarqueeText.StopMarquee();
        ApplySillOrientationState();
    }

    /// <summary>
    /// Applies the appropriate visual state based on the current sill orientation and size.
    /// </summary>
    /// <remarks>
    /// NOTE: VisualStates may not function correctly in dynamically loaded extension DLLs.
    /// If VisualStateManager.GoToState returns false, properties are set directly as a fallback.
    /// </remarks>
    private void ApplySillOrientationState()
    {
        string stateName = SillView.SillOrientationAndSize switch
        {
            SillOrientationAndSize.HorizontalLarge => "HorizontalLarge",
            SillOrientationAndSize.HorizontalMedium => "HorizontalMedium",
            SillOrientationAndSize.HorizontalSmall => "HorizontalSmall",
            SillOrientationAndSize.VerticalLarge => "VerticalLarge",
            SillOrientationAndSize.VerticalMedium => "VerticalMedium",
            SillOrientationAndSize.VerticalSmall => "VerticalSmall",
            _ => "HorizontalLarge",
        };

        if (!VisualStateManager.GoToState(this, stateName, useTransitions: false))
        {
            // Fallback: set properties directly when VisualStates are unavailable.
            ApplyOrientationFallback(SillView.SillOrientationAndSize);
        }

        // Marquee-specific logic that cannot be expressed in VisualStates.
        if (SillView.SillOrientationAndSize == SillOrientationAndSize.HorizontalSmall)
        {
            MarqueeText.InvalidateArrange();
            MarqueeText.InvalidateMeasure();
            MarqueeText.StartMarquee();
        }

        UpdateSongTextBlockAlignment();
    }

    private void ApplyOrientationFallback(SillOrientationAndSize orientationAndSize)
    {
        switch (orientationAndSize)
        {
            case SillOrientationAndSize.HorizontalLarge:
                MarqueeText.Visibility = Visibility.Collapsed;
                ArtistTextBlock.Visibility = Visibility.Visible;
                SongTextBlock.Visibility = Visibility.Visible;
                ThumbnailImage.Visibility = Visibility.Visible;
                ArtistTextBlock.FontSize = 12;
                SongTextBlock.FontSize = 16;
                ThumbnailAndTitlesGrid.Width = 190;
                break;

            case SillOrientationAndSize.HorizontalMedium:
                MarqueeText.Visibility = Visibility.Collapsed;
                ArtistTextBlock.Visibility = Visibility.Visible;
                SongTextBlock.Visibility = Visibility.Visible;
                ThumbnailImage.Visibility = Visibility.Visible;
                ArtistTextBlock.FontSize = 10;
                SongTextBlock.FontSize = 10;
                ThumbnailAndTitlesGrid.Width = 160;
                break;

            case SillOrientationAndSize.HorizontalSmall:
                MarqueeText.Visibility = Visibility.Visible;
                ArtistTextBlock.Visibility = Visibility.Collapsed;
                SongTextBlock.Visibility = Visibility.Collapsed;
                ThumbnailImage.Visibility = Visibility.Visible;
                ArtistTextBlock.FontSize = 12;
                SongTextBlock.FontSize = (double)Application.Current.Resources["SillFontSize"];
                ThumbnailAndTitlesGrid.Width = 140;
                break;

            case SillOrientationAndSize.VerticalLarge:
                MarqueeText.Visibility = Visibility.Collapsed;
                ArtistTextBlock.Visibility = Visibility.Visible;
                SongTextBlock.Visibility = Visibility.Visible;
                ThumbnailImage.Visibility = Visibility.Visible;
                ArtistTextBlock.FontSize = 12;
                SongTextBlock.FontSize = 16;
                ThumbnailAndTitlesGrid.Width = double.NaN;
                break;

            case SillOrientationAndSize.VerticalMedium:
                MarqueeText.Visibility = Visibility.Collapsed;
                ArtistTextBlock.Visibility = Visibility.Visible;
                SongTextBlock.Visibility = Visibility.Visible;
                ThumbnailImage.Visibility = Visibility.Visible;
                ArtistTextBlock.FontSize = 12;
                SongTextBlock.FontSize = (double)Application.Current.Resources["SillFontSize"];
                ThumbnailAndTitlesGrid.Width = double.NaN;
                break;

            case SillOrientationAndSize.VerticalSmall:
                MarqueeText.Visibility = Visibility.Collapsed;
                ArtistTextBlock.Visibility = Visibility.Visible;
                SongTextBlock.Visibility = Visibility.Visible;
                ThumbnailImage.Visibility = Visibility.Collapsed;
                ArtistTextBlock.FontSize = 12;
                SongTextBlock.FontSize = (double)Application.Current.Resources["SillFontSize"];
                ThumbnailAndTitlesGrid.Width = double.NaN;
                break;

            default:
                throw new NotSupportedException($"Unsupported SillOrientationAndSize: {orientationAndSize}");
        }
    }

    private void UpdateSongTextBlockAlignment()
    {
        if (ViewModel.HasArtistName)
        {
            Grid.SetRowSpan(SongTextBlock, 1);
            SongTextBlock.VerticalAlignment = VerticalAlignment.Bottom;
        }
        else
        {
            Grid.SetRowSpan(SongTextBlock, 2);
            SongTextBlock.VerticalAlignment = VerticalAlignment.Center;
        }
    }
}
