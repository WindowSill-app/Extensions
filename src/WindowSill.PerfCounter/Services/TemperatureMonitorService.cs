using System.Runtime.InteropServices;

using Microsoft.Win32;

namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Monitors CPU and GPU temperatures using no-admin-required APIs.
/// CPU temperature is read via PDH thermal zone counters.
/// GPU temperature is read via NVIDIA NVML or AMD ADL, depending on the detected vendor.
/// </summary>
internal sealed class TemperatureMonitorService : IDisposable
{
    private readonly Lock _lock = new();

    // CPU temperature via PDH
    private nint _cpuThermalQuery = nint.Zero;
    private readonly List<nint> _cpuThermalCounters = [];
    private bool _cpuInitialized;

    // GPU temperature via vendor APIs
    private GpuVendor _gpuVendor = GpuVendor.Unknown;
    private bool _gpuInitialized;

    // NVML state
    private bool _nvmlLoaded;
    private nint _nvmlDeviceHandle;

    // ADL state
    private bool _adlLoaded;
    private nint _adlContext;
    private int _adlAdapterIndex;

    /// <summary>
    /// Gets the current CPU temperature in Celsius, or null if unavailable.
    /// </summary>
    public double? GetCpuTemperature()
    {
        lock (_lock)
        {
            if (!_cpuInitialized && !InitializeCpuThermalCounters())
            {
                return null;
            }

            return CollectCpuTemperature();
        }
    }

    /// <summary>
    /// Gets the current GPU temperature in Celsius, or null if unavailable.
    /// </summary>
    public double? GetGpuTemperature()
    {
        lock (_lock)
        {
            if (!_gpuInitialized)
            {
                InitializeGpuTemperature();
            }

            return _gpuVendor switch
            {
                GpuVendor.Nvidia => GetNvidiaGpuTemperature(),
                GpuVendor.Amd => GetAmdGpuTemperature(),
                _ => null
            };
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lock)
        {
            DisposeCpuResources();
            DisposeGpuResources();
        }
    }

    #region CPU Temperature via PDH

    private bool InitializeCpuThermalCounters()
    {
        try
        {
            if (_cpuThermalQuery != nint.Zero)
            {
                PdhCloseQuery(_cpuThermalQuery);
                _cpuThermalQuery = nint.Zero;
            }

            _cpuThermalCounters.Clear();

            uint status = PdhOpenQuery(nint.Zero, nint.Zero, out _cpuThermalQuery);
            if (status != 0)
            {
                return false;
            }

            // Try wildcard counter for all thermal zones
            status = PdhAddCounter(
                _cpuThermalQuery,
                @"\Thermal Zone Information(*)\Temperature",
                nint.Zero,
                out nint counter);

            if (status == 0)
            {
                _cpuThermalCounters.Add(counter);
                _cpuInitialized = true;
                return true;
            }

            // Fallback: enumerate thermal zone instances
            List<string> instances = EnumerateThermalZoneInstances();
            foreach (string instance in instances)
            {
                string counterPath = $@"\Thermal Zone Information({instance})\Temperature";
                uint addStatus = PdhAddCounter(_cpuThermalQuery, counterPath, nint.Zero, out nint instanceCounter);
                if (addStatus == 0)
                {
                    _cpuThermalCounters.Add(instanceCounter);
                }
            }

            _cpuInitialized = _cpuThermalCounters.Count > 0;
            return _cpuInitialized;
        }
        catch
        {
            return false;
        }
    }

