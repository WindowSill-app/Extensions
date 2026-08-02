using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Logging;

using WindowSill.API;
using WindowSill.PerfCounter.Services.Interop;

namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Monitors CPU-identified ACPI thermal zones and supported GPU vendor sensors.
/// </summary>
[Export(typeof(ITemperatureMonitorService))]
internal sealed class TemperatureMonitorService : ITemperatureMonitorService
{
    private static readonly string[] CpuTemperatureCounterPaths =
    [
        @"\Thermal Zone Information(*)\Temperature"
    ];

    private static readonly ILogger Logger = typeof(TemperatureMonitorService).Log();

    private readonly Lock _lock = new();

    private PdhCounterQuery? _cpuQuery;
    private DateTimeOffset _cpuRetryAfter;
    private bool _cpuInitializationFailureLogged;
    private bool _cpuReadFailureLogged;

    private long? _gpuAdapterLuid;
    private GpuVendor _gpuVendor = GpuVendor.Unknown;
    private DateTimeOffset _gpuRetryAfter;
    private bool _gpuInitializationFailureLogged;

    private bool _nvmlLoaded;
    private nint _nvmlDeviceHandle;

    private bool _adlLoaded;
    private nint _adlContext;
    private int _adlAdapterIndex;
    private bool _isMonitoring;

    public void StartMonitoring()
    {
        lock (_lock)
        {
            _isMonitoring = true;
            _cpuRetryAfter = DateTimeOffset.MinValue;
            EnsureCpuQuery();
        }
    }

    public void StopMonitoring()
    {
        lock (_lock)
        {
            _isMonitoring = false;
            DisposeCpuResources();
            DisposeGpuResources();
        }
    }

    public double? GetCpuTemperature()
    {
        lock (_lock)
        {
            if (!_isMonitoring)
            {
                return null;
            }

            if (!EnsureCpuQuery() || _cpuQuery is null)
            {
                return null;
            }

            if (!_cpuQuery.Collect())
            {
                HandleCpuReadFailure("PDH failed to collect thermal-zone data.");
                return null;
            }

            IReadOnlyList<PdhCounterValue> thermalZones =
                _cpuQuery.GetFormattedCounterArray(0);
            if (thermalZones.Count == 0)
            {
                HandleCpuReadFailure("PDH returned no valid thermal-zone data.");
                return null;
            }

            _cpuReadFailureLogged = false;
            return ThermalZoneSelector.GetCpuTemperature(thermalZones);
        }
    }

    public double? GetGpuTemperature(GpuAdapterInfo? adapter)
    {
        lock (_lock)
        {
            if (!_isMonitoring)
            {
                return null;
            }

            if (adapter is null || adapter.Vendor is GpuVendor.Unknown or GpuVendor.Intel)
            {
                DisposeGpuResources();
                return null;
            }

            if (_gpuAdapterLuid != adapter.Luid)
            {
                DisposeGpuResources();
                _gpuAdapterLuid = adapter.Luid;
                _gpuVendor = adapter.Vendor;
                _gpuRetryAfter = DateTimeOffset.MinValue;
            }

            if (!EnsureGpuTemperatureInitialized(adapter))
            {
                return null;
            }

            return _gpuVendor switch
            {
                GpuVendor.Nvidia => GetNvidiaGpuTemperature(),
                GpuVendor.Amd => GetAmdGpuTemperature(),
                _ => null
            };
        }
    }

    public void Dispose()
    {
        StopMonitoring();
    }

    private bool EnsureCpuQuery()
    {
        if (!_isMonitoring)
        {
            return false;
        }

        if (_cpuQuery is not null)
        {
            return true;
        }

        if (DateTimeOffset.UtcNow < _cpuRetryAfter)
        {
            return false;
        }

        if (!PdhCounterQuery.TryCreate(
                CpuTemperatureCounterPaths,
                out PdhCounterQuery? query,
                out uint status) ||
            query is null)
        {
            if (!_cpuInitializationFailureLogged)
            {
                Logger.LogWarning(
                    "Unable to initialize thermal-zone counters. PDH status: 0x{Status:X8}.",
                    status);
                _cpuInitializationFailureLogged = true;
            }

            _cpuRetryAfter = DateTimeOffset.UtcNow.AddSeconds(30);
            return false;
        }

        if (!query.Collect())
        {
            query.Dispose();
            HandleCpuInitializationFailure("Unable to prime thermal-zone counters.");
            return false;
        }

        _cpuQuery = query;
        _cpuInitializationFailureLogged = false;
        _cpuReadFailureLogged = false;
        return true;
    }

    private void HandleCpuInitializationFailure(string message)
    {
        if (!_cpuInitializationFailureLogged)
        {
            Logger.LogWarning("{Message}", message);
            _cpuInitializationFailureLogged = true;
        }

        _cpuRetryAfter = DateTimeOffset.UtcNow.AddSeconds(30);
    }

    private void HandleCpuReadFailure(string message)
    {
        if (!_cpuReadFailureLogged)
        {
            Logger.LogWarning("{Message}", message);
            _cpuReadFailureLogged = true;
        }

        DisposeCpuResources();
        _cpuRetryAfter = DateTimeOffset.UtcNow.AddSeconds(5);
    }

