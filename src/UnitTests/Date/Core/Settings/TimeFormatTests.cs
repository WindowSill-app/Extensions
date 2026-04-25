using System.Globalization;
using FluentAssertions;
using WindowSill.Date.Settings;

namespace UnitTests.Date.Core.Settings;

public class TimeFormatTests
{
    [Fact]
    public void FormatTime_None_ReturnsEmpty()
    {
        TimeFormat.None.FormatTime(new DateTime(2026, 4, 19, 14, 30, 45), showSeconds: false)
            .Should().BeEmpty();
    }

    [Fact]
    public void FormatTime_TwentyFourHour_WithoutSeconds()
    {
        var time = new DateTime(2026, 4, 19, 14, 5, 0);

        string result = TimeFormat.TwentyFourHour.FormatTime(time, showSeconds: false);

        result.Should().Be("14:05");
    }

    [Fact]
    public void FormatTime_TwentyFourHour_WithSeconds()
    {
        var time = new DateTime(2026, 4, 19, 14, 5, 9);

        string result = TimeFormat.TwentyFourHour.FormatTime(time, showSeconds: true);

        result.Should().Be("14:05:09");
    }

    [Fact]
    public void FormatTime_TwelveHour_WithoutSeconds()
    {
        var time = new DateTime(2026, 4, 19, 14, 30, 0);

        string result = TimeFormat.TwelveHour.FormatTime(time, showSeconds: false);

        // Should contain "2:30" and AM/PM designator.
        result.Should().Contain("2:30");
        result.Should().MatchRegex(@"[AaPp][Mm]");
    }

    [Fact]
    public void FormatTime_TwelveHour_WithSeconds()
    {
        var time = new DateTime(2026, 4, 19, 14, 30, 45);

        string result = TimeFormat.TwelveHour.FormatTime(time, showSeconds: true);

        result.Should().Contain("2:30:45");
        result.Should().MatchRegex(@"[AaPp][Mm]");
    }

    [Fact]
    public void FormatTime_TwelveHour_FrenchCulture_StillShowsAmPm()
    {
        // French culture has empty AM/PM designators.
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

            var time = new DateTime(2026, 4, 19, 14, 30, 0);
            string result = TimeFormat.TwelveHour.FormatTime(time, showSeconds: false);

            // Should fall back to invariant culture and still show AM/PM.
            result.Should().Contain("PM");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    public static IEnumerable<object[]> FormatStringData =>
    [
        [TimeFormat.TwelveHour, false, "h:mm tt"],
        [TimeFormat.TwelveHour, true, "h:mm:ss tt"],
        [TimeFormat.TwentyFourHour, false, "HH:mm"],
        [TimeFormat.TwentyFourHour, true, "HH:mm:ss"],
        [TimeFormat.None, false, ""],
    ];

    [Theory]
    [MemberData(nameof(FormatStringData))]
    public void ToFormatString_ReturnsExpectedPattern(TimeFormat format, bool showSeconds, string expected)
    {
        format.ToFormatString(showSeconds).Should().Be(expected);
    }
}