    private double? CollectCpuTemperature()
    {
        if (_cpuThermalQuery == nint.Zero || _cpuThermalCounters.Count == 0)
        {
            return null;
        }

        try
        {
            PdhCollectQueryData(_cpuThermalQuery);

            double maxTemperature = double.MinValue;
            bool anyValid = false;

            foreach (nint counter in _cpuThermalCounters)
            {
                // Try counter array first (wildcard counters)
                uint bufferSize = 0u;
                uint itemCount = 0u;

                uint status = PdhGetFormattedCounterArray(
                    counter,
                    PDH_FMT_DOUBLE,
                    ref bufferSize,
                    ref itemCount,
                    nint.Zero);

                if (status == PDH_MORE_DATA && itemCount > 0)
                {
                    nint buffer = Marshal.AllocHGlobal((int)bufferSize);
                    try
                    {
                        status = PdhGetFormattedCounterArray(
                            counter,
                            PDH_FMT_DOUBLE,
                            ref bufferSize,
                            ref itemCount,
                            buffer);

                        if (status == 0)
                        {
                            nint current = buffer;
                            for (int i = 0; i < itemCount; i++)
                            {
                                var item = Marshal.PtrToStructure<PdhFmtCounterValueItemDouble>(current);
                                if (item.FmtValue.CStatus == PDH_CSTATUS_VALID_DATA)
                                {
                                    // PDH thermal zone values are in Kelvin
                                    double celsius = item.FmtValue.doubleValue - 273.15;
                                    if (celsius > maxTemperature)
                                    {
                                        maxTemperature = celsius;
                                        anyValid = true;
                                    }
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
                    uint singleStatus = PdhGetFormattedCounterValue(
                        counter,
                        PDH_FMT_DOUBLE,
                        nint.Zero,
                        ref counterValue);

                    if (singleStatus == 0 && counterValue.CStatus == PDH_CSTATUS_VALID_DATA)
                    {
                        double celsius = counterValue.doubleValue - 273.15;
                        if (celsius > maxTemperature)
                        {
                            maxTemperature = celsius;
                            anyValid = true;
                        }
                    }
                }
            }

            if (!anyValid)
            {
                return null;
            }

            // Sanity check: temperature should be in a reasonable range
            return maxTemperature is >= -40.0 and <= 150.0
                ? Math.Round(maxTemperature, 1)
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static List<string> EnumerateThermalZoneInstances()
    {
        var instances = new List<string>();

        try
        {
            uint counterListSize = 0u;
            uint instanceListSize = 0u;

            uint status = PdhEnumObjectItems(
                null,
                null,
                "Thermal Zone Information",
                nint.Zero,
                ref counterListSize,
                nint.Zero,
                ref instanceListSize,
                PERF_DETAIL_WIZARD,
                0);

            if (status != PDH_MORE_DATA || instanceListSize == 0)
            {
                return instances;
            }

            nint buffer = Marshal.AllocHGlobal((int)instanceListSize * 2);
            try
            {
                status = PdhEnumObjectItems(
                    null,
                    null,
                    "Thermal Zone Information",
                    nint.Zero,
                    ref counterListSize,
                    buffer,
                    ref instanceListSize,
                    PERF_DETAIL_WIZARD,
                    0);

                if (status == 0)
                {
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
            // Enumeration failed — return empty
        }

        return instances;
    }

    private void DisposeCpuResources()
    {
        if (_cpuThermalQuery != nint.Zero)
        {
            PdhCloseQuery(_cpuThermalQuery);
            _cpuThermalQuery = nint.Zero;
        }

        _cpuThermalCounters.Clear();
        _cpuInitialized = false;
    }

    #endregion

    #region GPU Temperature

    private void InitializeGpuTemperature()
    {
        _gpuInitialized = true;

        // Detect GPU vendor from registry (same approach as GpuMonitorService)
        GpuVendor vendor = DetectGpuVendor();

        switch (vendor)
        {
            case GpuVendor.Nvidia:
                if (InitializeNvml())
                {
                    _gpuVendor = GpuVendor.Nvidia;
                }
                break;

            case GpuVendor.Amd:
                if (InitializeAdl())
                {
                    _gpuVendor = GpuVendor.Amd;
                }
                break;
        }
    }

    private static GpuVendor DetectGpuVendor()
    {
        try
        {
            using RegistryKey? displayAdaptersKey = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");

            if (displayAdaptersKey == null)
            {
                return GpuVendor.Unknown;
            }

            foreach (string subKeyName in displayAdaptersKey.GetSubKeyNames())
            {
                if (!subKeyName.All(char.IsDigit))
                {
                    continue;
                }

                try
                {
                    using RegistryKey? adapterKey = displayAdaptersKey.OpenSubKey(subKeyName);
                    if (adapterKey == null)
                    {
                        continue;
                    }

                    string description = adapterKey.GetValue("DriverDesc") as string ?? "";
                    string hardwareId = adapterKey.GetValue("MatchingDeviceId") as string ?? "";
                    string adapterInfo = $"{description} {hardwareId}".ToUpperInvariant();

                    if (adapterInfo.Contains("VEN_10DE") ||
                        adapterInfo.Contains("NVIDIA") ||
                        adapterInfo.Contains("GEFORCE") ||
                        adapterInfo.Contains("RTX") ||
                        adapterInfo.Contains("GTX"))
                    {
                        return GpuVendor.Nvidia;
                    }

                    if (adapterInfo.Contains("VEN_1002") ||
                        adapterInfo.Contains("RADEON RX") ||
                        adapterInfo.Contains("RADEON R9") ||
                        adapterInfo.Contains("RADEON R7"))
                    {
                        return GpuVendor.Amd;
                    }
                }
                catch
                {
                    // Skip this adapter
                }
            }
        }
        catch
        {
            // Registry access failed
        }

        return GpuVendor.Unknown;
    }

    private void DisposeGpuResources()
    {
        if (_nvmlLoaded)
        {
            try
            {
                NvmlShutdown();
            }
            catch { /* best effort */ }
            _nvmlLoaded = false;
        }

        if (_adlLoaded && _adlContext != nint.Zero)
        {
            try
            {
                ADL2_Main_Control_Destroy(_adlContext);
            }
            catch { /* best effort */ }
            _adlContext = nint.Zero;
            _adlLoaded = false;
        }

        _gpuInitialized = false;
        _gpuVendor = GpuVendor.Unknown;
    }

    #endregion

    #region NVIDIA NVML

    private bool InitializeNvml()
    {
        try
        {
            int result = NvmlInit();
            if (result != NVML_SUCCESS)
            {
                return false;
            }

            _nvmlLoaded = true;

            // Get handle to the first GPU device
            result = NvmlDeviceGetHandleByIndex(0, out _nvmlDeviceHandle);
            return result == NVML_SUCCESS;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    private double? GetNvidiaGpuTemperature()
    {
        if (!_nvmlLoaded || _nvmlDeviceHandle == nint.Zero)
        {
            return null;
        }

        try
        {
            int result = NvmlDeviceGetTemperature(_nvmlDeviceHandle, NVML_TEMPERATURE_GPU, out uint temperature);
            if (result == NVML_SUCCESS && temperature is > 0 and < 150)
            {
                return temperature;
            }
        }
        catch
        {
            // NVML call failed
        }

        return null;
    }

    #endregion

    #region AMD ADL

    private bool InitializeAdl()
    {
        try
        {
            int result = ADL2_Main_Control_Create(ADL_Main_Memory_Alloc_Callback, 1, out _adlContext);
            if (result != ADL_OK)
            {
                return false;
            }

            _adlLoaded = true;

            // Get the first active adapter index
            result = ADL2_Adapter_NumberOfAdapters_Get(_adlContext, out int numberOfAdapters);
            if (result != ADL_OK || numberOfAdapters == 0)
            {
                return false;
            }

            // Find the first active adapter
            for (int i = 0; i < numberOfAdapters; i++)
            {
                result = ADL2_Adapter_Active_Get(_adlContext, i, out int adapterActive);
                if (result == ADL_OK && adapterActive == 1)
                {
                    _adlAdapterIndex = i;
                    return true;
                }
            }

            return false;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    private double? GetAmdGpuTemperature()
    {
        if (!_adlLoaded || _adlContext == nint.Zero)
        {
            return null;
        }

        try
        {
            var thermalParams = new ADLTemperature();
            thermalParams.iSize = Marshal.SizeOf<ADLTemperature>();

            int result = ADL2_Overdrive5_Temperature_Get(_adlContext, _adlAdapterIndex, 0, ref thermalParams);
            if (result == ADL_OK)
            {
                // ADL returns temperature in millidegrees Celsius
                double celsius = thermalParams.iTemperature / 1000.0;
                if (celsius is > 0 and < 150)
                {
                    return Math.Round(celsius, 1);
                }
            }
        }
        catch
        {
            // ADL call failed
        }

        return null;
    }

    private static nint ADL_Main_Memory_Alloc_Callback(int size)
    {
        return Marshal.AllocHGlobal(size);
    }

    #endregion

    #region Enums

    private enum GpuVendor
    {
        Unknown,
        Nvidia,
        Amd
    }

    #endregion

    #region PDH P/Invoke

    private const uint PDH_FMT_DOUBLE = 512U;
    private const uint PDH_MORE_DATA = 0x800007D2;
    private const uint PDH_CSTATUS_VALID_DATA = 0;
    private const uint PERF_DETAIL_WIZARD = 400U;

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

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhOpenQuery(nint szDataSource, nint dwUserData, out nint phQuery);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhAddCounter(nint hQuery, string szFullCounterPath, nint dwUserData, out nint phCounter);

    [DllImport("pdh.dll")]
    private static extern uint PdhCollectQueryData(nint hQuery);

    [DllImport("pdh.dll")]
    private static extern uint PdhCloseQuery(nint hQuery);

    [DllImport("pdh.dll")]
    private static extern uint PdhGetFormattedCounterValue(nint hCounter, uint dwFormat, nint lpdwType, ref PdhFmtCounterValue pValue);

    [DllImport("pdh.dll")]
    private static extern uint PdhGetFormattedCounterArray(nint hCounter, uint dwFormat, ref uint lpdwBufferSize, ref uint lpdwItemCount, nint ItemBuffer);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhEnumObjectItems(
        string? szDataSource,
        string? szMachineName,
        string szObjectName,
        nint mszCounterList,
        ref uint pcchCounterListLength,
        nint mszInstanceList,
        ref uint pcchInstanceListLength,
        uint dwDetailLevel,
        uint dwFlags);

    #endregion

    #region NVML P/Invoke

    private const int NVML_SUCCESS = 0;
    private const uint NVML_TEMPERATURE_GPU = 0;

    [DllImport("nvml.dll", EntryPoint = "nvmlInit_v2")]
    private static extern int NvmlInit();

    [DllImport("nvml.dll", EntryPoint = "nvmlShutdown")]
    private static extern int NvmlShutdown();

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
    private static extern int NvmlDeviceGetHandleByIndex(uint index, out nint device);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetTemperature")]
    private static extern int NvmlDeviceGetTemperature(nint device, uint sensorType, out uint temp);

    #endregion

    #region AMD ADL P/Invoke

    private const int ADL_OK = 0;

    private delegate nint ADL_Main_Memory_Alloc(int size);

    [StructLayout(LayoutKind.Sequential)]
    private struct ADLTemperature
    {
        public int iSize;
        public int iTemperature;
    }

    [DllImport("atiadlxx.dll")]
    private static extern int ADL2_Main_Control_Create(ADL_Main_Memory_Alloc callback, int enumConnectedAdapters, out nint context);

    [DllImport("atiadlxx.dll")]
    private static extern int ADL2_Main_Control_Destroy(nint context);

    [DllImport("atiadlxx.dll")]
    private static extern int ADL2_Adapter_NumberOfAdapters_Get(nint context, out int numAdapters);

    [DllImport("atiadlxx.dll")]
    private static extern int ADL2_Adapter_Active_Get(nint context, int adapterIndex, out int status);

    [DllImport("atiadlxx.dll")]
    private static extern int ADL2_Overdrive5_Temperature_Get(nint context, int adapterIndex, int thermalControllerIndex, ref ADLTemperature temperature);

    #endregion
}
