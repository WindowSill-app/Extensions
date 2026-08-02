using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Logging;

using Windows.Win32;
using Windows.Win32.System.SystemInformation;

using WindowSill.API;

namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Aggregates CPU, memory, GPU, and temperature data on a one-second interval.
/// </summary>
[Export(typeof(IPerformanceMonitorService))]
internal sealed class PerformanceMonitorService : IPerformanceMonitorService, IDisposable
{
    private static readonly TimeSpan DefaultSamplingInterval = TimeSpan.FromSeconds(1);
    private static readonly ILogger Logger = typeof(PerformanceMonitorService).Log();

    private readonly ICpuMonitorService _cpuMonitor;
    private readonly IGpuMonitorService _gpuMonitor;
    private readonly ITemperatureMonitorService _temperatureMonitor;
    private readonly TimeSpan _samplingInterval;
    private readonly Func<CancellationToken, Task> _waitForNextSampleAsync;
    private readonly object _lockObject = new();

    private CancellationTokenSource? _monitoringCancellation;
    private Task? _monitoringTask;
    private TaskCompletionSource<PerformanceData>? _nextSample;
    private PerformanceData? _latestPerformanceData;
    private int _monitoringCount;
    private long _nextGeneration;
    private long _activeGeneration;
    private bool _disposed;

    public event EventHandler<PerformanceDataEventArgs>? PerformanceDataUpdated;

    [ImportingConstructor]
    public PerformanceMonitorService(
        ICpuMonitorService cpuMonitor,
        IGpuMonitorService gpuMonitor,
        ITemperatureMonitorService temperatureMonitor)
        : this(cpuMonitor, gpuMonitor, temperatureMonitor, DefaultSamplingInterval)
    {
    }

    internal PerformanceMonitorService(
        ICpuMonitorService cpuMonitor,
        IGpuMonitorService gpuMonitor,
        ITemperatureMonitorService temperatureMonitor,
        TimeSpan samplingInterval,
        Func<CancellationToken, Task>? waitForNextSampleAsync = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            samplingInterval,
            TimeSpan.Zero);

