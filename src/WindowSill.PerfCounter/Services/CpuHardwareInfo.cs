namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Static hardware information for the CPU.
/// </summary>
/// <param name="Name">The processor name (e.g., "AMD Ryzen 7 3700X 8-Core Processor").</param>
/// <param name="Cores">The number of physical cores.</param>
/// <param name="Threads">The number of logical processors (threads).</param>
/// <param name="MaxClockMHz">The maximum clock speed in MHz.</param>
public record CpuHardwareInfo(
    string Name,
    int Cores,
    int Threads,
    int MaxClockMHz);
