namespace WindowSill.PerfCounter.Services;

internal sealed record GpuAdapterPerformanceData(
    long Luid,
    double Usage,
    GpuHardwareInfo? HardwareInfo);

internal sealed record GpuPerformanceData(
    double OverallUsage,
    IReadOnlyList<GpuAdapterPerformanceData> Adapters,
    GpuAdapterInfo? UnambiguousAdapter);
