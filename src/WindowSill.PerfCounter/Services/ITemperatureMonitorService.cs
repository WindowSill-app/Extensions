namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Provides CPU and GPU temperature monitoring.
/// </summary>
internal interface ITemperatureMonitorService : IDisposable
{
    /// <summary>
    /// Opens the system thermal-zone counter query.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Stops sampling and releases temperature-monitoring resources.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Gets the current CPU-identified thermal-zone temperature in Celsius.
    /// </summary>
    /// <returns>The temperature, or <c>null</c> when no CPU-specific zone is exposed.</returns>
    double? GetCpuTemperature();

    /// <summary>
    /// Gets the current temperature for an unambiguously identified GPU adapter.
    /// </summary>
    /// <param name="adapter">The adapter selected by the GPU utilization sample.</param>
    /// <returns>The temperature, or <c>null</c> when unsupported or ambiguous.</returns>
    double? GetGpuTemperature(GpuAdapterInfo? adapter);
}
