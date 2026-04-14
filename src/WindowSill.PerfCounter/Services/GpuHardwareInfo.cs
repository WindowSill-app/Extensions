namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Static hardware information for the GPU.
/// </summary>
/// <param name="Name">The GPU adapter name (e.g., "NVIDIA GeForce RTX 3080").</param>
/// <param name="TotalVramMB">The total video RAM in megabytes, or null if unavailable.</param>
public record GpuHardwareInfo(
    string Name,
    long? TotalVramMB);
