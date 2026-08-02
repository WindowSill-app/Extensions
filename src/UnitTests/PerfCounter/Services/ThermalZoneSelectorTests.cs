using FluentAssertions;

using WindowSill.PerfCounter.Services;
using WindowSill.PerfCounter.Services.Interop;

namespace UnitTests.PerfCounter.Services;

public class ThermalZoneSelectorTests
{
    [Fact]
    internal void GetCpuTemperature_ReturnsHottestCpuIdentifiedZone()
    {
        double? result = ThermalZoneSelector.GetCpuTemperature(
            [
                new PdhCounterValue(@"ACPI\CPUZ", 323.15),
                new PdhCounterValue(@"ACPI\CPU_PACKAGE", 333.15),
                new PdhCounterValue(@"ACPI\GPU", 343.15)
            ]);

        result.Should().Be(60);
    }

    [Fact]
    internal void GetCpuTemperature_RejectsGenericThermalZones()
    {
        double? result = ThermalZoneSelector.GetCpuTemperature(
            [
                new PdhCounterValue(@"\_TZ.TZ00", 330.15),
                new PdhCounterValue(@"\_TZ.THRM", 335.15)
            ]);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(200)]
    [InlineData(500)]
    internal void GetCpuTemperature_RejectsImplausibleValues(double kelvin)
    {
        ThermalZoneSelector.GetCpuTemperature(
                [new PdhCounterValue("CPU", kelvin)])
            .Should()
            .BeNull();
    }
}
