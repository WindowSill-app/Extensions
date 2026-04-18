using FluentAssertions;
using Microsoft.Graph.Models;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Providers.Outlook;

namespace UnitTests.Date.Outlook;

public class OutlookMappingTests
{
    [Fact]
    public void ParseGraphDateTime_Null_ReturnsMinValue()
    {
        OutlookCalendarAccountClient.ParseGraphDateTime(null).Should().Be(DateTimeOffset.MinValue);
    }

    [Fact]
    public void ParseGraphDateTime_EmptyDateTime_ReturnsMinValue()
    {
        var dt = new DateTimeTimeZone { DateTime = "", TimeZone = "UTC" };
        OutlookCalendarAccountClient.ParseGraphDateTime(dt).Should().Be(DateTimeOffset.MinValue);
    }

    [Fact]
    public void ParseGraphDateTime_UtcDateTime_ParsesCorrectly()
    {
        var dt = new DateTimeTimeZone { DateTime = "2026-04-18T10:00:00", TimeZone = "UTC" };
        DateTimeOffset result = OutlookCalendarAccountClient.ParseGraphDateTime(dt);

        result.Year.Should().Be(2026);
        result.Month.Should().Be(4);
        result.Day.Should().Be(18);
        result.Hour.Should().Be(10);
        result.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ParseGraphDateTime_PacificTimeZone_ParsesWithCorrectOffset()
    {
        var dt = new DateTimeTimeZone { DateTime = "2026-04-18T10:00:00", TimeZone = "Pacific Standard Time" };
        DateTimeOffset result = OutlookCalendarAccountClient.ParseGraphDateTime(dt);

        result.Hour.Should().Be(10);
        // Pacific is UTC-7 in April (daylight saving)
        result.Offset.Should().Be(TimeSpan.FromHours(-7));
    }

    [Fact]
    public void ParseGraphDateTime_UnknownTimeZone_FallsBackToUtc()
    {
        var dt = new DateTimeTimeZone { DateTime = "2026-04-18T10:00:00", TimeZone = "Fake/TimeZone" };
        DateTimeOffset result = OutlookCalendarAccountClient.ParseGraphDateTime(dt);

        result.Hour.Should().Be(10);
        result.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ParseGraphDateTime_NullTimeZone_DefaultsToUtc()
    {
        var dt = new DateTimeTimeZone { DateTime = "2026-04-18T10:00:00", TimeZone = null };
        DateTimeOffset result = OutlookCalendarAccountClient.ParseGraphDateTime(dt);

        result.Hour.Should().Be(10);
        result.Offset.Should().Be(TimeSpan.Zero);
    }
}
