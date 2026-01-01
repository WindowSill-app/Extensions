using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowSill.API;
using WindowSill.PerfCounter.Services;
using WindowSill.PerfCounter.Settings;

namespace WindowSill.PerfCounter.UI;

public partial class PerformanceCounterViewModel : ObservableObject
{
    private readonly IPerformanceMonitorService _performanceMonitorService;
    private readonly ISettingsProvider _settingsProvider;

    [ObservableProperty]
    private double cpuUsage;

    [ObservableProperty]
    private double memoryUsage;

    [ObservableProperty]
    private double? gpuUsage;

    [ObservableProperty]
    private long memoryUsedMB;

    [ObservableProperty]
    private long memoryTotalMB;

    [ObservableProperty]
    private bool isPercentageMode = true;

    [ObservableProperty]
    private double animationSpeed = 1.0;

    public string CpuText => $"{CpuUsage:F0}%";

    public string MemoryText => $"{MemoryUsage:F0}%";

    public string GpuText => $"{GpuUsage:F0}%";

    public PerformanceCounterViewModel(
        IPerformanceMonitorService performanceMonitorService,
        ISettingsProvider settingsProvider)
    {
        _performanceMonitorService = performanceMonitorService;
        _settingsProvider = settingsProvider;

        _performanceMonitorService.PerformanceDataUpdated += OnPerformanceDataUpdated;
        _settingsProvider.SettingChanged += OnSettingChanged;

        UpdateDisplayMode();
    }

    [RelayCommand]
    private void OpenTaskManager()
    {
        TaskManagerLauncher.OpenTaskManager(_settingsProvider);
    }

    public static (PerformanceCounterViewModel viewModel, SillView view) CreateView(
        IPerformanceMonitorService performanceMonitorService,
        ISettingsProvider settingsProvider,
        IPluginInfo pluginInfo)
    {
        var viewModel = new PerformanceCounterViewModel(performanceMonitorService, settingsProvider);

        var view = new SillView();
        view.Content = new PerformanceCounterView(view, pluginInfo, viewModel, settingsProvider);

        return (viewModel, view);
    }

    private void OnPerformanceDataUpdated(object? sender, PerformanceDataEventArgs e)
    {
        ThreadHelper.RunOnUIThreadAsync(() =>
        {
            CpuUsage = e.Data.CpuUsage;
            MemoryUsage = e.Data.MemoryUsage;
            GpuUsage = e.Data.GpuUsage;

            UpdateAnimationSpeed();

            // Notify computed properties changed
            OnPropertyChanged(nameof(CpuText));
            OnPropertyChanged(nameof(MemoryText));
            OnPropertyChanged(nameof(GpuText));
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
    }

    private void UpdateDisplayMode()
    {
        PerformanceDisplayMode displayMode = _settingsProvider.GetSetting(Settings.Settings.DisplayMode);
        IsPercentageMode = displayMode == PerformanceDisplayMode.Percentage;
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