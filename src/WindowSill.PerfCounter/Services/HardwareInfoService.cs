using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

using Microsoft.Win32;

using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Provides static hardware information using Windows APIs and registry.
/// </summary>
[Export(typeof(IHardwareInfoService))]
internal sealed class HardwareInfoService : IHardwareInfoService
{
    /// <inheritdoc/>
    public Task<CpuHardwareInfo?> GetCpuInfoAsync()
    {
        return Task.Run<CpuHardwareInfo?>(() =>
        {
            try
            {
                string? cpuName = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0",
                    "ProcessorNameString",
                    null) as string;

                int? cpuMHz = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0",
                    "~MHz",
                    null) as int?;

                if (string.IsNullOrWhiteSpace(cpuName))
                {
                    return null;
                }

                // Count physical cores and logical processors
                (int cores, int threads) = GetCpuCoreInfo();

                return new CpuHardwareInfo(
                    cpuName.Trim(),
                    cores,
                    threads,
                    cpuMHz ?? 0);
            }
            catch
            {
                return null;
            }
        });
    }

    /// <inheritdoc/>
    public Task<GpuHardwareInfo?> GetGpuInfoAsync()
    {
        return Task.Run<GpuHardwareInfo?>(() =>
        {
            IReadOnlyDictionary<long, GpuAdapterInfo> adapters =
                GpuAdapterCatalog.GetAdaptersByLuid();
            IReadOnlyList<GpuAdapterInfo> displayableAdapters =
                GpuAdapterCatalog.GetDisplayableAdapters(adapters);

            return displayableAdapters.Count == 1
                ? displayableAdapters[0].ToHardwareInfo()
                : null;
        });
    }

    /// <inheritdoc/>
    public MemoryHardwareInfo GetMemoryInfo()
    {
        var memoryStatus = new MEMORYSTATUSEX
        {
            dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
        };

        if (PInvoke.GlobalMemoryStatusEx(ref memoryStatus))
        {
            long totalMB = (long)(memoryStatus.ullTotalPhys / (1024 * 1024));
            return new MemoryHardwareInfo(totalMB);
        }

        return new MemoryHardwareInfo(0);
    }

    private static (int Cores, int Threads) GetCpuCoreInfo()
    {
        uint activeProcessorCount = GetActiveProcessorCount(AllProcessorGroups);
        int threads = activeProcessorCount is > 0 and <= int.MaxValue
            ? (int)activeProcessorCount
            : Environment.ProcessorCount;
        int cores = 0;

        try
        {
            uint returnLength = 0;
            GetLogicalProcessorInformationEx(
                LogicalProcessorRelationshipProcessorCore,
                nint.Zero,
                ref returnLength);

            if (returnLength > 0)
            {
                nint buffer = Marshal.AllocHGlobal(checked((int)returnLength));
                try
                {
                    if (GetLogicalProcessorInformationEx(
                            LogicalProcessorRelationshipProcessorCore,
                            buffer,
                            ref returnLength))
                    {
                        uint offset = 0;
                        while (offset + 8 <= returnLength)
                        {
                            nint entry = IntPtr.Add(buffer, checked((int)offset));
                            int relationship = Marshal.ReadInt32(entry);
                            uint size = unchecked((uint)Marshal.ReadInt32(entry, 4));
                            if (size < 8 || offset + size > returnLength)
                            {
                                break;
                            }

                            if (relationship == LogicalProcessorRelationshipProcessorCore)
                            {
                                cores++;
                            }

                            offset += size;
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }
        catch
        {
            cores = 0;
        }

        if (cores == 0)
        {
            cores = threads;
        }

        return (cores, threads);
    }

    private const ushort AllProcessorGroups = ushort.MaxValue;
    private const int LogicalProcessorRelationshipProcessorCore = 0;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetActiveProcessorCount(ushort groupNumber);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLogicalProcessorInformationEx(
        int relationshipType,
        nint buffer,
        ref uint returnedLength);
}
