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
    [NotifyPropertyChangedFor(nameof(CpuProgressBarValue))]
    public partial double CpuUsage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MemoryText))]
    public partial double MemoryUsage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GpuText))]
    [NotifyPropertyChangedFor(nameof(GpuProgressBarValue))]
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
    [NotifyPropertyChangedFor(nameof(IsCpuPanelVisible))]
    [NotifyPropertyChangedFor(nameof(CpuProgressBarValue))]
    public partial bool ShowCpu { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGpuPanelVisible))]
    [NotifyPropertyChangedFor(nameof(GpuProgressBarValue))]
    public partial bool ShowGpu { get; set; }

    [ObservableProperty]
    public partial bool ShowRam { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCpuTemperatureVisible))]
    [NotifyPropertyChangedFor(nameof(IsCpuPanelVisible))]
    [NotifyPropertyChangedFor(nameof(CpuProgressBarValue))]
    public partial bool ShowCpuTemperature { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGpuTemperatureVisible))]
    [NotifyPropertyChangedFor(nameof(IsGpuPanelVisible))]
    [NotifyPropertyChangedFor(nameof(GpuProgressBarValue))]
    public partial bool ShowGpuTemperature { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CpuTemperatureText))]
    [NotifyPropertyChangedFor(nameof(IsCpuTemperatureVisible))]
    [NotifyPropertyChangedFor(nameof(IsCpuPanelVisible))]
    [NotifyPropertyChangedFor(nameof(CpuProgressBarValue))]
    public partial double? CpuTemperature { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GpuTemperatureText))]
    [NotifyPropertyChangedFor(nameof(IsGpuTemperatureVisible))]
    [NotifyPropertyChangedFor(nameof(IsGpuPanelVisible))]
    [NotifyPropertyChangedFor(nameof(GpuProgressBarValue))]
    public partial double? GpuTemperature { get; set; }

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
    /// Gets whether the animated running man mode is active.
    /// </summary>
    public bool IsAnimatedGifMode => !IsPercentageMode;

    /// <summary>
    /// Gets whether the CPU panel should be visible (usage or temperature enabled).
    /// </summary>
    public bool IsCpuPanelVisible => ShowCpu || IsCpuTemperatureVisible;

    /// <summary>
    /// Gets whether the GPU panel should be visible (usage or temperature enabled, with data available).
    /// </summary>
    public bool IsGpuPanelVisible =>
        (ShowGpu && GpuUsage.HasValue) || IsGpuTemperatureVisible;

    /// <summary>
    /// Formatted CPU temperature text.
    /// </summary>
    public string CpuTemperatureText => CpuTemperature.HasValue ? $"{CpuTemperature:F0}°C" : "";

    /// <summary>
    /// Formatted GPU temperature text.
    /// </summary>
    public string GpuTemperatureText => GpuTemperature.HasValue ? $"{GpuTemperature:F0}°C" : "";

    /// <summary>
    /// Gets whether the CPU temperature text should be visible.
    /// </summary>
    public bool IsCpuTemperatureVisible => ShowCpuTemperature && CpuTemperature.HasValue;

    /// <summary>
    /// Gets whether the GPU temperature text should be visible.
    /// </summary>
    public bool IsGpuTemperatureVisible => ShowGpuTemperature && GpuTemperature.HasValue;

    /// <summary>
    /// Gets the CPU progress bar value. Shows usage when enabled, otherwise temperature.
    /// </summary>
    public double CpuProgressBarValue => ShowCpu ? CpuUsage : (CpuTemperature ?? 0);

    /// <summary>
    /// Gets the GPU progress bar value. Shows usage when enabled, otherwise temperature.
    /// </summary>
    public double GpuProgressBarValue => ShowGpu ? (GpuUsage ?? 0) : (GpuTemperature ?? 0);

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
            CpuTemperature = e.Data.CpuTemperature;
            GpuTemperature = e.Data.GpuTemperature;

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
             name == Settings.Settings.ShowRam.Name ||
             name == Settings.Settings.ShowCpuTemperature.Name ||
             name == Settings.Settings.ShowGpuTemperature.Name))
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
        ShowCpuTemperature = _settingsProvider.GetSetting(Settings.Settings.ShowCpuTemperature);
        ShowGpuTemperature = _settingsProvider.GetSetting(Settings.Settings.ShowGpuTemperature);
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
