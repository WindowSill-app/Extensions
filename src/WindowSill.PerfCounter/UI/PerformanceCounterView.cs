using AnimatedVisuals;

using CommunityToolkit.Diagnostics;

using Microsoft.UI.Xaml.Media.Imaging;

using WindowSill.API;

namespace WindowSill.PerfCounter.UI;

public sealed class PerformanceCounterView : Button
{
    private readonly SillView _sillView;
    private readonly IPluginInfo _pluginInfo;
    private readonly ISettingsProvider _settingsProvider;
    private readonly AnimatedVisualPlayer _animatedVisualPlayer = new();
    private readonly ImageIcon _cpuIcon = new();
    private readonly ImageIcon _memoryIcon = new();
    private readonly ImageIcon _gpuIcon = new();

    public PerformanceCounterView(SillView sillView, IPluginInfo pluginInfo, PerformanceCounterViewModel viewModel, ISettingsProvider settingsProvider)
    {
        _sillView = sillView;
        _pluginInfo = pluginInfo;
        _settingsProvider = settingsProvider;

        this.DataContext(
            viewModel,
            (view, vm) => view
            .Style(x => x.StaticResource("SillButtonStyle"))
            .Height(double.NaN)
            .Content(
                new Grid()
                    .Children(
                        new SillOrientedStackPanel()
                            .VerticalAlignment(VerticalAlignment.Center)
                            .Spacing(8)
                            .Visibility(x => x.Binding(() => vm.IsPercentageMode)
                                              .OneWay()
                                              .Convert(isPercentage => isPercentage ? Visibility.Visible : Visibility.Collapsed))
                            .Children(
                                new StackPanel()
                                    .VerticalAlignment(VerticalAlignment.Center)
                                    .Orientation(Orientation.Horizontal)
                                    .Spacing(4)
                                    .Children(
                                        _cpuIcon
                                            .Source(new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "microchip.svg")))),
                                        new TextBlock()
                                            .MinWidth(32)
                                            .VerticalAlignment(VerticalAlignment.Center)
                                            .Text(x => x.Binding(() => vm.CpuText).OneWay())
                                    ),
                                new StackPanel()
                                    .VerticalAlignment(VerticalAlignment.Center)
                                    .Orientation(Orientation.Horizontal)
                                    .Spacing(4)
                                    .Children(
                                        _memoryIcon
                                            .Source(new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "memory_slot.svg")))),
                                        new TextBlock()
                                            .MinWidth(32)
                                            .VerticalAlignment(VerticalAlignment.Center)
                                            .Text(x => x.Binding(() => vm.MemoryText).OneWay())
                                    ),
                                new StackPanel()
                                    .VerticalAlignment(VerticalAlignment.Center)
                                    .Orientation(Orientation.Horizontal)
                                    .Spacing(4)
                                    .Visibility(x => x.Binding(() => vm.GpuUsage)
                                                      .OneWay()
                                                      .Convert(gpuUsage => gpuUsage.HasValue ? Visibility.Visible : Visibility.Collapsed))
                                    .Children(
                                        _gpuIcon
                                            .Source(new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "video_card.svg")))),
                                        new TextBlock()
                                            .MinWidth(32)
                                            .VerticalAlignment(VerticalAlignment.Center)
                                            .Text(x => x.Binding(() => vm.GpuText).OneWay())
                                    )
                            ),

                        _animatedVisualPlayer
                            .Width(24)
                            .Visibility(x => x.Binding(() => vm.IsPercentageMode)
                                              .OneWay()
                                              .Convert(isPercentage => isPercentage ? Visibility.Collapsed : Visibility.Visible))
                            .PlaybackRate(x => x.Binding(() => vm.AnimationSpeed)
                                                .OneWay())
                    )
            )
        );

        OnIsSillOrientationOrSizeChanged(null, EventArgs.Empty);
        OnActualThemeChanged(_sillView, EventArgs.Empty);
        sillView.IsSillOrientationOrSizeChanged += OnIsSillOrientationOrSizeChanged;
        sillView.ActualThemeChanged += OnActualThemeChanged;
        Click += PerformanceCounterView_Click;
    }

    private void PerformanceCounterView_Click(object sender, RoutedEventArgs e)
    {
        TaskManagerLauncher.OpenTaskManager(_settingsProvider);
    }

    private void OnIsSillOrientationOrSizeChanged(object? sender, EventArgs e)
    {
        this.Padding(4);

        _cpuIcon.Height(x => x.ThemeResource("SillIconSize"));
        _cpuIcon.MaxWidth(x => x.ThemeResource("SillIconSize"));
        _memoryIcon.Height(x => x.ThemeResource("SillIconSize"));
        _memoryIcon.MaxWidth(x => x.ThemeResource("SillIconSize"));
        _gpuIcon.Height(x => x.ThemeResource("SillIconSize"));
        _gpuIcon.MaxWidth(x => x.ThemeResource("SillIconSize"));

        switch (_sillView.SillOrientationAndSize)
        {
            case SillOrientationAndSize.HorizontalLarge:
                _animatedVisualPlayer.Width(42);
                break;

            case SillOrientationAndSize.HorizontalMedium:
                _animatedVisualPlayer.Width(28);
                break;

            case SillOrientationAndSize.HorizontalSmall:
                _animatedVisualPlayer.Width(18);
                this.Padding(0);
                break;

            case SillOrientationAndSize.VerticalLarge:
            case SillOrientationAndSize.VerticalMedium:
            case SillOrientationAndSize.VerticalSmall:
                _animatedVisualPlayer.Width(42);
                break;

            default:
                throw new NotSupportedException($"Unsupported SillOrientationAndSize: {_sillView.SillOrientationAndSize}");
        }
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        switch (_sillView.ActualTheme)
        {
            case ElementTheme.Light:
                _animatedVisualPlayer.Source(new Running_person_light_theme());
                break;

            case ElementTheme.Dark:
                _animatedVisualPlayer.Source(new Running_person_dark_theme());
                break;

            default:
                ThrowHelper.ThrowInvalidOperationException();
                break;
        }
    }
}
