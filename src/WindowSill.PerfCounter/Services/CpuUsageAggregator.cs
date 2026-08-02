using WindowSill.PerfCounter.Services.Interop;

namespace WindowSill.PerfCounter.Services;

internal static class CpuUsageAggregator
{
    internal static CpuPerformanceData? Create(
        IReadOnlyList<PdhCounterValue> totalValues,
        IReadOnlyList<PdhCounterValue> userValues,
        IReadOnlyList<PdhCounterValue> privilegedValues)
    {
        Dictionary<string, double> totals = ToValueMap(totalValues);
        Dictionary<string, double> users = ToValueMap(userValues);
        Dictionary<string, double> privileged = ToValueMap(privilegedValues);

        var processors = totals
            .Select(pair => TryParseProcessor(pair.Key, pair.Value, users, privileged))
            .OfType<CpuLogicalProcessorUsage>()
            .OrderBy(processor => processor.Group)
            .ThenBy(processor => processor.Processor)
            .ToArray();

        double? totalUsage = GetAggregate(totals, processors.Select(processor => processor.Usage));
        if (!totalUsage.HasValue)
        {
            return null;
        }

        double userUsage = GetAggregate(
            users,
            processors.Select(processor => processor.UserUsage)) ?? 0;
        double privilegedUsage = GetAggregate(
            privileged,
            processors.Select(processor => processor.PrivilegedUsage)) ?? 0;

        return new CpuPerformanceData(
            ClampPercent(totalUsage.Value),
            ClampPercent(userUsage),
            ClampPercent(privilegedUsage),
            processors);
    }

    private static Dictionary<string, double> ToValueMap(
        IReadOnlyList<PdhCounterValue> values)
    {
        var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (PdhCounterValue value in values)
        {
            result[value.Name] = value.Value;
        }

        return result;
    }

    private static CpuLogicalProcessorUsage? TryParseProcessor(
        string instanceName,
        double usage,
        IReadOnlyDictionary<string, double> users,
        IReadOnlyDictionary<string, double> privileged)
    {
        int separator = instanceName.IndexOf(',');
        if (separator <= 0 ||
            !int.TryParse(instanceName.AsSpan(0, separator), out int group) ||
            !int.TryParse(instanceName.AsSpan(separator + 1), out int processor))
        {
            return null;
        }

        users.TryGetValue(instanceName, out double userUsage);
        privileged.TryGetValue(instanceName, out double privilegedUsage);

        return new CpuLogicalProcessorUsage(
            group,
            processor,
            ClampPercent(usage),
            ClampPercent(userUsage),
            ClampPercent(privilegedUsage));
    }

    private static double? GetAggregate(
        IReadOnlyDictionary<string, double> values,
        IEnumerable<double> fallbackValues)
    {
        if (values.TryGetValue("_Total", out double aggregate) &&
            double.IsFinite(aggregate))
        {
            return aggregate;
        }

        double[] fallback = fallbackValues.Where(double.IsFinite).ToArray();
        return fallback.Length == 0 ? null : fallback.Average();
    }

    private static double ClampPercent(double value) =>
        double.IsFinite(value) ? Math.Clamp(value, 0, 100) : 0;
}
