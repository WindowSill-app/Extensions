namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Static hardware information for system memory.
/// </summary>
/// <param name="TotalMemoryMB">The total physical memory in megabytes.</param>
public record MemoryHardwareInfo(
    long TotalMemoryMB);
