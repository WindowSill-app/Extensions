namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Provides group-aware CPU utilization samples.
/// </summary>
internal interface ICpuMonitorService : IDisposable
{
    /// <summary>
    /// Opens and primes the CPU performance-counter query.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Stops sampling and releases the CPU performance-counter query.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Gets the latest CPU utilization sample.
    /// </summary>
    /// <returns>The CPU sample, or <c>null</c> when counters are unavailable.</returns>
    CpuPerformanceData? GetCpuUsage();
}
