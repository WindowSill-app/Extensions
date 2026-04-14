using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

using SkiaSharp;

using WindowSill.API;
using WindowSill.PerfCounter.Services;

namespace WindowSill.PerfCounter.ViewModels;

/// <summary>
/// ViewModel for the performance counter popup, providing rolling chart data
/// and static hardware info for CPU, Memory, and GPU.
/// </summary>
internal sealed partial class PerfCounterPopupViewModel : ObservableObject, IDisposable
{
    private const int MaxDataPoints = 60;

    private readonly IPerformanceMonitorService _performanceMonitorService;

    private readonly ObservableCollection<ObservableValue> _cpuValues = [];
    private readonly ObservableCollection<ObservableValue> _memoryValues = [];
    private readonly ObservableCollection<ObservableValue> _gpuValues = [];

    // Real-time percentage displays
    [ObservableProperty]
    public partial string CpuPercentText { get; set; } = "0%";

    [ObservableProperty]
    public partial string MemoryPercentText { get; set; } = "0%";

    [ObservableProperty]
    public partial string GpuPercentText { get; set; } = "0%";

    [ObservableProperty]
    public partial string CpuTemperatureText { get; set; } = "";

    [ObservableProperty]
    public partial string GpuTemperatureText { get; set; } = "";

    // Real-time memory display (e.g., "12.4 / 32.0 GB")
    [ObservableProperty]
    public partial string MemoryUsageText { get; set; } = "";

    // Static hardware info
    [ObservableProperty]
    public partial string CpuName { get; set; } = "";

    [ObservableProperty]
    public partial string CpuDetailsText { get; set; } = "";

    [ObservableProperty]
    public partial string GpuName { get; set; } = "";

    [ObservableProperty]
    public partial string GpuVramText { get; set; } = "";

    // Visibility flags
    [ObservableProperty]
    public partial bool IsGpuAvailable { get; set; }

    [ObservableProperty]
    public partial bool HasCpuTemperature { get; set; }

    [ObservableProperty]
    public partial bool HasGpuTemperature { get; set; }

    [ObservableProperty]
    public partial bool HasCpuInfo { get; set; }

    [ObservableProperty]
    public partial bool HasGpuInfo { get; set; }

    /// <summary>
    /// Gets the CPU usage chart series.
    /// </summary>
    public ISeries[] CpuSeries { get; }

    /// <summary>
    /// Gets the Memory usage chart series.
    /// </summary>
    public ISeries[] MemorySeries { get; }

    /// <summary>
    /// Gets the GPU usage chart series.
    /// </summary>
    public ISeries[] GpuSeries { get; }

    /// <summary>
    /// Gets the Y axis configuration (0 to auto-max, hidden).
    /// Auto-scaling ensures the gradient fill always spans the visible data range.
    /// </summary>
    public IEnumerable<ICartesianAxis> YAxes { get; } =
    [
        new Axis
        {
            MinLimit = 0,
            IsVisible = false
        }
    ];

    /// <summary>
    /// Gets the X axis configuration (hidden, fixed 60-point window).
    /// </summary>
    public IEnumerable<ICartesianAxis> XAxes { get; } =
    [
        new Axis
        {
            IsVisible = false,
            MinLimit = 0,
            MaxLimit = MaxDataPoints - 1
        }
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="PerfCounterPopupViewModel"/> class.
    /// </summary>
    public PerfCounterPopupViewModel(
        IPerformanceMonitorService performanceMonitorService,
        IHardwareInfoService hardwareInfoService)
    {
        _performanceMonitorService = performanceMonitorService;

        CpuSeries = CreateSeries(_cpuValues, new SKColor(0x60, 0xCD, 0xFF));       // Light blue
        MemorySeries = CreateSeries(_memoryValues, new SKColor(0x8B, 0x5C, 0xF6)); // Purple
        GpuSeries = CreateSeries(_gpuValues, new SKColor(0x34, 0xD3, 0x99));       // Teal/green

        _performanceMonitorService.PerformanceDataUpdated += OnPerformanceDataUpdated;

        LoadHardwareInfoAsync(hardwareInfoService).ForgetSafely();
    }

    /// <summary>
    /// Opens the Windows Task Manager.
    /// </summary>
    [RelayCommand]
    private void OpenTaskManager()
    {
        TaskManagerLauncher.OpenTaskManager();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _performanceMonitorService.PerformanceDataUpdated -= OnPerformanceDataUpdated;
    }

    private async Task LoadHardwareInfoAsync(IHardwareInfoService hardwareInfoService)
    {
        CpuHardwareInfo? cpuInfo = await hardwareInfoService.GetCpuInfoAsync();
        if (cpuInfo is not null)
        {
            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                CpuName = cpuInfo.Name;
                CpuDetailsText = $"{cpuInfo.MaxClockMHz} MHz  •  {cpuInfo.Cores}C / {cpuInfo.Threads}T";
                HasCpuInfo = true;
            });
        }

