namespace WindowSill.PerfCounter.Services.Interop;

/// <summary>
/// Owns a PDH query and dynamically refreshes localized wildcard instances.
/// </summary>
internal sealed class PdhCounterQuery : IDisposable
{
    private readonly WildcardCounterSet[] _counterSets;
    private nint _query;

    private PdhCounterQuery(nint query, WildcardCounterSet[] counterSets)
    {
        _query = query;
        _counterSets = counterSets;
    }

    internal static bool TryCreate(
        IReadOnlyList<string> counterPaths,
        out PdhCounterQuery? query,
        out uint status)
    {
        query = null;
        status = PdhInterop.PdhOpenQuery(null, nint.Zero, out nint queryHandle);
        if (status != PdhInterop.ErrorSuccess)
        {
            return false;
        }

        var counterSets = new WildcardCounterSet[counterPaths.Count];
        for (int index = 0; index < counterPaths.Count; index++)
        {
            if (!PdhInterop.TryGetLocalizedCounterPath(
                    queryHandle,
                    counterPaths[index],
                    out string localizedPath,
                    out status))
            {
                PdhInterop.PdhCloseQuery(queryHandle);
                return false;
            }

            counterSets[index] = new WildcardCounterSet(localizedPath);
        }

        var createdQuery = new PdhCounterQuery(queryHandle, counterSets);
        if (!createdQuery.RefreshCounters(out status))
        {
            createdQuery.Dispose();
            return false;
        }

        query = createdQuery;
        return true;
    }

    internal bool Collect()
    {
        if (_query == nint.Zero)
        {
            return false;
        }

        // A refresh failure does not invalidate counters that were already registered.
        RefreshCounters(out _);

        if (_counterSets.Sum(counterSet => counterSet.Counters.Count) == 0)
        {
            return true;
        }

        return PdhInterop.PdhCollectQueryData(_query) == PdhInterop.ErrorSuccess;
    }

    internal IReadOnlyList<PdhCounterValue> GetFormattedCounterArray(int counterIndex)
    {
        ObjectDisposedException.ThrowIf(_query == nint.Zero, this);

        var values = new List<PdhCounterValue>();
        foreach (CounterRegistration registration in _counterSets[counterIndex].Counters.Values)
        {
            if (PdhInterop.TryGetFormattedCounterValue(
                    registration.Handle,
                    out double value))
            {
                values.Add(new PdhCounterValue(registration.InstanceName, value));
            }
        }

        return values;
    }

    public void Dispose()
    {
        nint query = Interlocked.Exchange(ref _query, nint.Zero);
        if (query != nint.Zero)
        {
            PdhInterop.PdhCloseQuery(query);
        }

        foreach (WildcardCounterSet counterSet in _counterSets)
        {
            counterSet.Counters.Clear();
        }
    }

    private bool RefreshCounters(out uint status)
    {
        status = PdhInterop.ErrorSuccess;

        foreach (WildcardCounterSet counterSet in _counterSets)
        {
            if (!PdhInterop.TryExpandWildcardPath(
                    counterSet.LocalizedPath,
                    out IReadOnlyList<string> expandedPaths,
                    out status))
            {
                return false;
            }

            var activePaths = expandedPaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (string expandedPath in expandedPaths)
            {
                if (counterSet.Counters.ContainsKey(expandedPath))
                {
                    continue;
                }

                if (!TryGetInstanceName(expandedPath, out string instanceName))
                {
                    continue;
                }

                status = PdhInterop.PdhAddCounter(
                    _query,
                    expandedPath,
                    nint.Zero,
                    out nint counter);
                if (status != PdhInterop.ErrorSuccess)
                {
                    return false;
                }

                counterSet.Counters.Add(
                    expandedPath,
                    new CounterRegistration(instanceName, counter));
            }

            foreach (string stalePath in counterSet.Counters.Keys
                         .Where(path => !activePaths.Contains(path))
                         .ToArray())
            {
                PdhInterop.PdhRemoveCounter(counterSet.Counters[stalePath].Handle);
                counterSet.Counters.Remove(stalePath);
            }
        }

        return true;
    }

    private static bool TryGetInstanceName(
        string counterPath,
        out string instanceName)
    {
        int instanceEnd = counterPath.LastIndexOf(")\\", StringComparison.Ordinal);
        int instanceStart = instanceEnd >= 0
            ? counterPath.LastIndexOf('(', instanceEnd)
            : -1;

        if (instanceStart < 0 || instanceEnd <= instanceStart + 1)
        {
            instanceName = string.Empty;
            return false;
        }

        instanceName = counterPath[(instanceStart + 1)..instanceEnd];
        return true;
    }

    private sealed class WildcardCounterSet(string localizedPath)
    {
        internal string LocalizedPath { get; } = localizedPath;

        internal Dictionary<string, CounterRegistration> Counters { get; } =
            new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed record CounterRegistration(string InstanceName, nint Handle);
}
