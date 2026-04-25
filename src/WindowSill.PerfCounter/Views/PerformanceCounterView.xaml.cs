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
    private readonly PerfCounterPopupViewModel _popupViewModel;

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
        string stateName = _sillView.SillOrientationAndSize switch
        {
            SillOrientationAndSize.HorizontalLarge => "HorizontalLarge",
            SillOrientationAndSize.HorizontalMedium => "HorizontalMedium",
            SillOrientationAndSize.HorizontalSmall => "HorizontalSmall",
            SillOrientationAndSize.VerticalLarge => "VerticalLarge",
            SillOrientationAndSize.VerticalMedium => "VerticalMedium",
            SillOrientationAndSize.VerticalSmall => "VerticalSmall",
            _ => throw new NotSupportedException($"Unsupported {nameof(SillOrientationAndSize)}: {_sillView.SillOrientationAndSize}")
        };

        VisualStateManager.GoToState(this, stateName, useTransitions: true);
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
