using FluentAssertions;

using WindowSill.PerfCounter.Services.Interop;

namespace UnitTests.PerfCounter.Services;

public class PdhCounterQueryIntegrationTests
{
    [Fact]
    [Trait("Category", "WindowsIntegration")]
    internal void ProcessorInformationQuery_ReturnsNamedAggregateAndLogicalProcessors()
    {
        bool created = PdhCounterQuery.TryCreate(
            [@"\Processor Information(*)\% Processor Time"],
            out PdhCounterQuery? query,
            out uint status);

        created.Should().BeTrue($"PDH initialization returned 0x{status:X8}");
        query.Should().NotBeNull();

        using (query!)
        {
            query.Collect().Should().BeTrue();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            query.Collect().Should().BeTrue();

            IReadOnlyList<PdhCounterValue> values =
                query.GetFormattedCounterArray(0);

            values.Should().Contain(value => value.Name == "_Total");
            values.Should().Contain(value =>
                value.Name.Contains(',') &&
                !value.Name.EndsWith(",_Total", StringComparison.OrdinalIgnoreCase));
        }
    }
}
