namespace WindowSill.PerfCounter.Services;

public class PerformanceDataEventArgs : EventArgs
{
    public PerformanceData Data { get; }

    public PerformanceDataEventArgs(PerformanceData data)
    {
        Data = data;
    }
}
