using System.Globalization;
using FluentAssertions;
using WindowSill.Date.Settings;

namespace UnitTests.Date.Core.Settings;

public class DateFormatTests
{
    [Fact]
    public void FormatDate_None_ReturnsEmpty()
    {
        DateFormat.None.FormatDate(new DateTime(2026, 4, 19))
            .Should().BeEmpty();
    }

    public static IEnumerable<object[]> AllFormatsAndPatterns =>
    [
        [DateFormat.AbbreviatedDayMonth, "ddd, MMM d"],
        [DateFormat.ShortMonthDay, "MMM d"],
        [DateFormat.DayShortMonth, "d MMM"],
        [DateFormat.FullDayMonth, "dddd, MMMM d"],
        [DateFormat.MonthSlashDayCompact, "M/d"],
        [DateFormat.MonthSlashDay, "MM/dd"],
        [DateFormat.DaySlashMonthCompact, "d/M"],
        [DateFormat.DaySlashMonth, "dd/MM"],
        [DateFormat.MonthDayYear, "MM/dd/yyyy"],
        [DateFormat.DayMonthYear, "dd/MM/yyyy"],
        [DateFormat.Iso8601, "yyyy-MM-dd"],
    ];

    [Theory]
    [MemberData(nameof(AllFormatsAndPatterns))]
    public void ToFormatString_AllFormats_ReturnExpectedPattern(DateFormat format, string expectedPattern)
    {
        format.ToFormatString().Should().Be(expectedPattern);
    }

    [Fact]
    public void FormatDate_Iso8601_ProducesCorrectOutput()
    {
        var date = new DateTime(2026, 4, 19);

        DateFormat.Iso8601.FormatDate(date).Should().Be("2026-04-19");
    }

    [Fact]
    public void FormatDate_MonthDayYear_ProducesCorrectOutput()
    {
        var date = new DateTime(2026, 4, 19);

        DateFormat.MonthDayYear.FormatDate(date).Should().Be("04/19/2026");
    }

    [Fact]
    public void FormatDate_DayMonthYear_ProducesCorrectOutput()
    {
        var date = new DateTime(2026, 4, 19);

        DateFormat.DayMonthYear.FormatDate(date).Should().Be("19/04/2026");
    }

    public static IEnumerable<object[]> NumericFormatsWithSuffix =>
    [
        [DateFormat.MonthSlashDayCompact, "(M/D)"],
        [DateFormat.MonthSlashDay, "(MM/DD)"],
        [DateFormat.DaySlashMonthCompact, "(D/M)"],
        [DateFormat.DaySlashMonth, "(DD/MM)"],
        [DateFormat.MonthDayYear, "(MM/DD/YYYY)"],
        [DateFormat.DayMonthYear, "(DD/MM/YYYY)"],
        [DateFormat.Iso8601, "(ISO 8601)"],
    ];

    [Theory]
    [MemberData(nameof(NumericFormatsWithSuffix))]
    public void GetLabelSuffix_NumericFormats_ReturnSuffix(DateFormat format, string expectedSuffix)
    {
        format.GetLabelSuffix().Should().Be(expectedSuffix);
    }

    public static IEnumerable<object[]> NamedFormatsWithoutSuffix =>
    [
        [DateFormat.AbbreviatedDayMonth],
        [DateFormat.ShortMonthDay],
        [DateFormat.DayShortMonth],
        [DateFormat.FullDayMonth],
        [DateFormat.None],
    ];

    [Theory]
    [MemberData(nameof(NamedFormatsWithoutSuffix))]
    public void GetLabelSuffix_NamedFormats_ReturnNull(DateFormat format)
    {
        format.GetLabelSuffix().Should().BeNull();
    }
}
