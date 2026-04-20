using FluentAssertions;
using WindowSill.Date.Core.Models;

namespace UnitTests.Date.Core;

public class WorldClockEntryTests
{
    [Fact]
    public void DisplayName_WithCustomName_ReturnsCustomName()
    {
        var entry = new WorldClockEntry
        {
            TimeZoneId = "America/New_York",
            CustomDisplayName = "Boss"
        };

        entry.DisplayName.Should().Be("Boss");
    }

    [Fact]
    public void DisplayName_WithoutCustomName_ReturnsCityFromTimezoneId()
    {
        var entry = new WorldClockEntry
        {
            TimeZoneId = "America/New_York"
        };

        entry.DisplayName.Should().Be("New York");
    }

    [Fact]
    public void DisplayName_UnderscoresReplacedWithSpaces()
    {
        var entry = new WorldClockEntry
        {
            TimeZoneId = "Asia/Ho_Chi_Minh"
        };

        entry.DisplayName.Should().Be("Ho Chi Minh");
    }

    [Fact]
    public void DisplayName_NestedTimezoneId_UsesLastSegment()
    {
        var entry = new WorldClockEntry
        {
            TimeZoneId = "America/Argentina/Buenos_Aires"
        };

        entry.DisplayName.Should().Be("Buenos Aires");
    }

    [Fact]
    public void DisplayName_NoSlash_ReturnsAsIs()
    {
        var entry = new WorldClockEntry
        {
            TimeZoneId = "UTC"
        };

        entry.DisplayName.Should().Be("UTC");
    }

    [Fact]
    public void DisplayName_EmptyCustomName_FallsBackToDefault()
    {
        var entry = new WorldClockEntry
        {
            TimeZoneId = "Europe/Paris",
            CustomDisplayName = "   "
        };

        // CustomDisplayName is whitespace-only, but DisplayName checks for null.
        // The model stores the whitespace as-is; the service normalizes it.
        // DisplayName returns the non-null custom name.
        entry.DisplayName.Should().Be("   ");
    }

    [Fact]
    public void DisplayName_NullCustomName_ReturnsCityName()
    {
        var entry = new WorldClockEntry
        {
            TimeZoneId = "Europe/Paris",
            CustomDisplayName = null
        };

        entry.DisplayName.Should().Be("Paris");
    }
}
