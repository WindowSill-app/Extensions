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
    /// Whether to show CPU usage in percentage mode
    /// </summary>
    internal static readonly SettingDefinition<bool> ShowCpu
        = new(true, typeof(Settings).Assembly);

    /// <summary>
    /// Whether to show GPU usage in percentage mode
    /// </summary>
    internal static readonly SettingDefinition<bool> ShowGpu
        = new(true, typeof(Settings).Assembly);

    /// <summary>
    /// Whether to show RAM usage in percentage mode
    /// </summary>
    internal static readonly SettingDefinition<bool> ShowRam
        = new(true, typeof(Settings).Assembly);
}
