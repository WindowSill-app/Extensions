using FluentAssertions;
using WindowSill.Date.Core.Providers.CalDav;

namespace UnitTests.Date.Core;

public class CalDavNormalizeColorTests
{
    [Theory]
    [InlineData("#1BADF8FF", "#1BADF8")]
    [InlineData("#FF5733FF", "#FF5733")]
    [InlineData("#00FF00", "#00FF00")]
    [InlineData("1BADF8FF", "#1BADF8")]
    [InlineData("FF5733", "#FF5733")]
    public void NormalizeColor_ValidInput_ReturnsStandard6CharHex(string input, string expected)
    {
        CalDavCalendarAccountClient.NormalizeColor(input).Should().Be(expected);
    }

    [Fact]
    public void NormalizeColor_Null_ReturnsNull()
    {
        CalDavCalendarAccountClient.NormalizeColor(null).Should().BeNull();
    }

    [Theory]
    [InlineData("#ABC")]
    [InlineData("AB")]
    [InlineData("#")]
    public void NormalizeColor_TooShort_ReturnsNull(string input)
    {
        CalDavCalendarAccountClient.NormalizeColor(input).Should().BeNull();
    }
}
