namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Provides GPU utilization monitoring.
/// </summary>
internal interface IGpuMonitorService : IDisposable
{
    /// <summary>
    /// Opens and primes the GPU performance-counter query.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Stops sampling and releases the GPU performance-counter query.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Gets adapter-aware GPU utilization.
    /// </summary>
    /// <returns>The GPU sample, or <c>null</c> when counters are unavailable.</returns>
    GpuPerformanceData? GetGpuUsage();
}
