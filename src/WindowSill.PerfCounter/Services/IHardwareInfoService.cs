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
    /// Gets static GPU hardware information.
    /// </summary>
    /// <returns>GPU info, or null if no dedicated GPU is detected.</returns>
    Task<GpuHardwareInfo?> GetGpuInfoAsync();

    /// <summary>
    /// Gets static memory hardware information.
    /// </summary>
    /// <returns>Memory info.</returns>
    MemoryHardwareInfo GetMemoryInfo();
}
