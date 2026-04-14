namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Provides CPU and GPU temperature monitoring.
/// </summary>
internal interface ITemperatureMonitorService : IDisposable
{
    /// <summary>
    /// Gets the current CPU temperature in Celsius, or null if unavailable.
    /// </summary>
    double? GetCpuTemperature();

    /// <summary>
    /// Gets the current GPU temperature in Celsius, or null if unavailable.
    /// </summary>
    double? GetGpuTemperature();
}
