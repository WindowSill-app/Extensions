namespace WindowSill.PerfCounter.Settings;

internal sealed class DisplayModeItem
{
    public PerformanceDisplayMode Value { get; }
    public string DisplayName { get; }

    public DisplayModeItem(PerformanceDisplayMode value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    public override string ToString() => DisplayName;
}