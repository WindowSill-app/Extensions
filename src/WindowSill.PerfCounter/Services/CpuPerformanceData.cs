namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Utilization for one logical processor in a Windows processor group.
/// </summary>
/// <param name="Group">The processor-group index.</param>
/// <param name="Processor">The logical-processor index within the group.</param>
/// <param name="Usage">The total utilization percentage.</param>
/// <param name="UserUsage">The user-mode utilization percentage.</param>
/// <param name="PrivilegedUsage">The privileged-mode utilization percentage.</param>
public sealed record CpuLogicalProcessorUsage(
    int Group,
    int Processor,
    double Usage,
    double UserUsage,
    double PrivilegedUsage);

/// <summary>
/// Group-aware aggregate and per-logical-processor CPU utilization.
/// </summary>
/// <param name="TotalUsage">The total processor utilization percentage.</param>
/// <param name="UserUsage">The total user-mode utilization percentage.</param>
/// <param name="PrivilegedUsage">The total privileged-mode utilization percentage.</param>
/// <param name="LogicalProcessors">Utilization grouped by logical processor.</param>
public sealed record CpuPerformanceData(
    double TotalUsage,
    double UserUsage,
    double PrivilegedUsage,
    IReadOnlyList<CpuLogicalProcessorUsage> LogicalProcessors);
