using AnimatedVisuals;

using CommunityToolkit.Diagnostics;

using WindowSill.API;
using WindowSill.PerfCounter.Services;
using WindowSill.PerfCounter.ViewModels;

namespace WindowSill.PerfCounter.Views;

/// <summary>
/// Main view for the performance counter extension.
/// Displays CPU, memory, and GPU usage or an animated running man.
/// </summary>
public sealed partial class PerformanceCounterView : UserControl
{
    private readonly SillView _sillView;

    private PerfCounterPopupViewModel _popupViewModel;
    private SillPopup? _popup;

    /// <summary>
    /// Gets the ViewModel for data binding.
    /// </summary>
    internal PerformanceCounterViewModel ViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceCounterView"/> class.
    /// </summary>
    public PerformanceCounterView(
        SillView sillView,
        IPerformanceMonitorService performanceMonitorService,
        IHardwareInfoService hardwareInfoService,
        PerformanceCounterViewModel viewModel)
    {
        _sillView = sillView;
        ViewModel = viewModel;

        // Create popup ViewModel eagerly so it starts accumulating chart data immediately,
        // not just when the popup is first opened.
        _popupViewModel = new PerfCounterPopupViewModel(performanceMonitorService, hardwareInfoService);

        InitializeComponent();

        UpdateOrientationLayout();
        UpdateThemeAnimation();

        sillView.IsSillOrientationOrSizeChanged += OnIsSillOrientationOrSizeChanged;
        sillView.ActualThemeChanged += OnActualThemeChanged;
    }

    private void MainButton_Click(object sender, RoutedEventArgs e)
    {
        _popup ??= new SillPopup
        {
            Content = new PerfCounterPopupContent(_popupViewModel)
        };

        _popup.ShowAsync(_sillView).ForgetSafely();
    }

    private void OnIsSillOrientationOrSizeChanged(object? sender, EventArgs e)
    {
        UpdateOrientationLayout();
    }

    private void UpdateOrientationLayout()
    {
        MainButton.Padding = new Thickness(4);
        bool showProgressBars = false;

        switch (_sillView.SillOrientationAndSize)
        {
            case SillOrientationAndSize.HorizontalLarge:
                AnimationPlayer.Width = 42;
                showProgressBars = true;
                break;

            case SillOrientationAndSize.HorizontalMedium:
                AnimationPlayer.Width = 28;
                break;

            case SillOrientationAndSize.HorizontalSmall:
                AnimationPlayer.Width = 18;
                MainButton.Padding = new Thickness(0);
                break;

            case SillOrientationAndSize.VerticalLarge:
                AnimationPlayer.Width = 42;
                showProgressBars = true;
                break;

            case SillOrientationAndSize.VerticalMedium:
                AnimationPlayer.Width = 28;
                break;

            case SillOrientationAndSize.VerticalSmall:
                AnimationPlayer.Width = 18;
                break;

            default:
                throw new NotSupportedException($"Unsupported SillOrientationAndSize: {_sillView.SillOrientationAndSize}");
        }

        // In Large mode, show progress bars instead of labels
        Visibility labelVisibility = Visibility.Collapsed;
        CpuLabel.Visibility = labelVisibility;
        MemoryLabel.Visibility = labelVisibility;
        GpuLabel.Visibility = labelVisibility;

        Visibility progressVisibility = showProgressBars ? Visibility.Visible : Visibility.Collapsed;
        CpuProgressBar.Visibility = progressVisibility;
        MemoryProgressBar.Visibility = progressVisibility;
        GpuProgressBar.Visibility = progressVisibility;
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
