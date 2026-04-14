using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WindowSill.API;
using WindowSill.PerfCounter.Services;
using WindowSill.PerfCounter.Settings;

namespace WindowSill.PerfCounter.ViewModels;

/// <summary>
/// ViewModel for the performance counter main view.
/// Exposes CPU, memory, and GPU usage data with display mode switching.
/// </summary>
public partial class PerformanceCounterViewModel : ObservableObject, IDisposable
{
    private readonly IPerformanceMonitorService _performanceMonitorService;
    private readonly ISettingsProvider _settingsProvider;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CpuText))]
    public partial double CpuUsage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MemoryText))]
    public partial double MemoryUsage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GpuText))]
    [NotifyPropertyChangedFor(nameof(GpuUsageValue))]
    [NotifyPropertyChangedFor(nameof(IsGpuPanelVisible))]
    public partial double? GpuUsage { get; set; }

    [ObservableProperty]
    public partial long MemoryUsedMB { get; set; }

    [ObservableProperty]
    public partial long MemoryTotalMB { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnimatedGifMode))]
    public partial bool IsPercentageMode { get; set; }

    [ObservableProperty]
    public partial bool ShowCpu { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGpuPanelVisible))]
    public partial bool ShowGpu { get; set; }

    [ObservableProperty]
    public partial bool ShowRam { get; set; }

    [ObservableProperty]
    public partial double AnimationSpeed { get; set; }

    /// <summary>
    /// Formatted CPU usage text.
    /// </summary>
    public string CpuText => $"{CpuUsage:F0}%";

    /// <summary>
    /// Formatted memory usage text.
    /// </summary>
    public string MemoryText => $"{MemoryUsage:F0}%";

    /// <summary>
    /// Formatted GPU usage text.
    /// </summary>
    public string GpuText => $"{GpuUsage:F0}%";

    /// <summary>
    /// Gets the GPU usage as a non-nullable value for binding to ProgressBar.
    /// </summary>
    public double GpuUsageValue => GpuUsage ?? 0;

    /// <summary>
    /// Gets whether the animated running man mode is active.
    /// </summary>
    public bool IsAnimatedGifMode => !IsPercentageMode;

    /// <summary>
    /// Gets whether the GPU panel should be visible.
    /// </summary>
    public bool IsGpuPanelVisible => ShowGpu && GpuUsage.HasValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceCounterViewModel"/> class.
    /// </summary>
    /// <param name="performanceMonitorService">The performance data provider.</param>
    /// <param name="settingsProvider">The settings provider for display preferences.</param>
    public PerformanceCounterViewModel(
        IPerformanceMonitorService performanceMonitorService,
        ISettingsProvider settingsProvider)
    {
        _performanceMonitorService = performanceMonitorService;
        _settingsProvider = settingsProvider;

        IsPercentageMode = true;
        ShowCpu = true;
        ShowGpu = true;
        ShowRam = true;
        AnimationSpeed = 1.0;

        _performanceMonitorService.PerformanceDataUpdated += OnPerformanceDataUpdated;
        _settingsProvider.SettingChanged += OnSettingChanged;

        UpdateDisplayMode();
        UpdateMetricVisibility();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _performanceMonitorService.PerformanceDataUpdated -= OnPerformanceDataUpdated;
        _settingsProvider.SettingChanged -= OnSettingChanged;
    }

    private void OnPerformanceDataUpdated(object? sender, PerformanceDataEventArgs e)
    {
        ThreadHelper.RunOnUIThreadAsync(() =>
        {
            CpuUsage = e.Data.CpuUsage;
            MemoryUsage = e.Data.MemoryUsage;
            GpuUsage = e.Data.GpuUsage;

            UpdateAnimationSpeed();
        });
    }

    private void OnSettingChanged(ISettingsProvider sender, SettingChangedEventArgs args)
    {
        if (args.SettingName == Settings.Settings.DisplayMode.Name ||
            args.SettingName == Settings.Settings.AnimationMetric.Name)
        {
            UpdateDisplayMode();
            UpdateAnimationSpeed();
        }
        else if (args.SettingName is { } name &&
            (name == Settings.Settings.ShowCpu.Name ||
             name == Settings.Settings.ShowGpu.Name ||
             name == Settings.Settings.ShowRam.Name))
        {
            UpdateMetricVisibility();
        }
    }

    private void UpdateDisplayMode()
    {
        PerformanceDisplayMode displayMode = _settingsProvider.GetSetting(Settings.Settings.DisplayMode);
        IsPercentageMode = displayMode == PerformanceDisplayMode.Percentage;
    }

    private void UpdateMetricVisibility()
    {
        ShowCpu = _settingsProvider.GetSetting(Settings.Settings.ShowCpu);
        ShowGpu = _settingsProvider.GetSetting(Settings.Settings.ShowGpu);
        ShowRam = _settingsProvider.GetSetting(Settings.Settings.ShowRam);
    }

    private void UpdateAnimationSpeed()
    {
        if (!IsPercentageMode)
        {
            PerformanceMetric animationMetric = _settingsProvider.GetSetting(Settings.Settings.AnimationMetric);
            double? metricValue = animationMetric switch
            {
                PerformanceMetric.CPU => CpuUsage,
                PerformanceMetric.GPU => GpuUsage,
                PerformanceMetric.RAM => MemoryUsage,
                _ => CpuUsage
            };

            // Convert 0-100% to animation speed (0.1x to 1.5x)
            AnimationSpeed = Math.Max(0.1, Math.Min(1.5, (metricValue.GetValueOrDefault(0) / 100.0) * 1.4 + 0.1));
        }
    }
}