        _cpuMonitor = cpuMonitor;
        _gpuMonitor = gpuMonitor;
        _temperatureMonitor = temperatureMonitor;
        _samplingInterval = samplingInterval;
        _waitForNextSampleAsync = waitForNextSampleAsync
            ?? (cancellationToken => Task.Delay(
                _samplingInterval,
                cancellationToken));
    }

    public void StartMonitoring()
    {
        lock (_lockObject)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _monitoringCount++;
            if (_monitoringCount != 1)
            {
                return;
            }

            try
            {
                StartMonitoringGeneration();
            }
            catch
            {
                _monitoringCount = 0;
                StopMonitoringGeneration();
                throw;
            }
        }
    }

    public void StopMonitoring()
    {
        lock (_lockObject)
        {
            if (_monitoringCount == 0)
            {
                return;
            }

            _monitoringCount--;
            if (_monitoringCount == 0)
            {
                StopMonitoringGeneration();
            }
        }
    }

    public PerformanceData GetCurrentPerformanceData()
    {
        bool temporaryLease = false;
        Task<PerformanceData> pendingSample;

        lock (_lockObject)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_monitoringCount > 0 && _latestPerformanceData is not null)
            {
                return _latestPerformanceData;
            }

            _monitoringCount++;
            temporaryLease = true;

            if (_monitoringCount == 1)
            {
                try
                {
                    StartMonitoringGeneration();
                }
                catch
                {
                    _monitoringCount = 0;
                    StopMonitoringGeneration();
                    throw;
                }
            }

            if (_nextSample is null)
            {
                _monitoringCount--;
                temporaryLease = false;
                if (_monitoringCount == 0)
                {
                    StopMonitoringGeneration();
                }

                throw new InvalidOperationException(
                    "Performance monitoring did not create a pending sample.");
            }

            pendingSample = _nextSample.Task;
        }

        try
        {
            return pendingSample
                .WaitAsync(TimeSpan.FromSeconds(10))
                .GetAwaiter()
                .GetResult();
        }
        finally
        {
            if (temporaryLease)
            {
                StopMonitoring();
            }
        }
    }

    public void Dispose()
    {
        lock (_lockObject)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _monitoringCount = 0;
            StopMonitoringGeneration();
        }
    }

    private void StartMonitoringGeneration()
    {
        _cpuMonitor.StartMonitoring();
        _gpuMonitor.StartMonitoring();
        _temperatureMonitor.StartMonitoring();

        long generation = checked(++_nextGeneration);
        _activeGeneration = generation;
        _latestPerformanceData = null;
        _nextSample = new TaskCompletionSource<PerformanceData>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellation = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellation.Token;
        _monitoringCancellation = cancellation;
        _monitoringTask = Task.Run(
            () => MonitorAsync(generation, cancellationToken));
    }

    private void StopMonitoringGeneration()
    {
        _activeGeneration = 0;

        CancellationTokenSource? cancellation = _monitoringCancellation;
        _monitoringCancellation = null;
        cancellation?.Cancel();

        _nextSample?.TrySetCanceled();
        _nextSample = null;
        _latestPerformanceData = null;
        _monitoringTask = null;

        _cpuMonitor.StopMonitoring();
        _gpuMonitor.StopMonitoring();
        _temperatureMonitor.StopMonitoring();

        cancellation?.Dispose();
    }

    private async Task MonitorAsync(long generation, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                await _waitForNextSampleAsync(cancellationToken)
                    .ConfigureAwait(false);

                lock (_lockObject)
                {
                    if (_disposed ||
                        _monitoringCount == 0 ||
                        _activeGeneration != generation)
                    {
                        return;
                    }

                    try
                    {
                        PerformanceData performanceData = SamplePerformanceData();
                        _latestPerformanceData = performanceData;
                        _nextSample?.TrySetResult(performanceData);
                        PerformanceDataUpdated?.Invoke(
                            this,
                            new PerformanceDataEventArgs(performanceData));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Unable to update performance-counter data.");
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private PerformanceData SamplePerformanceData()
    {
        CpuPerformanceData? cpu = _cpuMonitor.GetCpuUsage();
        (double memoryUsage, long memoryUsedMB, long memoryTotalMB) = GetMemoryInfo();
        GpuPerformanceData? gpu = _gpuMonitor.GetGpuUsage();
        double? cpuTemperature = _temperatureMonitor.GetCpuTemperature();
        double? gpuTemperature =
            _temperatureMonitor.GetGpuTemperature(gpu?.UnambiguousAdapter);

        return new PerformanceData(
            cpu?.TotalUsage ?? 0,
            memoryUsage,
            gpu?.OverallUsage,
            cpuTemperature,
            gpuTemperature,
            memoryUsedMB,
            memoryTotalMB)
        {
            CpuUserUsage = cpu?.UserUsage ?? 0,
            CpuPrivilegedUsage = cpu?.PrivilegedUsage ?? 0,
            CpuLogicalProcessors = cpu?.LogicalProcessors ?? [],
            GpuInfo = gpu?.UnambiguousAdapter?.ToHardwareInfo()
        };
    }

    private static (double Usage, long UsedMB, long TotalMB) GetMemoryInfo()
    {
        var memoryStatus = new MEMORYSTATUSEX
        {
            dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
        };

        if (!PInvoke.GlobalMemoryStatusEx(ref memoryStatus) ||
            memoryStatus.ullTotalPhys == 0)
        {
            return (0, 0, 0);
        }

        ulong usedBytes = memoryStatus.ullTotalPhys >= memoryStatus.ullAvailPhys
            ? memoryStatus.ullTotalPhys - memoryStatus.ullAvailPhys
            : 0;
        double usage = usedBytes * 100d / memoryStatus.ullTotalPhys;
        long totalMB = checked((long)(memoryStatus.ullTotalPhys / (1024 * 1024)));
        long usedMB = checked((long)(usedBytes / (1024 * 1024)));

        return (Math.Clamp(usage, 0, 100), usedMB, totalMB);
    }
}
