using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ImageMagick;

using Microsoft.UI.Xaml.Media.Animation;

using Windows.Storage;

using WindowSill.API;

namespace WindowSill.ImageHelper.ConvertImage;

internal sealed partial class ConvertImageViewModel : ObservableObject
{
    private readonly SillPopupContent _view;
    private readonly XamlLessFrame _mainFrame = new();

    private CancellationTokenSource _cancellationTokenSource = new();

    private ConvertImageViewModel(IReadOnlyList<IStorageFile> files)
    {
        CancellationToken = _cancellationTokenSource.Token;
        Files = files;
        _view
            = new SillPopupContent(OnOpening, OnClosing)
                .Width(350)
                .Height(400)
                .DataContext(
                    this,
                    (view, viewModel) => view
                    .Content(
                        _mainFrame
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .HorizontalContentAlignment(HorizontalAlignment.Stretch)
                            .VerticalAlignment(VerticalAlignment.Stretch)
                            .VerticalContentAlignment(VerticalAlignment.Stretch)
                            .IsNavigationStackEnabled(false)
                    )
                );
    }

    internal static SillPopupContent CreateView(IReadOnlyList<IStorageFile> files)
    {
        var viewModel = new ConvertImageViewModel(files);
        return viewModel._view;
    }

    public ObservableCollection<ConversionTask> ConversionTasks { get; } = new();

    internal IReadOnlyList<IStorageFile> Files { get; }

    internal MagickFormat SelectedFormat { get; set; }

    internal CancellationToken CancellationToken { get; set; }

    [ObservableProperty]
    public partial string CancelButtonText { get; set; } = "/WindowSill.ImageHelper/ConvertImage/Cancel".GetLocalizedString();

    [RelayCommand]
    private void Convert(MagickFormat format)
    {
        SelectedFormat = format;

        _mainFrame.Navigate(
            typeof(ConversionPage),
            this,
            new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
    }

    [RelayCommand]
    private void Cancel()
    {
        _view.Close();
    }

    private void OnOpening()
    {
        _mainFrame.Navigate(typeof(MainPage), this);
    }

    private void OnClosing()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        CancellationToken = _cancellationTokenSource.Token;
    }
}
