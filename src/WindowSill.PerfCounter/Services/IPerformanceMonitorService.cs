namespace WindowSill.PerfCounter.Services;

public interface IPerformanceMonitorService
{
    event EventHandler<PerformanceDataEventArgs> PerformanceDataUpdated;

    void StartMonitoring();

    void StopMonitoring();

    PerformanceData GetCurrentPerformanceData();
}
