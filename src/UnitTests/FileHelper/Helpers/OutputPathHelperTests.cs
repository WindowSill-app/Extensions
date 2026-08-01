using FluentAssertions;

using WindowSill.FileHelper.Helpers;

using Path = System.IO.Path;

namespace UnitTests.FileHelper.Helpers;

public class OutputPathHelperTests
{

    [Fact]
    internal void GetCandidatePath_ReturnsBaseName_ForCounterZero()
    {
        string result = OutputPathHelper.GetCandidatePath(@"C:\dir", "document", ".pdf", 0);

        result.Should().Be(Path.Combine(@"C:\dir", "document.pdf"));
    }

    [Theory]
    [InlineData(1, "document (1).pdf")]
    [InlineData(2, "document (2).pdf")]
    [InlineData(42, "document (42).pdf")]
    internal void GetCandidatePath_ReturnsParenthesizedName_ForPositiveCounter(int counter, string expectedFileName)
    {
        string result = OutputPathHelper.GetCandidatePath(@"C:\dir", "document", ".pdf", counter);

        result.Should().Be(Path.Combine(@"C:\dir", expectedFileName));
    }

    [Fact]
    internal void GetCandidatePath_Throws_ForNegativeCounter()
    {
        Action act = () => OutputPathHelper.GetCandidatePath(@"C:\dir", "document", ".pdf", -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
