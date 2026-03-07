using AnimatedVisuals;

using CommunityToolkit.Diagnostics;

using Microsoft.UI.Xaml.Media.Imaging;

using WindowSill.API;
using WindowSill.PerfCounter.ViewModels;

namespace WindowSill.PerfCounter.Views;

/// <summary>
/// Main view for the performance counter extension.
/// Displays CPU, memory, and GPU usage or an animated running man.
/// </summary>
public sealed partial class PerformanceCounterView : UserControl
{
    private readonly SillView _sillView;
    private readonly IPluginInfo _pluginInfo;

    /// <summary>
    /// Gets the ViewModel for data binding.
    /// </summary>
    internal PerformanceCounterViewModel ViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceCounterView"/> class.
    /// </summary>
    /// <param name="sillView">The parent SillView for orientation and theme tracking.</param>
    /// <param name="pluginInfo">Plugin info for resolving asset paths.</param>
    /// <param name="viewModel">The ViewModel providing performance data.</param>
    public PerformanceCounterView(
        SillView sillView,
        IPluginInfo pluginInfo,
        PerformanceCounterViewModel viewModel)
    {
        _sillView = sillView;
        _pluginInfo = pluginInfo;
        ViewModel = viewModel;

        InitializeComponent();

        LoadIcons();
        UpdateAnimationVisibility();
        UpdateTemperatureVisibility();
        UpdateOrientationLayout();
        UpdateThemeAnimation();

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        sillView.IsSillOrientationOrSizeChanged += OnIsSillOrientationOrSizeChanged;
        sillView.ActualThemeChanged += OnActualThemeChanged;
    }

    private void LoadIcons()
    {
        string assetsDir = _pluginInfo.GetPluginContentDirectory();
        CpuIcon.Source = new SvgImageSource(new Uri(System.IO.Path.Combine(assetsDir, "Assets", "microchip.svg")));
        MemoryIcon.Source = new SvgImageSource(new Uri(System.IO.Path.Combine(assetsDir, "Assets", "memory_slot.svg")));
        GpuIcon.Source = new SvgImageSource(new Uri(System.IO.Path.Combine(assetsDir, "Assets", "video_card.svg")));
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PerformanceCounterViewModel.GpuUsage))
        {
            GpuPanel.Visibility = ViewModel.GpuUsage.HasValue ? Visibility.Visible : Visibility.Collapsed;
        }
        else if (e.PropertyName == nameof(PerformanceCounterViewModel.IsPercentageMode))
        {
            UpdateAnimationVisibility();
        }
        else if (e.PropertyName is nameof(PerformanceCounterViewModel.CpuTemperature)
            or nameof(PerformanceCounterViewModel.GpuTemperature)
            or nameof(PerformanceCounterViewModel.ShowTemperature))
        {
            UpdateTemperatureVisibility();
        }
    }

    private void UpdateAnimationVisibility()
    {
        AnimationPlayer.Visibility = ViewModel.IsPercentageMode ? Visibility.Collapsed : Visibility.Visible;
        PercentagePanel.Visibility = ViewModel.IsPercentageMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateTemperatureVisibility()
    {
        bool showCpuTemp = ViewModel.ShowTemperature && ViewModel.CpuTemperature.HasValue;
        bool showGpuTemp = ViewModel.ShowTemperature && ViewModel.GpuTemperature.HasValue;

        CpuTemperatureText.Visibility = showCpuTemp ? Visibility.Visible : Visibility.Collapsed;
        GpuTemperatureText.Visibility = showGpuTemp ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnIsSillOrientationOrSizeChanged(object? sender, EventArgs e)
    {
        UpdateOrientationLayout();
    }

    private void UpdateOrientationLayout()
    {
        MainButton.Padding = new Thickness(4);

        switch (_sillView.SillOrientationAndSize)
        {
            case SillOrientationAndSize.HorizontalLarge:
                AnimationPlayer.Width = 42;
                break;

            case SillOrientationAndSize.HorizontalMedium:
                AnimationPlayer.Width = 28;
                break;

            case SillOrientationAndSize.HorizontalSmall:
                AnimationPlayer.Width = 18;
                MainButton.Padding = new Thickness(0);
                break;

            case SillOrientationAndSize.VerticalLarge:
            case SillOrientationAndSize.VerticalMedium:
            case SillOrientationAndSize.VerticalSmall:
                AnimationPlayer.Width = 42;
                break;

            default:
                throw new NotSupportedException($"Unsupported SillOrientationAndSize: {_sillView.SillOrientationAndSize}");
        }
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        UpdateThemeAnimation();
    }

    private void UpdateThemeAnimation()
    {
        switch (_sillView.ActualTheme)
        {
            case ElementTheme.Light:
                AnimationPlayer.Source = new Running_person_light_theme();
                break;

            case ElementTheme.Dark:
                AnimationPlayer.Source = new Running_person_dark_theme();
                break;

            default:
                ThrowHelper.ThrowInvalidOperationException();
                break;
        }
    }
}
