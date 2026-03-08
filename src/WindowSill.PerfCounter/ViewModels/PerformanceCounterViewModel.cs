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
public partial class PerformanceCounterViewModel : ObservableObject
{
    private readonly IPerformanceMonitorService _performanceMonitorService;
    private readonly ISettingsProvider _settingsProvider;

    [ObservableProperty]
    public partial double CpuUsage { get; set; }

    [ObservableProperty]
    public partial double MemoryUsage { get; set; }

    [ObservableProperty]
    public partial double? GpuUsage { get; set; }

    [ObservableProperty]
    public partial double? CpuTemperature { get; set; }

    [ObservableProperty]
    public partial double? GpuTemperature { get; set; }

    [ObservableProperty]
    public partial long MemoryUsedMB { get; set; }

    [ObservableProperty]
    public partial long MemoryTotalMB { get; set; }

    [ObservableProperty]
    public partial bool IsPercentageMode { get; set; }

    [ObservableProperty]
    public partial bool ShowTemperature { get; set; }

    [ObservableProperty]
    public partial bool ShowCpu { get; set; }

    [ObservableProperty]
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
    /// Formatted CPU temperature text.
    /// </summary>
    public string CpuTemperatureText => CpuTemperature.HasValue ? $"{CpuTemperature:F0}°C" : "";

    /// <summary>
    /// Formatted GPU temperature text.
    /// </summary>
    public string GpuTemperatureText => GpuTemperature.HasValue ? $"{GpuTemperature:F0}°C" : "";

    /// <summary>
    /// Gets whether the animated running man mode is active.
    /// </summary>
    public bool IsAnimatedGifMode => !IsPercentageMode;

    /// <summary>
    /// Gets whether the GPU panel should be visible.
    /// </summary>
    public bool IsGpuPanelVisible => ShowGpu && GpuUsage.HasValue;

    /// <summary>
    /// Gets whether CPU temperature should be visible.
    /// </summary>
    public bool IsCpuTemperatureVisible => ShowTemperature && CpuTemperature.HasValue;

    /// <summary>
    /// Gets whether GPU temperature should be visible.
    /// </summary>
    public bool IsGpuTemperatureVisible => ShowTemperature && GpuTemperature.HasValue;

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
        ShowTemperature = true;
        ShowCpu = true;
        ShowGpu = true;
        ShowRam = true;
        AnimationSpeed = 1.0;

        _performanceMonitorService.PerformanceDataUpdated += OnPerformanceDataUpdated;
        _settingsProvider.SettingChanged += OnSettingChanged;

        UpdateDisplayMode();
        UpdateShowTemperature();
        UpdateMetricVisibility();
    }

    /// <summary>
    /// Opens the Windows Task Manager.
    /// </summary>
    [RelayCommand]
    private void OpenTaskManager()
    {
        TaskManagerLauncher.OpenTaskManager(_settingsProvider);
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

            OnPropertyChanged(nameof(CpuText));
            OnPropertyChanged(nameof(MemoryText));
            OnPropertyChanged(nameof(GpuText));
            OnPropertyChanged(nameof(CpuTemperatureText));
            OnPropertyChanged(nameof(GpuTemperatureText));
            OnPropertyChanged(nameof(IsGpuPanelVisible));
            OnPropertyChanged(nameof(IsCpuTemperatureVisible));
            OnPropertyChanged(nameof(IsGpuTemperatureVisible));
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
        else if (args.SettingName == Settings.Settings.ShowTemperature.Name)
        {
            UpdateShowTemperature();
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
        OnPropertyChanged(nameof(IsAnimatedGifMode));
    }

    private void UpdateShowTemperature()
    {
        ShowTemperature = _settingsProvider.GetSetting(Settings.Settings.ShowTemperature);
        OnPropertyChanged(nameof(IsCpuTemperatureVisible));
        OnPropertyChanged(nameof(IsGpuTemperatureVisible));
    }

    private void UpdateMetricVisibility()
    {
        ShowCpu = _settingsProvider.GetSetting(Settings.Settings.ShowCpu);
        ShowGpu = _settingsProvider.GetSetting(Settings.Settings.ShowGpu);
        ShowRam = _settingsProvider.GetSetting(Settings.Settings.ShowRam);
        OnPropertyChanged(nameof(IsGpuPanelVisible));
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
