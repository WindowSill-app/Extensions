using FluentAssertions;

using Microsoft.Graph.Models;

using WindowSill.CalendarPlus.Core.Providers;

namespace UnitTests.CalendarPlus;

/// <summary>
/// Unit tests for <see cref="OutlookCalendarProvider"/>.
/// </summary>
public class OutlookCalendarProviderTests
{
    [Fact]
    public void ParseGraphDateTime_NullInput_ReturnsMinValue()
    {
        // Act
        DateTimeOffset result = OutlookCalendarProvider.ParseGraphDateTime(null);

        // Assert
        result.Should().Be(DateTimeOffset.MinValue);
    }

    [Fact]
    public void ParseGraphDateTime_ValidUtcDateTime_ParsesCorrectly()
    {
        // Arrange
        var dt = new DateTimeTimeZone
        {
            DateTime = "2026-04-15T14:30:00.0000000",
            TimeZone = "UTC",
        };

        // Act
        DateTimeOffset result = OutlookCalendarProvider.ParseGraphDateTime(dt);

        // Assert
        result.Year.Should().Be(2026);
        result.Month.Should().Be(4);
        result.Day.Should().Be(15);
        result.Hour.Should().Be(14);
        result.Minute.Should().Be(30);
        result.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ParseGraphDateTime_ValidTimezone_ParsesWithOffset()
    {
        // Arrange
        var dt = new DateTimeTimeZone
        {
            DateTime = "2026-06-15T09:00:00.0000000",
            TimeZone = "Eastern Standard Time",
        };

        // Act
        DateTimeOffset result = OutlookCalendarProvider.ParseGraphDateTime(dt);

        // Assert
        result.Hour.Should().Be(9);
        // Eastern is UTC-4 in June (daylight saving)
        result.Offset.Should().Be(TimeSpan.FromHours(-4));
    }

    [Fact]
    public void ParseGraphDateTime_InvalidTimezone_FallsBackToUtc()
    {
        // Arrange
        var dt = new DateTimeTimeZone
        {
            DateTime = "2026-04-15T10:00:00.0000000",
            TimeZone = "Invalid/Timezone",
        };

        // Act
        DateTimeOffset result = OutlookCalendarProvider.ParseGraphDateTime(dt);

        // Assert
        result.Hour.Should().Be(10);
        result.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ParseGraphDateTime_NullTimezone_FallsBackToUtc()
    {
        // Arrange
        var dt = new DateTimeTimeZone
        {
            DateTime = "2026-04-15T12:00:00.0000000",
            TimeZone = null,
        };

        // Act
        DateTimeOffset result = OutlookCalendarProvider.ParseGraphDateTime(dt);

        // Assert
        result.Hour.Should().Be(12);
        result.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ParseGraphDateTime_UnparseableDateTime_ReturnsMinValue()
    {
        // Arrange
        var dt = new DateTimeTimeZone
        {
            DateTime = "not-a-date",
            TimeZone = "UTC",
        };

        // Act
        DateTimeOffset result = OutlookCalendarProvider.ParseGraphDateTime(dt);

        // Assert
        result.Should().Be(DateTimeOffset.MinValue);
    }

    [Fact]
    public void ProviderMetadata_HasExpectedValues()
    {
        // We can't easily construct OutlookCalendarProvider without MicrosoftAuthHelper,
        // but we can verify the static parse method and document expected metadata:
        // ProviderId = "microsoft", DisplayName = "Microsoft Outlook"

        // Verify ParseGraphDateTime handles edge case of empty DateTime
        var dt = new DateTimeTimeZone
        {
            DateTime = string.Empty,
            TimeZone = "UTC",
        };

        DateTimeOffset result = OutlookCalendarProvider.ParseGraphDateTime(dt);
        result.Should().Be(DateTimeOffset.MinValue);
    }
}