        GpuHardwareInfo? gpuInfo = await hardwareInfoService.GetGpuInfoAsync();
        if (gpuInfo is not null)
        {
            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                GpuName = gpuInfo.Name;
                if (gpuInfo.TotalVramMB.HasValue)
                {
                    double vramGB = gpuInfo.TotalVramMB.Value / 1024.0;
                    GpuVramText = $"{vramGB:F1} GB VRAM";
                }

                HasGpuInfo = true;
            });
        }

        MemoryHardwareInfo memInfo = hardwareInfoService.GetMemoryInfo();
        // MemoryUsageText is updated on each tick with used/total
    }

    private void OnPerformanceDataUpdated(object? sender, PerformanceDataEventArgs e)
    {
        ThreadHelper.RunOnUIThreadAsync(() =>
        {
            CpuPercentText = $"{e.Data.CpuUsage:F0}%";
            MemoryPercentText = $"{e.Data.MemoryUsage:F0}%";
            AddDataPoint(_cpuValues, e.Data.CpuUsage);
            AddDataPoint(_memoryValues, e.Data.MemoryUsage);

            // Memory used/total display
            double usedGB = e.Data.MemoryUsedMB / 1024.0;
            double totalGB = e.Data.MemoryTotalMB / 1024.0;
            MemoryUsageText = $"{usedGB:F1} / {totalGB:F1} GB";

            if (e.Data.CpuTemperature.HasValue)
            {
                CpuTemperatureText = $"{e.Data.CpuTemperature:F0}°C";
                HasCpuTemperature = true;
            }
            else
            {
                HasCpuTemperature = false;
            }

            if (e.Data.GpuUsage.HasValue)
            {
                GpuPercentText = $"{e.Data.GpuUsage.Value:F0}%";
                AddDataPoint(_gpuValues, e.Data.GpuUsage.Value);
                IsGpuAvailable = true;
            }
            else
            {
                IsGpuAvailable = false;
            }

            if (e.Data.GpuTemperature.HasValue)
            {
                GpuTemperatureText = $"{e.Data.GpuTemperature:F0}°C";
                HasGpuTemperature = true;
            }
            else
            {
                HasGpuTemperature = false;
            }
        });
    }

    private static void AddDataPoint(ObservableCollection<ObservableValue> collection, double value)
    {
        collection.Add(new ObservableValue(value));
        if (collection.Count > MaxDataPoints)
        {
            collection.RemoveAt(0);
        }
    }

    private static ISeries[] CreateSeries(ObservableCollection<ObservableValue> values, SKColor color)
    {
        return
        [
            new LineSeries<ObservableValue>
            {
                Values = values,
                Fill = new LinearGradientPaint(
                    new[] { color.WithAlpha(80), color.WithAlpha(0) },
                    new SKPoint(0, 0),
                    new SKPoint(0, 1)),
                Stroke = new SolidColorPaint(color, 2),
                GeometryFill = null,
                GeometryStroke = null,
                GeometrySize = 0,
                LineSmoothness = 0.1,
                AnimationsSpeed = TimeSpan.FromMilliseconds(150)
            }
        ];
    }
}
