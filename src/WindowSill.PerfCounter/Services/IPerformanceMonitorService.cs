namespace WindowSill.PerfCounter.Services;

public interface IPerformanceMonitorService
{
    /// <summary>
    /// Raised when a new performance sample is available.
    /// </summary>
    event EventHandler<PerformanceDataEventArgs> PerformanceDataUpdated;

    /// <summary>
    /// Starts or retains shared performance monitoring.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Releases one performance-monitoring lease.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Gets a performance sample immediately.
    /// </summary>
    /// <returns>The current performance data.</returns>
    PerformanceData GetCurrentPerformanceData();
}
