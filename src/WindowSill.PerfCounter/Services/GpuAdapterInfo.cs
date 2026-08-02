namespace WindowSill.PerfCounter.Services;

internal enum GpuVendor
{
    Unknown,
    Nvidia,
    Amd,
    Intel
}

internal sealed record GpuAdapterInfo(
    long Luid,
    uint Index,
    string Name,
    uint VendorId,
    uint DeviceId,
    long DedicatedMemoryBytes,
    long SharedMemoryBytes,
    bool IsSoftware)
{
    internal GpuVendor Vendor => VendorId switch
    {
        0x10DE => GpuVendor.Nvidia,
        0x1002 => GpuVendor.Amd,
        0x8086 => GpuVendor.Intel,
        _ => GpuVendor.Unknown
    };

    internal GpuHardwareInfo ToHardwareInfo() =>
        new(
            Name,
            DedicatedMemoryBytes > 0
                ? DedicatedMemoryBytes / (1024 * 1024)
                : null)
        {
            AdapterLuid = Luid
        };
}
