using CommunityToolkit.Diagnostics;

using WindowSill.API;

namespace WindowSill.ImageHelper.ConvertImage;

internal sealed class ConversionPage : XamlLessPage
{
    public ConversionPage()
    {
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
                            .Padding(24)
                            .Content(
                                new ItemsControl()
                                    .ItemsSource(() => viewModel.ConversionTasks)
                                    .ItemTemplate<ConversionTask>(item =>
                                        new Grid()
                                            .CornerRadius(x => x.ThemeResource("ControlCornerRadius"))
                                            .Background(x => x.ThemeResource("CardBackgroundFillColorDefaultBrush"))
                                            .Height(64)
                                            .Padding(16)
                                            .Margin(0, 0, 0, 8)
                                            .ColumnDefinitions(
                                                new GridLength(1, GridUnitType.Star),
                                                GridLength.Auto
                                            )
                                            .Children(
                                                new TextBlock()
                                                    .Grid(column: 0)
                                                    .Style(x => x.ThemeResource("BodyStrongTextBlockStyle"))
                                                    .VerticalAlignment(VerticalAlignment.Center)
                                                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                                                    .Text(x => x.Binding(() => item.FileName).OneTime())
                                                    .TextTrimming(TextTrimming.CharacterEllipsis)
                                                    .TextWrapping(TextWrapping.NoWrap),

                                                new ProgressRing()
                                                    .Grid(column: 1)
                                                    .Value(0)
                                                    .IsIndeterminate(x => x.Binding(() => item.IsRunning).OneWay())
                                                    .HorizontalAlignment(HorizontalAlignment.Right)
                                                    .VerticalAlignment(VerticalAlignment.Center),

                                                new InfoBadge()
                                                    .Grid(column: 1)
                                                    .Background(x => x.ThemeResource("SystemFillColorSuccessBrush"))
                                                    .HorizontalAlignment(HorizontalAlignment.Right)
                                                    .VerticalAlignment(VerticalAlignment.Center)
                                                    .Visibility(x => x.Binding(() => item.IsSucceeded).OneWay())
                                                    .IconSource(new FontIconSource()
                                                        .FontFamily("Segoe Fluent Icons,Segoe MDL2 Assets")
                                                        .Glyph("\uE73E")),

                                                new InfoBadge()
                                                    .Grid(column: 1)
                                                    .Background(x => x.ThemeResource("SystemFillColorCriticalBrush"))
                                                    .HorizontalAlignment(HorizontalAlignment.Right)
                                                    .VerticalAlignment(VerticalAlignment.Center)
                                                    .Visibility(x => x.Binding(() => item.IsFailed).OneWay())
                                                    .IconSource(new FontIconSource()
                                                        .FontFamily("Segoe Fluent Icons,Segoe MDL2 Assets")
                                                        .Glyph("\uE711"))
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
                                        new Button()
                                            .Grid(column: 1)
                                            .Style(x => x.ThemeResource("LargeButtonStyle"))
                                            .ElementSoundMode(ElementSoundMode.FocusOnly)
                                            .Content(x => x.Binding(() => viewModel.CancelButtonText).OneWay())
                                            .Command(() => viewModel.CancelCommand)
                                    )
                            )
                    )
            ));
    }

    internal ConvertImageViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(XamlLessNavigationEventArgs e)
    {
        Guard.IsNotNull(e.Parameter);
        Guard.IsOfType<ConvertImageViewModel>(e.Parameter);
        ViewModel = (ConvertImageViewModel)e.Parameter;

        ViewModel.CancelButtonText = "/WindowSill.ImageHelper/ConvertImage/Cancel".GetLocalizedString();

        ViewModel.ConversionTasks.Clear();
        for (int i = 0; i < ViewModel.Files.Count; i++)
        {
            ViewModel.ConversionTasks.Add(new ConversionTask(ViewModel.Files[i], ViewModel.SelectedFormat));
        }

        RunConversionAsync(ViewModel.CancellationToken).Forget();
    }

    private async Task RunConversionAsync(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            try
            {
                for (int i = 0; i < ViewModel.ConversionTasks.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    await ViewModel.ConversionTasks[i].ConvertAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // TODO: handle exceptions, including cancellation.
            }
        }, cancellationToken);

        ViewModel.CancelButtonText = "/WindowSill.ImageHelper/ConvertImage/Done".GetLocalizedString();
    }
}
