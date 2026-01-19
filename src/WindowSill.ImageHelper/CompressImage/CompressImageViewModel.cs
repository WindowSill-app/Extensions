using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Converters;

using Microsoft.Extensions.Logging;

using Windows.Storage;

using WindowSill.API;

namespace WindowSill.ImageHelper.CompressImage;

internal sealed partial class CompressImageViewModel : ObservableObject
{
    private readonly IReadOnlyList<IStorageFile> _files;
    private readonly SillPopupContent _view;
    private readonly ShortcutButton _cancelButton = new();
    private readonly FileSizeToFriendlyStringConverter _fileSizeToFriendlyStringConverter = new();
    private readonly BoolToVisibilityConverter _trueToCollapsedConverter = new()
    {
        TrueValue = Visibility.Collapsed,
        FalseValue = Visibility.Visible
    };

    private CancellationTokenSource _cancellationTokenSource = new();

    private CompressImageViewModel(IReadOnlyList<IStorageFile> files)
    {
        _files = files;
        _view
            = new SillPopupContent(OnOpening, OnClosing)
                .Width(350)
                .Height(400)
                .DataContext(
                    this,
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
                                    .Padding(24)
                                    .Content(
                                        new ItemsControl()
                                            .ItemsSource(() => viewModel.CompressionTasks)
                                            .ItemTemplate<CompressionTask>(item =>
                                                new Grid()
                                                    .CornerRadius(x => x.ThemeResource("ControlCornerRadius"))
                                                    .Background(x => x.ThemeResource("CardBackgroundFillColorDefaultBrush"))
                                                    .Height(64)
                                                    .Padding(16)
                                                    .Margin(0, 0, 0, 8)
                                                    .RowDefinitions(
                                                        new GridLength(1, GridUnitType.Star),
                                                        new GridLength(1, GridUnitType.Star)
                                                    )
                                                    .ColumnDefinitions(
                                                        new GridLength(1, GridUnitType.Star),
                                                        new GridLength(1, GridUnitType.Star)
                                                    )
                                                    .Children(
                                                        new TextBlock()
                                                            .Grid(row: 0, column: 0)
                                                            .Style(x => x.ThemeResource("BodyStrongTextBlockStyle"))
                                                            .VerticalAlignment(VerticalAlignment.Bottom)
                                                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                                                            .Text(x => x.Binding(() => item.FileName)
                                                                        .OneTime())
                                                            .TextTrimming(TextTrimming.CharacterEllipsis)
                                                            .TextWrapping(TextWrapping.NoWrap),

                                                        new TextBlock()
                                                            .Grid(row: 1, column: 0)
                                                            .Style(x => x.ThemeResource("CaptionTextBlockStyle"))
                                                            .Foreground(x => x.ThemeResource("TextFillColorSecondaryBrush"))
                                                            .VerticalAlignment(VerticalAlignment.Top)
                                                            .Text(x => x.Binding(() => item.ByteLengthBeforeCompression)
                                                                        .Converter(_fileSizeToFriendlyStringConverter)),

                                                        new TextBlock()
                                                            .Grid(row: 0, column: 1)
                                                            .Style(x => x.ThemeResource("BodyStrongTextBlockStyle"))
                                                            .HorizontalAlignment(HorizontalAlignment.Right)
                                                            .VerticalAlignment(VerticalAlignment.Bottom)
                                                            .Text(x => x.Binding(() => item.CompressionPercentage)
                                                                        .OneWay())
                                                            .Visibility(x => x.Binding(() => item.IsRunning)
                                                                              .OneWay()
                                                                              .Converter(_trueToCollapsedConverter)),

                                                        new TextBlock()
                                                            .Grid(row: 1, column: 1)
                                                            .Style(x => x.ThemeResource("CaptionTextBlockStyle"))
                                                            .Foreground(x => x.ThemeResource("TextFillColorSecondaryBrush"))
                                                            .HorizontalAlignment(HorizontalAlignment.Right)
                                                            .VerticalAlignment(VerticalAlignment.Top)
                                                            .Text(x => x.Binding(() => item.ByteLengthAfterCompression)
                                                                        .OneWay()
                                                                        .Converter(_fileSizeToFriendlyStringConverter))
                                                            .Visibility(x => x.Binding(() => item.IsRunning)
                                                                              .OneWay()
                                                                              .Converter(_trueToCollapsedConverter)),

                                                        new ProgressRing()
                                                            .Grid(row: 0, column: 1, rowSpan: 2)
                                                            .Value(0)
                                                            .IsIndeterminate(x => x.Binding(() => item.IsRunning)
                                                                                   .OneWay())
                                                            .HorizontalAlignment(HorizontalAlignment.Right)
                                                    )
                                            )
                                    ),

                                new Border()
                                    .Name("CommandSpace")
                                    .Grid(row: 1)
                                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                                    .VerticalAlignment(VerticalAlignment.Bottom)
                                    .XYFocusKeyboardNavigation(XYFocusKeyboardNavigationMode.Enabled)
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
                                                _cancelButton
                                                    .Grid(column: 1)
                                                    .Style(x => x.ThemeResource("LargeShortcutButtonStyle"))
                                                    .ElementSoundMode(ElementSoundMode.FocusOnly)
                                                    .Command(() => viewModel.CancelCommand)
                                            )
                                    )
                            )
                    )
                );
    }

    internal static SillPopupContent CreateView(IReadOnlyList<IStorageFile> files)
    {
        var viewModel = new CompressImageViewModel(files);
        return viewModel._view;
    }

    public ObservableCollection<CompressionTask> CompressionTasks { get; } = new();

    [RelayCommand]
    private void Cancel()
    {
        _view.Close();
    }

    private void OnOpening()
    {
        _cancelButton.Content = "/WindowSill.ImageHelper/CompressImage/Cancel".GetLocalizedString();

        CompressionTasks.Clear();
        for (int i = 0; i < _files.Count; i++)
        {
            CompressionTasks.Add(new CompressionTask(_files[i]));
        }

        RunLosslessCompressionAsync(_cancellationTokenSource.Token).Forget();
    }

    private void OnClosing()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    private async Task RunLosslessCompressionAsync(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            try
            {
                CompressionTask[] compressionTasks = CompressionTasks.ToArray(); // Copy to array to avoid modification during iteration
                for (int i = 0; i < compressionTasks.Length; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    CompressionTask compressionTask = compressionTasks[i];
                    await compressionTask.LosslessCompressAsync();
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException)
            {
                // Handle cancellation gracefully
            }
            catch (Exception ex)
            {
                this.Log().LogError(ex, "Error while doing lossless image compression.");
            }
        }, cancellationToken);

        _cancelButton.Content = "/WindowSill.ImageHelper/CompressImage/Done".GetLocalizedString();
    }
}
