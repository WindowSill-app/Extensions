using System.Runtime.InteropServices;
using Microsoft.Win32;

using Windows.Win32;

namespace WindowSill.PerfCounter.Services;

internal class GpuMonitorService : IDisposable
{
    private readonly Lock _lock = new();
    private readonly List<nint> _gpuCounters = [];

    private nint _query = nint.Zero;
    private bool _initialized = false;

    public void Dispose()
    {
        lock (_lock)
        {
            if (_query != nint.Zero)
            {
                PdhCloseQuery(_query);
                _query = nint.Zero;
            }
            _gpuCounters.Clear();
            _initialized = false;
        }
    }

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
            // Check if there's a dedicated GPU first
            if (!HasDedicatedGpu())
            {
                return false;
            }

            // Close existing query if any
            if (_query != nint.Zero)
            {
                PdhCloseQuery(_query);
                _query = nint.Zero;
            }

            // Clear existing counters
            _gpuCounters.Clear();

            // Open a new query
            uint status = PdhOpenQuery(nint.Zero, nint.Zero, out _query);
            if (status != 0)
            {
                return false;
            }

            // Try to add wildcard GPU counter
            uint status3 = PdhAddCounter(_query, "\\GPU Engine(*)\\Utilization Percentage", nint.Zero, out nint counter);
            if (status3 == 0)
            {
                _gpuCounters.Add(counter);
                _initialized = true;
                return true;
            }

            // If wildcard fails, try enumeration
            List<string> instanceNames = EnumerateGpuEngineInstances();