    private bool EnsureGpuTemperatureInitialized(GpuAdapterInfo adapter)
    {
        if (_gpuVendor == adapter.Vendor &&
            (_nvmlLoaded || _adlLoaded))
        {
            return true;
        }

        if (DateTimeOffset.UtcNow < _gpuRetryAfter)
        {
            return false;
        }

        bool initialized = adapter.Vendor switch
        {
            GpuVendor.Nvidia => InitializeNvml(),
            GpuVendor.Amd => InitializeAdl(),
            _ => false
        };

        if (initialized)
        {
            _gpuInitializationFailureLogged = false;
            return true;
        }

        DisposeGpuApiResources();
        _gpuAdapterLuid = adapter.Luid;
        _gpuVendor = adapter.Vendor;
        _gpuRetryAfter = DateTimeOffset.UtcNow.AddSeconds(30);

        if (!_gpuInitializationFailureLogged)
        {
            Logger.LogWarning(
                "Unable to initialize the temperature sensor for GPU adapter {AdapterName}.",
                adapter.Name);
            _gpuInitializationFailureLogged = true;
        }

        return false;
    }

    private bool InitializeNvml()
    {
        try
        {
            if (NvmlInit() != NvmlSuccess)
            {
                return false;
            }

            _nvmlLoaded = true;
            return NvmlDeviceGetHandleByIndex(0, out _nvmlDeviceHandle) == NvmlSuccess;
        }
        catch (Exception ex) when (
            ex is DllNotFoundException or
                EntryPointNotFoundException or
                BadImageFormatException)
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

        int result = NvmlDeviceGetTemperature(
            _nvmlDeviceHandle,
            NvmlTemperatureGpu,
            out uint temperature);

        return result == NvmlSuccess && temperature is > 0 and < 150
            ? temperature
            : null;
    }

    private bool InitializeAdl()
    {
        try
        {
            int result = Adl2MainControlCreate(
                AdlMainMemoryAllocCallback,
                1,
                out _adlContext);
            if (result != AdlOk)
            {
                return false;
            }

            _adlLoaded = true;

            result = Adl2AdapterNumberOfAdaptersGet(_adlContext, out int adapterCount);
            if (result != AdlOk || adapterCount == 0)
            {
                return false;
            }

            for (int index = 0; index < adapterCount; index++)
            {
                result = Adl2AdapterActiveGet(_adlContext, index, out int isActive);
                if (result == AdlOk && isActive == 1)
                {
                    _adlAdapterIndex = index;
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex) when (
            ex is DllNotFoundException or
                EntryPointNotFoundException or
                BadImageFormatException)
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

        var temperature = new AdlTemperature
        {
            Size = Marshal.SizeOf<AdlTemperature>()
        };

        int result = Adl2Overdrive5TemperatureGet(
            _adlContext,
            _adlAdapterIndex,
            0,
            ref temperature);
        if (result != AdlOk)
        {
            return null;
        }

        double celsius = temperature.Temperature / 1000.0;
        return celsius is > 0 and < 150
            ? Math.Round(celsius, 1)
            : null;
    }

    private void DisposeCpuResources()
    {
        _cpuQuery?.Dispose();
        _cpuQuery = null;
    }

    private void DisposeGpuResources()
    {
        DisposeGpuApiResources();
        _gpuAdapterLuid = null;
        _gpuVendor = GpuVendor.Unknown;
        _gpuRetryAfter = DateTimeOffset.MinValue;
        _gpuInitializationFailureLogged = false;
    }

    private void DisposeGpuApiResources()
    {
        if (_nvmlLoaded)
        {
            NvmlShutdown();
            _nvmlLoaded = false;
            _nvmlDeviceHandle = nint.Zero;
        }

        if (_adlLoaded && _adlContext != nint.Zero)
        {
            Adl2MainControlDestroy(_adlContext);
            _adlContext = nint.Zero;
        }

        _adlLoaded = false;
        _adlAdapterIndex = 0;
    }

    private static nint AdlMainMemoryAllocCallback(int size)
    {
        return Marshal.AllocHGlobal(size);
    }

    private const int NvmlSuccess = 0;
    private const uint NvmlTemperatureGpu = 0;
    private const int AdlOk = 0;

    private delegate nint AdlMainMemoryAlloc(int size);

    [StructLayout(LayoutKind.Sequential)]
    private struct AdlTemperature
    {
        public int Size;
        public int Temperature;
    }

    [DllImport("nvml.dll", EntryPoint = "nvmlInit_v2")]
    private static extern int NvmlInit();

    [DllImport("nvml.dll", EntryPoint = "nvmlShutdown")]
    private static extern int NvmlShutdown();

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
    private static extern int NvmlDeviceGetHandleByIndex(uint index, out nint device);

    [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetTemperature")]
    private static extern int NvmlDeviceGetTemperature(
        nint device,
        uint sensorType,
        out uint temperature);

    [DllImport("atiadlxx.dll", EntryPoint = "ADL2_Main_Control_Create")]
    private static extern int Adl2MainControlCreate(
        AdlMainMemoryAlloc callback,
        int enumerateConnectedAdapters,
        out nint context);

    [DllImport("atiadlxx.dll", EntryPoint = "ADL2_Main_Control_Destroy")]
    private static extern int Adl2MainControlDestroy(nint context);

    [DllImport("atiadlxx.dll", EntryPoint = "ADL2_Adapter_NumberOfAdapters_Get")]
    private static extern int Adl2AdapterNumberOfAdaptersGet(
        nint context,
        out int adapterCount);

    [DllImport("atiadlxx.dll", EntryPoint = "ADL2_Adapter_Active_Get")]
    private static extern int Adl2AdapterActiveGet(
        nint context,
        int adapterIndex,
        out int isActive);

    [DllImport("atiadlxx.dll", EntryPoint = "ADL2_Overdrive5_Temperature_Get")]
    private static extern int Adl2Overdrive5TemperatureGet(
        nint context,
        int adapterIndex,
        int thermalControllerIndex,
        ref AdlTemperature temperature);
}
