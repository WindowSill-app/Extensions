using Microsoft.Extensions.Logging;

using Windows.Win32;
using Windows.Win32.Graphics.Dxgi;

using WindowSill.API;

namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Resolves GPU metadata by the adapter LUID used in GPU Engine counters.
/// </summary>
internal static class GpuAdapterCatalog
{
    private static readonly ILogger Logger = typeof(GpuAdapterCatalog).Log();

    internal static unsafe IReadOnlyDictionary<long, GpuAdapterInfo> GetAdaptersByLuid()
    {
        var adapters = new Dictionary<long, GpuAdapterInfo>();
        IDXGIFactory1* factory = null;

        try
        {
            if (PInvoke.CreateDXGIFactory1(
                    IDXGIFactory1.IID_Guid,
                    out void* factoryPointer).Failed ||
                factoryPointer is null)
            {
                return adapters;
            }

            factory = (IDXGIFactory1*)factoryPointer;

            for (uint index = 0; ; index++)
            {
                IDXGIAdapter1* adapter = null;
                if (factory->EnumAdapters1(index, &adapter).Failed || adapter is null)
                {
                    break;
                }

                try
                {
                    DXGI_ADAPTER_DESC1 description = default;
                    if (adapter->GetDesc1(&description).Failed)
                    {
                        continue;
                    }

                    long luid =
                        ((long)(uint)description.AdapterLuid.HighPart << 32) |
                        description.AdapterLuid.LowPart;
                    bool isSoftware =
                        (description.Flags & DXGI_ADAPTER_FLAG.DXGI_ADAPTER_FLAG_SOFTWARE) != 0;

                    adapters[luid] = new GpuAdapterInfo(
                        luid,
                        index,
                        description.Description.ToString(),
                        description.VendorId,
                        description.DeviceId,
                        checked((long)description.DedicatedVideoMemory),
                        checked((long)description.SharedSystemMemory),
                        isSoftware);
                }
                finally
                {
                    adapter->Release();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Unable to enumerate DXGI GPU adapters.");
        }
        finally
        {
            if (factory is not null)
            {
                factory->Release();
            }
        }

        return adapters;
    }

    internal static IReadOnlyList<GpuAdapterInfo> GetDisplayableAdapters(
        IReadOnlyDictionary<long, GpuAdapterInfo> adapters)
    {
        GpuAdapterInfo[] hardwareAdapters = adapters.Values
            .Where(adapter => !adapter.IsSoftware)
            .OrderBy(adapter => adapter.Index)
            .ToArray();

        return hardwareAdapters.Length > 0
            ? hardwareAdapters
            : adapters.Values.OrderBy(adapter => adapter.Index).ToArray();
    }
}
