using System.ComponentModel.Composition;

using WindowSill.PerfCounter.Services.Interop;

namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Monitors GPU utilization using PDH (Performance Data Helper) counters.
/// </summary>
[Export(typeof(IGpuMonitorService))]
internal sealed class GpuMonitorService : IGpuMonitorService
{
    private readonly Lock _lock = new();
    private readonly List<nint> _gpuCounters = [];

    private nint _query = nint.Zero;
    private bool _initialized;

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_query != nint.Zero)
            {
                PdhInterop.PdhCloseQuery(_query);
                _query = nint.Zero;
            }

            _gpuCounters.Clear();
            _initialized = false;
        }
    }

    /// <inheritdoc/>
    public double? GetGpuUsage()
    {
        lock (_lock)
        {
            if (!_initialized && !InitializeGpuCounters())
            {
                return null;
            }

            return CollectGpuData();
        }
    }

    private bool InitializeGpuCounters()
    {
        try
        {
            if (!GpuDetector.HasDedicatedGpu())
            {
                return false;
            }

            if (_query != nint.Zero)
            {
                PdhInterop.PdhCloseQuery(_query);
                _query = nint.Zero;
            }

            _gpuCounters.Clear();

            uint status = PdhInterop.PdhOpenQuery(nint.Zero, nint.Zero, out _query);
            if (status != 0)
            {
                return false;
            }

            // Try wildcard GPU counter
            uint addStatus = PdhInterop.PdhAddCounter(_query, "\\GPU Engine(*)\\Utilization Percentage", nint.Zero, out nint counter);
            if (addStatus == 0)
            {
                _gpuCounters.Add(counter);
                _initialized = true;
                return true;
            }

            // Fallback: enumerate individual instances
            List<string> instanceNames = EnumerateGpuEngineInstances();
            foreach (string instanceName in instanceNames)
            {
                string counterPath = $"\\GPU Engine({instanceName})\\Utilization Percentage";
                uint instanceStatus = PdhInterop.PdhAddCounter(_query, counterPath, nint.Zero, out nint instanceCounter);
                if (instanceStatus == 0)
                {
                    _gpuCounters.Add(instanceCounter);
                }
            }

            _initialized = _gpuCounters.Count > 0;
            return _initialized;
        }
        catch
        {
            return false;
        }
    }

    private double? CollectGpuData()
    {
        if (_query == nint.Zero || _gpuCounters.Count == 0)
        {
            return null;
        }

        try
        {
            // PDH requires two collections with a delay for rate-based counters
            PdhInterop.PdhCollectQueryData(_query);
            Thread.Sleep(100);
            PdhInterop.PdhCollectQueryData(_query);

            double totalUsage = 0.0;
            int validCounters = 0;

            foreach (nint counter in _gpuCounters)
            {
                (double total, int count) = PdhInterop.CollectFormattedCounterArray(counter);
                totalUsage += total;
                validCounters += count;
            }

            return Math.Max(0.0, Math.Min(100.0, totalUsage));
        }
        catch
        {
            return null;
        }
    }

    private static List<string> EnumerateGpuEngineInstances()
    {
        try
        {
            List<string> instances = PdhInterop.EnumerateObjectInstances("GPU Engine");
            return instances.Count > 0 ? instances : GetFallbackInstances();
        }
        catch
        {
            return GetFallbackInstances();
        }
    }

    private static List<string> GetFallbackInstances()
    {
        return
        [
            "pid_*_luid_*_phys_*_eng_*_engtype_3D",
            "pid_*_luid_*_phys_*_eng_*_engtype_Graphics",
            "pid_*_luid_*_phys_*_eng_*_engtype_Compute"
        ];
    }
}
