namespace WindowSill.PerfCounter.Services;

/// <summary>
/// A synchronized snapshot of the extension's system-performance metrics.
/// </summary>
public record PerformanceData(
    double CpuUsage,
    double MemoryUsage,
    double? GpuUsage,
    double? CpuTemperature,
    double? GpuTemperature,
    long MemoryUsedMB,
    long MemoryTotalMB)
{
    /// <summary>
    /// Gets the total user-mode CPU utilization percentage.
    /// </summary>
    public double CpuUserUsage { get; init; }

    /// <summary>
    /// Gets the total privileged-mode CPU utilization percentage.
    /// </summary>
    public double CpuPrivilegedUsage { get; init; }

    /// <summary>
    /// Gets utilization by processor group and logical processor.
    /// </summary>
    public IReadOnlyList<CpuLogicalProcessorUsage> CpuLogicalProcessors { get; init; } = [];

    /// <summary>
    /// Gets hardware information when the sampled GPU adapter is unambiguous.
    /// </summary>
    public GpuHardwareInfo? GpuInfo { get; init; }
}
