using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Aggregates CPU, memory, GPU, and temperature data on a 1-second timer.
/// </summary>
[Export(typeof(IPerformanceMonitorService))]
internal sealed class PerformanceMonitorService : IPerformanceMonitorService, IDisposable
{
    private readonly Timer _timer;
    private readonly IGpuMonitorService _gpuMonitor;
    private readonly ITemperatureMonitorService _temperatureMonitor;
    private ulong _lastIdleTime;
    private ulong _lastKernelTime;
    private ulong _lastUserTime;
    private readonly object _lockObject = new();
    private int _monitoringCount;
    private int _callbackRunning;

    public event EventHandler<PerformanceDataEventArgs>? PerformanceDataUpdated;

    [ImportingConstructor]
    public PerformanceMonitorService(
        IGpuMonitorService gpuMonitor,
        ITemperatureMonitorService temperatureMonitor)
    {
        _timer = new Timer(OnTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        _gpuMonitor = gpuMonitor;
        _temperatureMonitor = temperatureMonitor;
        InitializeCpuUsageTracking();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        StopMonitoring();
        _timer?.Dispose();
    }

    /// <inheritdoc/>
    public void StartMonitoring()
    {
        lock (_lockObject)
        {
            if (Interlocked.Increment(ref _monitoringCount) == 1)
            {
                InitializeCpuUsageTracking();
                _timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            }
        }
    }

    /// <inheritdoc/>
    public void StopMonitoring()
    {
        lock (_lockObject)
        {
            if (Interlocked.Decrement(ref _monitoringCount) == 0)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }
    }

    /// <inheritdoc/>
    public PerformanceData GetCurrentPerformanceData()
    {
        double cpuUsage = GetCpuUsage();
        (double memoryUsage, long memoryUsedMB, long memoryTotalMB) = GetMemoryInfo();
        double? gpuUsage = _gpuMonitor.GetGpuUsage();
        double? cpuTemperature = _temperatureMonitor.GetCpuTemperature();
        double? gpuTemperature = _temperatureMonitor.GetGpuTemperature();

        return new PerformanceData(
            cpuUsage,
            memoryUsage,
            gpuUsage,
            cpuTemperature,
            gpuTemperature,
            memoryUsedMB,
            memoryTotalMB
        );
    }

    private void OnTimerCallback(object? state)
    {
        if (_monitoringCount == 0)
        {
            return;
        }

        // Prevent overlapping callbacks (GPU sampling can take >100ms)
        if (Interlocked.CompareExchange(ref _callbackRunning, 1, 0) != 0)
        {
            return;
        }

        try
        {
            PerformanceData performanceData = GetCurrentPerformanceData();
            PerformanceDataUpdated?.Invoke(this, new PerformanceDataEventArgs(performanceData));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting performance data: {ex.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _callbackRunning, 0);
        }
    }

    private void InitializeCpuUsageTracking()
    {
        unsafe
        {
            FILETIME idleTime, kernelTime, userTime;
            if (PInvoke.GetSystemTimes(&idleTime, &kernelTime, &userTime))
            {
                _lastIdleTime = FileTimeToUInt64(idleTime);
                _lastKernelTime = FileTimeToUInt64(kernelTime);
                _lastUserTime = FileTimeToUInt64(userTime);
            }
        }
    }

    private double GetCpuUsage()
    {
        unsafe
        {
            FILETIME idleTime, kernelTime, userTime;
            if (!PInvoke.GetSystemTimes(&idleTime, &kernelTime, &userTime))
            {
                return 0.0;
            }

            ulong currentIdleTime = FileTimeToUInt64(idleTime);
            ulong currentKernelTime = FileTimeToUInt64(kernelTime);
            ulong currentUserTime = FileTimeToUInt64(userTime);

            ulong idleDiff = currentIdleTime - _lastIdleTime;
            ulong kernelDiff = currentKernelTime - _lastKernelTime;
            ulong userDiff = currentUserTime - _lastUserTime;

            ulong totalSys = kernelDiff + userDiff;
            ulong totalCpu = totalSys - idleDiff;

            double cpuUsage = 0.0;
            if (totalSys > 0)
            {
                cpuUsage = (double)totalCpu * 100.0 / totalSys;
            }

            _lastIdleTime = currentIdleTime;
            _lastKernelTime = currentKernelTime;
            _lastUserTime = currentUserTime;

            return Math.Max(0.0, Math.Min(100.0, cpuUsage));
        }
    }

    private static (double Usage, long UsedMB, long TotalMB) GetMemoryInfo()
    {
        var memoryStatus = new MEMORYSTATUSEX
        {
            dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
        };

        if (!PInvoke.GlobalMemoryStatusEx(ref memoryStatus))
        {
            return (0.0, 0, 0);
        }

        double memoryUsage = (double)memoryStatus.dwMemoryLoad;
        long totalMB = (long)(memoryStatus.ullTotalPhys / (1024 * 1024));
        long availMB = (long)(memoryStatus.ullAvailPhys / (1024 * 1024));
        long usedMB = totalMB - availMB;

        return (memoryUsage, usedMB, totalMB);
    }

    private static ulong FileTimeToUInt64(FILETIME fileTime)
    {
        return ((ulong)(uint)fileTime.dwHighDateTime << 32) | (ulong)(uint)fileTime.dwLowDateTime;
    }
}
