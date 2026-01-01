using WindowSill.API;

namespace WindowSill.PerfCounter.Settings;

internal static class Settings
{
    /// <summary>
    /// The display mode for performance metrics (Percentage or RunningMan)
    /// </summary>
    internal static readonly SettingDefinition<PerformanceDisplayMode> DisplayMode
        = new(PerformanceDisplayMode.Percentage, typeof(Settings).Assembly);

    /// <summary>
    /// The metric to use for animated GIF speed (CPU, GPU, or RAM)
    /// </summary>
    internal static readonly SettingDefinition<PerformanceMetric> AnimationMetric
        = new(PerformanceMetric.CPU, typeof(Settings).Assembly);

    /// <summary>
    /// Whether to enable launching Task Manager when clicking the performance counter
    /// </summary>
    internal static readonly SettingDefinition<bool> EnableTaskManagerLaunch
        = new(true, typeof(Settings).Assembly);
}

public enum PerformanceDisplayMode
{
    Percentage,
    RunningMan
}

public enum PerformanceMetric
{
    CPU,
    GPU,
    RAM
}
