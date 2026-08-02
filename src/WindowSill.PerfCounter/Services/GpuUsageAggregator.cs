using WindowSill.PerfCounter.Services.Interop;

namespace WindowSill.PerfCounter.Services;

internal static class GpuUsageAggregator
{
    internal static GpuPerformanceData? Create(
        IReadOnlyList<PdhCounterValue> counterValues,
        IReadOnlyDictionary<long, GpuAdapterInfo> adaptersByLuid)
    {
        var perEngineUsage = new Dictionary<(long Luid, string EngineId), double>();

        foreach (PdhCounterValue counterValue in counterValues)
        {
            if (counterValue.Value < 0 ||
                !double.IsFinite(counterValue.Value) ||
                !GpuEngineInstance.TryParse(counterValue.Name, out GpuEngineInstance instance))
            {
                continue;
            }

            var key = (instance.Luid, instance.EngineId);
            perEngineUsage[key] =
                perEngineUsage.GetValueOrDefault(key) + counterValue.Value;
        }

        IReadOnlyList<GpuAdapterInfo> displayableAdapters =
            GpuAdapterCatalog.GetDisplayableAdapters(adaptersByLuid);

        if (perEngineUsage.Count == 0 && counterValues.Count > 0)
        {
            return null;
        }

        var usageByLuid = new Dictionary<long, double>();
        foreach (((long luid, string _), double usage) in perEngineUsage)
        {
            double clampedUsage = ClampPercent(usage);
            if (!usageByLuid.TryGetValue(luid, out double currentUsage) ||
                clampedUsage > currentUsage)
            {
                usageByLuid[luid] = clampedUsage;
            }
        }

        bool hasHardwareAdapter = displayableAdapters.Any(adapter => !adapter.IsSoftware);
        var excludedSoftwareLuids = adaptersByLuid.Values
            .Where(adapter => hasHardwareAdapter && adapter.IsSoftware)
            .Select(adapter => adapter.Luid)
            .ToHashSet();
        var representedLuids = displayableAdapters
            .Select(adapter => adapter.Luid)
            .ToHashSet();

        foreach (long observedLuid in usageByLuid.Keys)
        {
            if (!excludedSoftwareLuids.Contains(observedLuid))
            {
                representedLuids.Add(observedLuid);
            }
        }

        var adapters = representedLuids
            .Select(luid =>
            {
                adaptersByLuid.TryGetValue(luid, out GpuAdapterInfo? adapter);
                return new GpuAdapterPerformanceData(
                    luid,
                    usageByLuid.GetValueOrDefault(luid),
                    adapter?.ToHardwareInfo());
            })
            .ToArray();

        if (adapters.Length == 0)
        {
            return null;
        }

        double overallUsage = representedLuids
            .Select(luid => usageByLuid.GetValueOrDefault(luid))
            .DefaultIfEmpty(0)
            .Max();

        GpuAdapterInfo? unambiguousAdapter = null;
        if (representedLuids.Count == 1)
        {
            long luid = representedLuids.Single();
            GpuAdapterInfo? adapter = displayableAdapters
                .SingleOrDefault(candidate => candidate.Luid == luid);
            if (adapter is not null)
            {
                unambiguousAdapter = adapter;
            }
        }

        return new GpuPerformanceData(
            ClampPercent(overallUsage),
            adapters,
            unambiguousAdapter);
    }

    private static double ClampPercent(double value) =>
        double.IsFinite(value) ? Math.Clamp(value, 0, 100) : 0;
}
