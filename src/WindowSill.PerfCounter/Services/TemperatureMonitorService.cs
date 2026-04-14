using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

using WindowSill.PerfCounter.Services.Interop;

namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Monitors CPU and GPU temperatures using no-admin-required APIs.
/// CPU temperature is read via PDH thermal zone counters.
/// GPU temperature is read via NVIDIA NVML or AMD ADL, depending on the detected vendor.
/// </summary>
[Export(typeof(ITemperatureMonitorService))]
internal sealed class TemperatureMonitorService : ITemperatureMonitorService
{
    private readonly Lock _lock = new();

    // CPU temperature via PDH
    private nint _cpuThermalQuery = nint.Zero;
    private readonly List<nint> _cpuThermalCounters = [];
    private bool _cpuInitialized;

    // GPU temperature via vendor APIs
    private GpuDetector.GpuVendor _gpuVendor = GpuDetector.GpuVendor.Unknown;
    private bool _gpuInitialized;

    // NVML state
    private bool _nvmlLoaded;
    private nint _nvmlDeviceHandle;

    // ADL state
    private bool _adlLoaded;
    private nint _adlContext;
    private int _adlAdapterIndex;

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
                GpuDetector.GpuVendor.Nvidia => GetNvidiaGpuTemperature(),
                GpuDetector.GpuVendor.Amd => GetAmdGpuTemperature(),
                _ => null
            };
        }
    }

    /// <inheritdoc/>
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
                PdhInterop.PdhCloseQuery(_cpuThermalQuery);
                _cpuThermalQuery = nint.Zero;
            }

            _cpuThermalCounters.Clear();

            uint status = PdhInterop.PdhOpenQuery(nint.Zero, nint.Zero, out _cpuThermalQuery);
            if (status != 0)
            {
                return false;
            }

            // Try wildcard counter for all thermal zones
            status = PdhInterop.PdhAddCounter(
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
            List<string> instances = PdhInterop.EnumerateObjectInstances("Thermal Zone Information");
            foreach (string instance in instances)
            {
                string counterPath = $@"\Thermal Zone Information({instance})\Temperature";
                uint addStatus = PdhInterop.PdhAddCounter(_cpuThermalQuery, counterPath, nint.Zero, out nint instanceCounter);
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
            PdhInterop.PdhCollectQueryData(_cpuThermalQuery);

            double maxTemperature = double.MinValue;
            bool anyValid = false;

            foreach (nint counter in _cpuThermalCounters)
            {
                (double total, int count) = PdhInterop.CollectFormattedCounterArray(counter);
                if (count > 0)
                {
                    // PDH thermal zone values are in Kelvin; convert the max reading.
                    // CollectFormattedCounterArray returns the sum; for temperature we want individual values.
                    // Re-read individual values for temperature to get the max.
                    double celsius = GetMaxTemperatureFromCounter(counter);
                    if (celsius > maxTemperature)
                    {
                        maxTemperature = celsius;
                        anyValid = true;
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

    /// <summary>
    /// Reads all values from a temperature counter and returns the max Celsius value.
    /// Temperature counters report in Kelvin, so we subtract 273.15.
    /// </summary>
    private static double GetMaxTemperatureFromCounter(nint counter)
    {
        double maxCelsius = double.MinValue;

        uint bufferSize = 0u;
        uint itemCount = 0u;

        uint status = PdhInterop.PdhGetFormattedCounterArray(
            counter, PdhInterop.PDH_FMT_DOUBLE,
            ref bufferSize, ref itemCount, nint.Zero);

        if (status == PdhInterop.PDH_MORE_DATA && itemCount > 0)
        {
            nint buffer = Marshal.AllocHGlobal((int)bufferSize);
            try
            {
                status = PdhInterop.PdhGetFormattedCounterArray(
                    counter, PdhInterop.PDH_FMT_DOUBLE,
                    ref bufferSize, ref itemCount, buffer);

                if (status == 0)
                {
                    nint current = buffer;
                    for (int i = 0; i < itemCount; i++)
                    {
                        PdhInterop.PdhFmtCounterValueItemDouble item = Marshal.PtrToStructure<PdhInterop.PdhFmtCounterValueItemDouble>(current);
                        if (item.FmtValue.CStatus == PdhInterop.PDH_CSTATUS_VALID_DATA)
                        {
                            double celsius = item.FmtValue.DoubleValue - 273.15;
                            if (celsius > maxCelsius)
                            {
                                maxCelsius = celsius;
                            }
                        }

                        current = IntPtr.Add(current, Marshal.SizeOf<PdhInterop.PdhFmtCounterValueItemDouble>());
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
            var counterValue = new PdhInterop.PdhFmtCounterValue();
            uint singleStatus = PdhInterop.PdhGetFormattedCounterValue(
                counter, PdhInterop.PDH_FMT_DOUBLE, nint.Zero, ref counterValue);

            if (singleStatus == 0 && counterValue.CStatus == PdhInterop.PDH_CSTATUS_VALID_DATA)
            {
                double celsius = counterValue.DoubleValue - 273.15;
                if (celsius > maxCelsius)
                {
                    maxCelsius = celsius;
                }
            }
        }

        return maxCelsius;
    }

    private void DisposeCpuResources()
    {
        if (_cpuThermalQuery != nint.Zero)
        {
            PdhInterop.PdhCloseQuery(_cpuThermalQuery);
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

        GpuDetector.GpuVendor vendor = GpuDetector.DetectDedicatedGpuVendor();

        switch (vendor)
        {
            case GpuDetector.GpuVendor.Nvidia:
                if (InitializeNvml())
                {
                    _gpuVendor = GpuDetector.GpuVendor.Nvidia;
                }

                break;

            case GpuDetector.GpuVendor.Amd:
                if (InitializeAdl())
                {
                    _gpuVendor = GpuDetector.GpuVendor.Amd;
                }

                break;
        }
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
        _gpuVendor = GpuDetector.GpuVendor.Unknown;
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

            result = ADL2_Adapter_NumberOfAdapters_Get(_adlContext, out int numberOfAdapters);
            if (result != ADL_OK || numberOfAdapters == 0)
            {
                return false;
            }

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
