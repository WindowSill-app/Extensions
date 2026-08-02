namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Provides one-shot static hardware information for CPU, GPU, and memory.
/// </summary>
public interface IHardwareInfoService
{
    /// <summary>
    /// Gets static CPU hardware information.
    /// </summary>
    /// <returns>CPU info, or null if unavailable.</returns>
    Task<CpuHardwareInfo?> GetCpuInfoAsync();

    /// <summary>
    /// Gets static GPU hardware information when exactly one adapter is present.
    /// </summary>
    /// <returns>GPU info, or null when adapter identity would be ambiguous.</returns>
    Task<GpuHardwareInfo?> GetGpuInfoAsync();

    /// <summary>
    /// Gets static memory hardware information.
    /// </summary>
    /// <returns>Memory info.</returns>
    MemoryHardwareInfo GetMemoryInfo();
}
