using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ImageMagick;

using Microsoft.UI.Xaml.Media.Animation;

using Windows.Storage;

using WindowSill.API;
using WindowSill.ImageHelper.Helpers;

namespace WindowSill.ImageHelper.ResizeImage;

internal sealed partial class ResizeImageViewModel : ObservableObject
{
    private readonly IStorageFile _file;
    private readonly SillPopupContent _view;
    private readonly SelectorBar _selectorBar = new();
    private readonly XamlLessFrame _mainFrame = new();

    private int _previousSelectedIndex = -1;
    private uint _originalWidth;
    private uint _originalHeight;
    private bool _userIsChangingWidth;
    private bool _userIsChangingHeight;

    private ResizeImageViewModel(IStorageFile file)
    {
        _file = file;

        _view
            = new SillPopupContent(OnOpening)
                .Width(350)
                .Height(275)
                .DataContext(
                    this,
                    (view, viewModel) => view
                    .Content(
                        new Grid()
                            .RowDefinitions(
                                GridLength.Auto,
                                new GridLength(1, GridUnitType.Star),
                                GridLength.Auto
                            )
                            .Children(
                                _selectorBar
                                    .Grid(row: 0)
                                    .Margin(24, 8, 24, 8)
                                    .Items(
                                        new SelectorBarItem()
                                            .Text("/WindowSill.ImageHelper/ResizeImage/AbsoluteSizeBarItem".GetLocalizedString())
                                            .IsSelected(true),

                                        new SelectorBarItem()
                                            .Text("/WindowSill.ImageHelper/ResizeImage/PercentageBarItem".GetLocalizedString())
                                    // TODO: Social Media
                                    ),

                                _mainFrame
                                    .Grid(row: 1)
                                    .Padding(24, 0, 24, 0)
                                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                                    .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                                    .VerticalAlignment(VerticalAlignment.Stretch)
                                    .VerticalContentAlignment(VerticalAlignment.Stretch)
                                    .IsNavigationStackEnabled(false)
                                    .ContentTransitions(
                                        new NavigationThemeTransition()
                                        {
                                            DefaultNavigationTransitionInfo = new SlideNavigationTransitionInfo()
                                            {
                                                Effect = SlideNavigationTransitionEffect.FromRight
                                            }
                                        }),

                                new Border()
                                    .Grid(row: 2)
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
                                                new ProgressRing()
                                                    .Name("ResizeProgressRing")
                                                    .IsIndeterminate(x => x.Binding(() => viewModel.IsResizeInProgress).OneWay())
                                                    .HorizontalAlignment(HorizontalAlignment.Right),

                                                new Button()
                                                    .Name("ResizeButton")
                                                    .Grid(column: 1)
                                                    .Style(x => x.StaticResource("LargeButtonStyle"))
                                                    .IsEnabled(x => x.Binding(() => viewModel.IsResizeInProgress).OneWay().Convert(value => !value))
                                                    .Content("/WindowSill.ImageHelper/ResizeImage/ResizeButton".GetLocalizedString())
                                                    .Command(() => viewModel.ResizeCommand)
                                            )
                                    )
                            )
                    )
                );

        _selectorBar.SelectionChanged += SelectorBar_SelectionChanged;
    }

    internal static SillPopupContent CreateView(IStorageFile file)
    {
        Guard.IsNotNull(file, nameof(file));
        return new ResizeImageViewModel(file)._view;
    }

    [ObservableProperty]
    public partial ResizeMode ResizeMode { get; set; }

    [ObservableProperty]
    public partial uint Width { get; set; }

    [ObservableProperty]
    public partial uint Height { get; set; }

    [ObservableProperty]
    public partial bool MaintainAspectRatio { get; set; } = true;

    [ObservableProperty]
    public partial int Percentage { get; set; } = 100;

    [ObservableProperty]
    public partial bool IsResizeInProgress { get; set; }

    partial void OnHeightChanging(uint value)
    {
        if (MaintainAspectRatio && !_userIsChangingWidth)
        {
            _userIsChangingHeight = true;
            Width = value * _originalWidth / _originalHeight;
        }

        _userIsChangingHeight = false;
    }

    partial void OnWidthChanging(uint value)
    {
        if (MaintainAspectRatio && !_userIsChangingHeight)
        {
            _userIsChangingWidth = true;
            Height = value * _originalHeight / _originalWidth;
        }

        _userIsChangingWidth = false;
    }

    partial void OnMaintainAspectRatioChanged(bool value)
    {
        if (MaintainAspectRatio)
        {
            Height = _originalHeight * Width / _originalWidth;
        }
    }

    [RelayCommand]
    private void Resize()
    {
        IsResizeInProgress = true;

        ResizeAsync().ContinueWith(task =>
        {
            ThreadHelper.RunOnUIThreadAsync(() =>
            {
                IsResizeInProgress = false;
                if (task.IsFaulted)
                {
                    // Handle error, e.g., show a message to the user
                    // For example: ShowErrorMessage(task.Exception);
                }
                else
                {
                    _view.Close();
                }
            });
        });
    }

    private void OnOpening()
    {
        try
        {
            using var image = new MagickImage(_file.Path);
            _originalWidth = image.Width;
            _originalHeight = image.Height;
            Width = _originalWidth;
            Height = _originalHeight;
        }
        catch (Exception ex)
        {
            // TODO: Log the exception and display it to the user.
        }
    }

    internal async Task ResizeAsync()
    {
        await Task.Run(() =>
        {
            MagickGeometry newSize;

            switch (ResizeMode)
            {
                case ResizeMode.AbsoluteSize:
                    newSize = new MagickGeometry(Width, Height)
                    {
                        IgnoreAspectRatio = !MaintainAspectRatio
                    };
                    break;

                case ResizeMode.Percentage:
                    uint newWidth = (uint)(_originalWidth * Percentage / 100.0);
                    uint newHeight = (uint)(_originalHeight * Percentage / 100.0);
                    newSize = new MagickGeometry(newWidth, newHeight)
                    {
                        IgnoreAspectRatio = false
                    };
                    break;

                default:
                    ThrowHelper.ThrowNotSupportedException();
                    return;
            }

            var image = new MagickImage(_file.Path);
            string outputPath = FilePathHelper.GetUniqueOutputPath(_file.Path, "_resized");

            if (image.Format == MagickFormat.Gif)
            {
                image.Dispose();

                using var collection = new MagickImageCollection(_file.Path);
                // This will remove the optimization and change the image to how it looks at that point
                // during the animation. More info here: http://www.imagemagick.org/Usage/anim_basics/#coalesce
                collection.Coalesce();

                // Resize each frame in the GIF
                foreach (IMagickImage<ushort> frame in collection)
                {
                    frame.Resize(newSize);
                }

                collection.Write(outputPath);
            }
            else
            {
                image.Resize(newSize);
                image.Write(outputPath);
                image.Dispose();
            }
        });
    }

    private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        SelectorBarItem selectedItem = _selectorBar.SelectedItem;
        int currentSelectedIndex = _selectorBar.Items.IndexOf(selectedItem);
        Type pageType;

        switch (currentSelectedIndex)
        {
            case 0:
                ResizeMode = ResizeMode.AbsoluteSize;
                pageType = typeof(AbsoluteSizePage);
                break;

            case 1:
                ResizeMode = ResizeMode.Percentage;
                pageType = typeof(PercentagePage);
                break;

            default:
                ThrowHelper.ThrowNotSupportedException();
                return;
        }

        if (_previousSelectedIndex == -1)
        {
            _mainFrame.Navigate(pageType, this, new EntranceNavigationTransitionInfo());
        }
        else
        {
            SlideNavigationTransitionEffect slideNavigationTransitionEffect
                = currentSelectedIndex - _previousSelectedIndex > 0
                ? SlideNavigationTransitionEffect.FromRight
                : SlideNavigationTransitionEffect.FromLeft;

            _mainFrame.Navigate(pageType, this, new SlideNavigationTransitionInfo() { Effect = slideNavigationTransitionEffect });
        }

        _previousSelectedIndex = currentSelectedIndex;
    }
}
