namespace WindowSill.PerfCounter.Services;

public interface IPerformanceMonitorService
{
    event EventHandler<PerformanceDataEventArgs> PerformanceDataUpdated;

    void StartMonitoring();

    void StopMonitoring();

    PerformanceData GetCurrentPerformanceData();
}

public class PerformanceDataEventArgs : EventArgs
{
    public PerformanceData Data { get; }
    
    public PerformanceDataEventArgs(PerformanceData data)
    {
        Data = data;
    }
}

public record PerformanceData(
    double CpuUsage,
    double MemoryUsage, 
    double? GpuUsage
);