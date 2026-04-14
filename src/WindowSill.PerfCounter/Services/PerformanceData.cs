namespace WindowSill.PerfCounter.Services;

public record PerformanceData(
    double CpuUsage,
    double MemoryUsage,
    double? GpuUsage,
    double? CpuTemperature,
    double? GpuTemperature,
    long MemoryUsedMB,
    long MemoryTotalMB
);
