using Microsoft.Win32;

namespace WindowSill.PerfCounter.Services;

/// <summary>
/// Detects GPU adapters from the Windows registry.
/// </summary>
internal static class GpuDetector
{
    private const string DisplayAdaptersRegistryPath =
        @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

    /// <summary>
    /// GPU vendor classification.
    /// </summary>
    internal enum GpuVendor
    {
        Unknown,
        Nvidia,
        Amd
    }

    /// <summary>
    /// Information about a detected GPU adapter.
    /// </summary>
    internal record GpuAdapterInfo(
        string Description,
        string HardwareId,
        GpuVendor Vendor,
        bool IsDedicated);

    /// <summary>
    /// Enumerates GPU adapters from the registry, returning info about each.
    /// </summary>
    internal static List<GpuAdapterInfo> GetAdapters()
    {
        var adapters = new List<GpuAdapterInfo>();

        try
        {
            using RegistryKey? displayAdaptersKey = Registry.LocalMachine.OpenSubKey(DisplayAdaptersRegistryPath);
            if (displayAdaptersKey == null)
            {
                return adapters;
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

                    GpuVendor vendor = ClassifyVendor(adapterInfo);
                    bool isDedicated = IsDedicatedGpu(adapterInfo);

                    adapters.Add(new GpuAdapterInfo(description, hardwareId, vendor, isDedicated));
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

        return adapters;
    }

    /// <summary>
    /// Returns true if any dedicated GPU is present.
    /// </summary>
    internal static bool HasDedicatedGpu()
    {
        return GetAdapters().Any(a => a.IsDedicated);
    }

    /// <summary>
    /// Returns the vendor of the first dedicated GPU, or <see cref="GpuVendor.Unknown"/>.
    /// </summary>
    internal static GpuVendor DetectDedicatedGpuVendor()
    {
        GpuAdapterInfo? dedicated = GetAdapters().FirstOrDefault(a => a.IsDedicated);
        return dedicated?.Vendor ?? GpuVendor.Unknown;
    }

    private static GpuVendor ClassifyVendor(string adapterInfo)
    {
        if (adapterInfo.Contains("VEN_10DE") ||
            adapterInfo.Contains("NVIDIA") ||
            adapterInfo.Contains("GEFORCE") ||
            adapterInfo.Contains("RTX") ||
            adapterInfo.Contains("GTX") ||
            adapterInfo.Contains("QUADRO") ||
            adapterInfo.Contains("TESLA"))
        {
            return GpuVendor.Nvidia;
        }

        if (adapterInfo.Contains("VEN_1002") ||
            adapterInfo.Contains("AMD RADEON RX") ||
            adapterInfo.Contains("RADEON RX") ||
            adapterInfo.Contains("RADEON R9") ||
            adapterInfo.Contains("RADEON R7") ||
            adapterInfo.Contains("RADEON HD 7") ||
            adapterInfo.Contains("RADEON HD 6") ||
            adapterInfo.Contains("RADEON HD 5"))
        {
            return GpuVendor.Amd;
        }

        return GpuVendor.Unknown;
    }

    private static bool IsDedicatedGpu(string adapterInfo)
    {
        string[] dedicatedKeywords =
        [
            "NVIDIA", "GEFORCE", "QUADRO", "TESLA", "RTX", "GTX",
            "AMD RADEON RX", "AMD RADEON R9", "AMD RADEON R7",
            "RADEON RX", "RADEON R9", "RADEON R7",
            "RADEON HD 7", "RADEON HD 6", "RADEON HD 5"
        ];

        foreach (string keyword in dedicatedKeywords)
        {
            if (adapterInfo.Contains(keyword))
            {
                return true;
            }
        }

        // Vendor ID check
        if (adapterInfo.Contains("VEN_10DE") || // NVIDIA
            (adapterInfo.Contains("VEN_1002") && !adapterInfo.Contains("RADEON GRAPHICS"))) // AMD (but not APU)
        {
            return true;
        }

        return false;
    }
}
