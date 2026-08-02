using FluentAssertions;

using WindowSill.PerfCounter.Services;

namespace UnitTests.PerfCounter.Services;

public class GpuEngineInstanceTests
{
    [Fact]
    internal void TryParse_ReadsLuidPhysicalAdapterAndEngine()
    {
        const string instanceName =
            "pid_1234_luid_0x00000001_0x00000002_phys_3_eng_7_engtype_3D";

        bool parsed = GpuEngineInstance.TryParse(
            instanceName,
            out GpuEngineInstance instance);

        parsed.Should().BeTrue();
        instance.Luid.Should().Be(0x0000000100000002);
        instance.EngineId.Should().Be("3:7");
    }

    [Theory]
    [InlineData("")]
    [InlineData("pid_1_phys_0_eng_0_engtype_3D")]
    [InlineData("pid_1_luid_invalid_phys_0_eng_0_engtype_3D")]
    [InlineData("pid_1_luid_0x0_0x1_phys_0_engtype_3D")]
    internal void TryParse_RejectsMalformedInstances(string instanceName)
    {
        GpuEngineInstance.TryParse(instanceName, out _).Should().BeFalse();
    }
}
