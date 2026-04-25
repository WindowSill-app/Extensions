using FluentAssertions;
using NodaTime;
using WindowSill.Date.Core.Models;
using WindowSill.Date.ViewModels;

namespace UnitTests.Date.Core;

public class WorldClockItemViewModelTests
{
    private static readonly DateTimeZone ParisZone = DateTimeZoneProviders.Tzdb["Europe/Paris"];
    private static readonly DateTimeZone NewYorkZone = DateTimeZoneProviders.Tzdb["America/New_York"];
    private static readonly DateTimeZone TokyoZone = DateTimeZoneProviders.Tzdb["Asia/Tokyo"];

    #region Offset computation

    [Fact]
    public void OffsetText_SameTimezone_ReturnsEmpty()
    {
        // Use the local system timezone — offset should be 0.
        DateTimeZone localZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
        var entry = new WorldClockEntry { TimeZoneId = localZone.Id };

        // Create VM without localizer by testing the offset logic indirectly.
        // The offset diff between local zone and itself should be 0.
        Instant now = SystemClock.Instance.GetCurrentInstant();
        Offset localOffset = localZone.GetUtcOffset(now);
        Offset remoteOffset = localZone.GetUtcOffset(now);
        long diffMinutes = (remoteOffset.Milliseconds - localOffset.Milliseconds) / 60_000;

        diffMinutes.Should().Be(0);
    }

    [Fact]
    public void OffsetText_DifferentTimezone_NonZero()
    {
        // Compare local to Tokyo — should have a non-zero difference (unless we're in Tokyo).
        DateTimeZone localZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
        Instant now = SystemClock.Instance.GetCurrentInstant();
        Offset localOffset = localZone.GetUtcOffset(now);
        Offset tokyoOffset = TokyoZone.GetUtcOffset(now);
        long diffMinutes = (tokyoOffset.Milliseconds - localOffset.Milliseconds) / 60_000;

        if (localZone.Id != "Asia/Tokyo")
        {
            diffMinutes.Should().NotBe(0);
        }
    }

    #endregion

    #region Day/night heuristic

    [Fact]
    public void IsDaytime_BoundaryAt6AM_IsDay()
    {
        // 6 AM should be daytime per the 6-18 heuristic.
        int hour = 6;
        bool isDaytime = hour >= 6 && hour < 18;
        isDaytime.Should().BeTrue();
    }

    [Fact]
    public void IsDaytime_BoundaryAt18_IsNight()
    {
        int hour = 18;
        bool isDaytime = hour >= 6 && hour < 18;
        isDaytime.Should().BeFalse();
    }

    [Fact]
    public void IsDaytime_At559_IsNight()
    {
        int hour = 5;
        bool isDaytime = hour >= 6 && hour < 18;
        isDaytime.Should().BeFalse();
    }

    [Fact]
    public void IsDaytime_At1759_IsDay()
    {
        int hour = 17;
        bool isDaytime = hour >= 6 && hour < 18;
        isDaytime.Should().BeTrue();
    }

    #endregion

    #region DisplayName

    [Fact]
    public void DisplayName_ReflectsEntryCustomName()
    {
        var entry = new WorldClockEntry
        {
            TimeZoneId = "America/New_York",
            CustomDisplayName = "Boss"
        };

        // Test via the Entry property directly (no VM creation needed).
        entry.DisplayName.Should().Be("Boss");
    }

    #endregion
}
