using FluentAssertions;

using WindowSill.PerfCounter.Services;
using WindowSill.PerfCounter.Services.Interop;

namespace UnitTests.PerfCounter.Services;

public class CpuUsageAggregatorTests
{
    [Fact]
    internal void Create_UsesAggregateAndPerProcessorValues()
    {
        PdhCounterValue[] totals =
        [
            new("_Total", 55),
            new("0,0", 40),
            new("0,1", 60),
            new("1,0", 65),
            new("0,_Total", 50)
        ];
        PdhCounterValue[] users =
        [
            new("_Total", 35),
            new("0,0", 25),
            new("0,1", 40),
            new("1,0", 45)
        ];
        PdhCounterValue[] privileged =
        [
            new("_Total", 20),
            new("0,0", 15),
            new("0,1", 20),
            new("1,0", 20)
        ];

        CpuPerformanceData? result =
            CpuUsageAggregator.Create(totals, users, privileged);

        result.Should().NotBeNull();
        result!.TotalUsage.Should().Be(55);
        result.UserUsage.Should().Be(35);
        result.PrivilegedUsage.Should().Be(20);
        result.LogicalProcessors.Should().Equal(
            new CpuLogicalProcessorUsage(0, 0, 40, 25, 15),
            new CpuLogicalProcessorUsage(0, 1, 60, 40, 20),
            new CpuLogicalProcessorUsage(1, 0, 65, 45, 20));
    }

    [Fact]
    internal void Create_AveragesLogicalProcessors_WhenAggregateIsUnavailable()
    {
        PdhCounterValue[] totals =
        [
            new("0,0", 20),
            new("0,1", 60)
        ];

        CpuPerformanceData? result =
            CpuUsageAggregator.Create(totals, [], []);

        result.Should().NotBeNull();
        result!.TotalUsage.Should().Be(40);
        result.LogicalProcessors.Should().HaveCount(2);
    }

    [Fact]
    internal void Create_ReturnsNull_WhenNoAggregateOrLogicalProcessorsExist()
    {
        CpuPerformanceData? result = CpuUsageAggregator.Create(
            [new PdhCounterValue("0,_Total", 50)],
            [],
            []);

        result.Should().BeNull();
    }

    [Fact]
    internal void Create_ClampsPercentages()
    {
        CpuPerformanceData? result = CpuUsageAggregator.Create(
            [
                new PdhCounterValue("_Total", 120),
                new PdhCounterValue("0,0", -10)
            ],
            [new PdhCounterValue("_Total", -5)],
            [new PdhCounterValue("_Total", 150)]);

        result.Should().NotBeNull();
        result!.TotalUsage.Should().Be(100);
        result.UserUsage.Should().Be(0);
        result.PrivilegedUsage.Should().Be(100);
        result.LogicalProcessors.Single().Usage.Should().Be(0);
    }
}
