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
            try
            {
                using RegistryKey? displayKey = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");

                if (displayKey is null)
                {
                    return null;
                }

                foreach (string subKeyName in displayKey.GetSubKeyNames())
                {
                    if (!subKeyName.All(char.IsDigit))
                    {
                        continue;
                    }

                    using RegistryKey? adapterKey = displayKey.OpenSubKey(subKeyName);
                    if (adapterKey is null)
                    {
                        continue;
                    }

                    string? description = adapterKey.GetValue("DriverDesc") as string;
                    if (string.IsNullOrWhiteSpace(description))
                    {
                        continue;
                    }

                    // Try to get VRAM size from various registry values
                    long? vramBytes = GetRegistryQwordOrDword(adapterKey, "HardwareInformation.qwMemorySize")
                        ?? GetRegistryQwordOrDword(adapterKey, "HardwareInformation.MemorySize");

                    long? vramMB = vramBytes.HasValue ? vramBytes.Value / (1024 * 1024) : null;

                    // Return the first adapter with a description (typically the primary GPU)
                    return new GpuHardwareInfo(description.Trim(), vramMB);
                }
            }
            catch
            {
                // Registry access failed
            }

            return null;
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
        int threads = Environment.ProcessorCount;

        // Count physical cores by enumerating processor core registry keys
        int cores = 0;
        try
        {
            using RegistryKey? cpuKey = Registry.LocalMachine.OpenSubKey(
                @"HARDWARE\DESCRIPTION\System\CentralProcessor");
            if (cpuKey is not null)
            {
                // Each subkey is a logical processor
                int logicalCount = cpuKey.GetSubKeyNames().Length;
                // Estimate physical cores (common heuristic when WMI is unavailable)
                // If HT/SMT is enabled, typically threads = 2 * cores
                cores = logicalCount;
                threads = logicalCount;
            }
        }
        catch
        {
            cores = threads;
        }

        // Use GetLogicalProcessorInformation for accurate core count
        try
        {
            uint returnLength = 0;
            PInvoke.GetLogicalProcessorInformation(Span<SYSTEM_LOGICAL_PROCESSOR_INFORMATION>.Empty, ref returnLength);
            int structSize = Marshal.SizeOf<SYSTEM_LOGICAL_PROCESSOR_INFORMATION>();
            int count = (int)returnLength / structSize;
            var buffer = new SYSTEM_LOGICAL_PROCESSOR_INFORMATION[count];

            if (PInvoke.GetLogicalProcessorInformation(buffer, ref returnLength))
            {
                int physicalCores = 0;
                for (int i = 0; i < count; i++)
                {
                    if (buffer[i].Relationship == LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore)
                    {
                        physicalCores++;
                    }
                }

                if (physicalCores > 0)
                {
                    cores = physicalCores;
                }
            }
        }
        catch
        {
            // Fall back to the registry-based count
        }

        return (cores, threads);
    }

    private static long? GetRegistryQwordOrDword(RegistryKey key, string valueName)
    {
        object? value = key.GetValue(valueName);
        return value switch
        {
            long l => l,
            int i => i,
            byte[] bytes when bytes.Length >= 8 => BitConverter.ToInt64(bytes, 0),
            byte[] bytes when bytes.Length >= 4 => BitConverter.ToInt32(bytes, 0),
            _ => null
        };
    }
}
