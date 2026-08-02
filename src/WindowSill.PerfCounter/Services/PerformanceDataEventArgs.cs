namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Carries a newly sampled performance snapshot.
/// </summary>
public class PerformanceDataEventArgs : EventArgs
{
    /// <summary>
    /// Gets the sampled performance data.
    /// </summary>
    public PerformanceData Data { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceDataEventArgs"/> class.
    /// </summary>
    /// <param name="data">The sampled performance data.</param>
    public PerformanceDataEventArgs(PerformanceData data)
    {
        Data = data;
    }
}
