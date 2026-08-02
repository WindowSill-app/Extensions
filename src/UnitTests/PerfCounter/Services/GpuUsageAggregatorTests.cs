using FluentAssertions;

using WindowSill.PerfCounter.Services;
using WindowSill.PerfCounter.Services.Interop;

namespace UnitTests.PerfCounter.Services;

public class GpuUsageAggregatorTests
{
    [Fact]
    internal void Create_SumsProcessesPerEngine_ThenUsesBusiestEngine()
    {
        IReadOnlyDictionary<long, GpuAdapterInfo> adapters =
            CreateAdapters(CreateAdapter(1, 0x10DE, "Discrete GPU"));
        PdhCounterValue[] values =
        [
            Counter(10, 1, 0, 0, "3D", 35),
            Counter(20, 1, 0, 0, "3D", 25),
            Counter(30, 1, 0, 1, "Compute", 70)
        ];

        GpuPerformanceData? result =
            GpuUsageAggregator.Create(values, adapters);

        result.Should().NotBeNull();
        result!.OverallUsage.Should().Be(70);
        result.Adapters.Single().Usage.Should().Be(70);
        result.UnambiguousAdapter.Should().Be(adapters[1]);
    }

    [Fact]
    internal void Create_UsesBusiestAdapter_WithoutCombiningAdapters()
    {
        IReadOnlyDictionary<long, GpuAdapterInfo> adapters = CreateAdapters(
            CreateAdapter(1, 0x8086, "Integrated GPU"),
            CreateAdapter(2, 0x10DE, "Discrete GPU"));
        PdhCounterValue[] values =
        [
            Counter(10, 1, 0, 0, "3D", 45),
            Counter(20, 2, 0, 0, "3D", 80)
        ];

        GpuPerformanceData? result =
            GpuUsageAggregator.Create(values, adapters);

        result.Should().NotBeNull();
        result!.OverallUsage.Should().Be(80);
        result.Adapters.Should().HaveCount(2);
        result.UnambiguousAdapter.Should().BeNull();
    }

    [Fact]
    internal void Create_IncludesIntegratedGpu_WhenItIsTheOnlyAdapter()
    {
        GpuAdapterInfo integrated = CreateAdapter(7, 0x8086, "Integrated GPU");

        GpuPerformanceData? result = GpuUsageAggregator.Create(
            [Counter(10, 7, 0, 0, "3D", 32)],
            CreateAdapters(integrated));

        result.Should().NotBeNull();
        result!.OverallUsage.Should().Be(32);
        result.UnambiguousAdapter.Should().Be(integrated);
        result.UnambiguousAdapter!.Vendor.Should().Be(GpuVendor.Intel);
    }

    [Fact]
    internal void Create_ClampsOneEngineAfterSummingItsProcesses()
    {
        GpuPerformanceData? result = GpuUsageAggregator.Create(
            [
                Counter(10, 1, 0, 0, "3D", 80),
                Counter(20, 1, 0, 0, "3D", 60)
            ],
            CreateAdapters(CreateAdapter(1, 0x10DE, "GPU")));

        result.Should().NotBeNull();
        result!.OverallUsage.Should().Be(100);
    }

    [Fact]
    internal void Create_PreservesUnknownObservedAdapters()
    {
        GpuPerformanceData? result = GpuUsageAggregator.Create(
            [Counter(10, 2, 0, 0, "3D", 75)],
            CreateAdapters(CreateAdapter(1, 0x10DE, "Known GPU")));

        result.Should().NotBeNull();
        result!.OverallUsage.Should().Be(75);
        result.Adapters.Select(adapter => adapter.Luid).Should().BeEquivalentTo([1L, 2L]);
        result.UnambiguousAdapter.Should().BeNull();
    }

    [Fact]
    internal void Create_ReportsIdleGpuWhenDxgiMetadataIsUnavailable()
    {
        GpuPerformanceData? result = GpuUsageAggregator.Create(
            [Counter(10, 1, 0, 0, "3D", 0)],
            new Dictionary<long, GpuAdapterInfo>());

        result.Should().NotBeNull();
        result!.OverallUsage.Should().Be(0);
        result.Adapters.Single().Luid.Should().Be(1);
    }

    [Fact]
    internal void Create_CreatesIdleSnapshotFromKnownAdapters()
    {
        GpuAdapterInfo adapter = CreateAdapter(1, 0x8086, "Integrated GPU");

        GpuPerformanceData? result = GpuUsageAggregator.Create(
            [],
            CreateAdapters(adapter));

        result.Should().NotBeNull();
        result!.OverallUsage.Should().Be(0);
        result.UnambiguousAdapter.Should().Be(adapter);
    }

    [Fact]
    internal void Create_IgnoresMalformedAndNegativeValues()
    {
        GpuPerformanceData? result = GpuUsageAggregator.Create(
            [
                new PdhCounterValue("malformed", 90),
                Counter(10, 1, 0, 0, "3D", -1)
            ],
            CreateAdapters(CreateAdapter(1, 0x10DE, "GPU")));

        result.Should().BeNull();
    }

    private static PdhCounterValue Counter(
        int processId,
        long luid,
        int physicalAdapter,
        int engine,
        string engineType,
        double value)
    {
        uint highPart = unchecked((uint)(luid >> 32));
        uint lowPart = unchecked((uint)luid);
        string name =
            $"pid_{processId}_luid_0x{highPart:X8}_0x{lowPart:X8}" +
            $"_phys_{physicalAdapter}_eng_{engine}_engtype_{engineType}";
        return new PdhCounterValue(name, value);
    }

    private static GpuAdapterInfo CreateAdapter(
        long luid,
        uint vendorId,
        string name) =>
        new(
            luid,
            checked((uint)luid),
            name,
            vendorId,
            1,
            8L * 1024 * 1024 * 1024,
            4L * 1024 * 1024 * 1024,
            false);

    private static IReadOnlyDictionary<long, GpuAdapterInfo> CreateAdapters(
        params GpuAdapterInfo[] adapters) =>
        adapters.ToDictionary(adapter => adapter.Luid);
}
