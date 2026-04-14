namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Provides GPU utilization monitoring.
/// </summary>
internal interface IGpuMonitorService : IDisposable
{
    /// <summary>
    /// Gets the current GPU usage percentage, or null if no dedicated GPU is available.
    /// </summary>
    double? GetGpuUsage();
}