            foreach (string instanceName in instanceNames)
            {
                string counterPath = $"\\GPU Engine({instanceName})\\Utilization Percentage";

                uint status2 = PdhAddCounter(_query, counterPath, nint.Zero, out nint instanceCounter);
                if (status2 == 0)
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
            // Collect data twice with a small delay for accurate readings
            PdhCollectQueryData(_query);
            Thread.Sleep(100);
            PdhCollectQueryData(_query);

            double totalUsage = 0.0;
            int validCounters = 0;

            foreach (nint counter in _gpuCounters)
            {
                // Try to get formatted counter array first (for wildcard counters)
                uint bufferSize = 0u;
                uint itemCount = 0u;

                uint status = PdhGetFormattedCounterArray(counter, PDH_FMT.PDH_FMT_DOUBLE,
                    ref bufferSize, ref itemCount, nint.Zero);

                if (status == PInvoke.PDH_MORE_DATA && itemCount > 0)
                {
                    nint buffer = Marshal.AllocHGlobal((int)bufferSize);
                    try
                    {
                        status = PdhGetFormattedCounterArray(counter, PDH_FMT.PDH_FMT_DOUBLE,
                            ref bufferSize, ref itemCount, buffer);

                        if (status == 0)
                        {
                            // Sum up all GPU engine utilizations
                            nint current = buffer;
                            for (int i = 0; i < itemCount; i++)
                            {
                                PdhFmtCounterValueItemDouble item = Marshal.PtrToStructure<PdhFmtCounterValueItemDouble>(current);
                                if (item.FmtValue.CStatus == PInvoke.PDH_CSTATUS_VALID_DATA)
                                {
                                    totalUsage += item.FmtValue.doubleValue;
                                    validCounters++;
                                }
                                current = IntPtr.Add(current, Marshal.SizeOf<PdhFmtCounterValueItemDouble>());
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }
                else
                {
                    // Try single counter value
                    var counterValue = new PdhFmtCounterValue();
                    uint status2 = PdhGetFormattedCounterValue(counter, PDH_FMT.PDH_FMT_DOUBLE, nint.Zero, ref counterValue);

                    if (status2 == 0 && counterValue.CStatus == 0)
                    {
                        totalUsage += counterValue.doubleValue;
                        validCounters++;
                    }
                }
            }

            // Return average usage, clamped between 0 and 100
            return Math.Max(0.0, Math.Min(100.0, totalUsage));
        }
        catch
        {
            return null;
        }
    }

    private static bool HasDedicatedGpu()
    {
        try
        {
            // Check the Windows Registry for installed display adapters
            using RegistryKey? displayAdaptersKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");

            if (displayAdaptersKey == null)
            {
                return false;
            }

            foreach (string subKeyName in displayAdaptersKey.GetSubKeyNames())
            {
                // Skip non-numeric subkeys (like "Properties")
                if (!subKeyName.All(char.IsDigit))
                {
                    continue;
                }

                // Check this adapter
                if (IsAdapterDedicated(displayAdaptersKey, subKeyName))
                {
                    return true;
                }
            }
        }
        catch
        {
            // Registry access failed
        }

        return false;
    }

    private static bool IsAdapterDedicated(RegistryKey parentKey, string subKeyName)
    {
        try
        {
            using RegistryKey? adapterKey = parentKey.OpenSubKey(subKeyName);
            if (adapterKey == null)
            {
                return false;
            }

            // Get adapter description
            string description = adapterKey.GetValue("DriverDesc") as string ?? "";
            string hardwareId = adapterKey.GetValue("MatchingDeviceId") as string ?? "";

            // Combine for analysis
            string adapterInfo = $"{description} {hardwareId}".ToUpperInvariant();

            // Check for dedicated GPU indicators
            return IsDedicatedGpuByDescription(adapterInfo);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsDedicatedGpuByDescription(string adapterInfo)
    {
        // Check for dedicated GPU brands/models
        string[] dedicatedKeywords = {
            "NVIDIA", "GEFORCE", "QUADRO", "TESLA", "RTX", "GTX",
            "AMD RADEON RX", "AMD RADEON R9", "AMD RADEON R7",
            "RADEON RX", "RADEON R9", "RADEON R7",
            "RADEON HD 7", "RADEON HD 6", "RADEON HD 5"
        };

        foreach (string keyword in dedicatedKeywords)
        {
            if (adapterInfo.Contains(keyword))
            {
                return true;
            }
        }

        // If it contains VEN_ vendor IDs for known dedicated GPU vendors, it's likely dedicated
        if (adapterInfo.Contains("VEN_10DE") || // NVIDIA
            (adapterInfo.Contains("VEN_1002") && !adapterInfo.Contains("RADEON GRAPHICS"))) // AMD (but not APU)
        {
            return true;
        }

        return false;
    }

    private static List<string> EnumerateGpuEngineInstances()
    {
        var instances = new List<string>();

        try
        {
            // Get buffer sizes first
            uint counterListSize = 0u;
            uint instanceListSize = 0u;

            uint status
                = PdhEnumObjectItems(
                    null,
                    null,
                    "GPU Engine",
                    nint.Zero,
                    ref counterListSize,
                    nint.Zero, ref instanceListSize,
                    PERF_DETAIL.PERF_DETAIL_WIZARD,
                    0);

            if (status != PInvoke.PDH_MORE_DATA || instanceListSize == 0) // PDH_MORE_DATA
            {
                return GetFallbackInstances();
            }

            // Allocate buffer for instance names
            nint buffer = Marshal.AllocHGlobal((int)instanceListSize * 2);

            try
            {
                status
                    = PdhEnumObjectItems(
                        null,
                        null,
                        "GPU Engine",
                        nint.Zero,
                        ref counterListSize,
                        buffer,
                        ref instanceListSize,
                        PERF_DETAIL.PERF_DETAIL_WIZARD,
                        0);

                if (status == 0)
                {
                    // Parse null-terminated string list
                    nint current = buffer;
                    while (true)
                    {
                        string? instanceName = Marshal.PtrToStringUni(current);
                        if (string.IsNullOrEmpty(instanceName))
                        {
                            break;
                        }

                        instances.Add(instanceName);
                        current = IntPtr.Add(current, (instanceName.Length + 1) * 2);
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch
        {
            // Fall back to common patterns
            instances = GetFallbackInstances();
        }

        return instances;
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

    #region PDH P/Invoke Declarations

    [StructLayout(LayoutKind.Sequential)]
    private struct PdhFmtCounterValue
    {
        public uint CStatus;
        public double doubleValue;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PdhFmtCounterValueItemDouble
    {
        public nint szName;
        public PdhFmtCounterValue FmtValue;
    }

    private enum PDH_FMT : uint
    {
        PDH_FMT_DOUBLE = 512U,
        PDH_FMT_LARGE = 1024U,
        PDH_FMT_LONG = 256U,
    }

    private enum PERF_DETAIL : uint
    {
        PERF_DETAIL_NOVICE = 100U,
        PERF_DETAIL_ADVANCED = 200U,
        PERF_DETAIL_EXPERT = 300U,
        PERF_DETAIL_WIZARD = 400U,
    }

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhOpenQuery(nint szDataSource, nint dwUserData, out nint phQuery);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhAddCounter(nint hQuery, string szFullCounterPath, nint dwUserData, out nint phCounter);

    [DllImport("pdh.dll")]
    private static extern uint PdhCollectQueryData(nint hQuery);

    [DllImport("pdh.dll")]
    private static extern uint PdhCloseQuery(nint hQuery);

    [DllImport("pdh.dll")]
    private static extern uint PdhGetFormattedCounterValue(nint hCounter, PDH_FMT dwFormat, nint lpdwType, ref PdhFmtCounterValue pValue);

    [DllImport("pdh.dll")]
    private static extern uint PdhGetFormattedCounterArray(nint hCounter, PDH_FMT dwFormat, ref uint lpdwBufferSize, ref uint lpdwItemCount, nint ItemBuffer);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhEnumObjectItems(
        string? szDataSource,
        string? szMachineName,
        string szObjectName,
        nint mszCounterList,
        ref uint pcchCounterListLength,
        nint mszInstanceList,
        ref uint pcchInstanceListLength,
        PERF_DETAIL dwDetailLevel,
        uint dwFlags);

    #endregion
}
